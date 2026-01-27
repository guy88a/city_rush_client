using System;
using System.Collections.Generic;
using UnityEngine;
using CityRush.Quests.Data;

namespace CityRush.Quests
{
    public enum QuestStatus
    {
        Available = 0,
        Active = 1,
        Finished = 2,
        Completed = 3,
    }

    // Persisted container (v1.0): QuestId + ObjectiveProgress[]
    [Serializable]
    public sealed class QuestRuntime
    {
        [SerializeField] private int questId;
        [SerializeField] private int[] objectiveProgress;

        public int QuestId => questId;
        public int[] ObjectiveProgress => objectiveProgress;

        public QuestRuntime(int questId, int objectivesCount)
        {
            this.questId = questId;
            objectiveProgress = objectivesCount > 0 ? new int[objectivesCount] : Array.Empty<int>();
        }

        public bool IsFinished(in QuestDefinition def)
        {
            var objectives = def.Objectives;
            if (objectives == null || objectives.Length == 0)
                return true;

            if (objectiveProgress == null || objectiveProgress.Length != objectives.Length)
                return false;

            for (int i = 0; i < objectives.Length; i++)
            {
                if (objectiveProgress[i] < objectives[i].RequiredCount)
                    return false;
            }

            return true;
        }

        public void ClampToDefinition(in QuestDefinition def)
        {
            var objectives = def.Objectives;
            if (objectives == null)
                objectives = Array.Empty<QuestObjective>();

            if (objectiveProgress == null || objectiveProgress.Length != objectives.Length)
                objectiveProgress = objectives.Length > 0 ? new int[objectives.Length] : Array.Empty<int>();

            for (int i = 0; i < objectives.Length; i++)
                objectiveProgress[i] = Mathf.Clamp(objectiveProgress[i], 0, Mathf.Max(0, objectives[i].RequiredCount));
        }

        public bool TryAddProgress(in QuestDefinition def, QuestActionType actionType, int targetId, int amount)
        {
            if (amount <= 0)
                return false;

            var objectives = def.Objectives;
            if (objectives == null || objectives.Length == 0)
                return false;

            ClampToDefinition(def);

            bool changed = false;

            for (int i = 0; i < objectives.Length; i++)
            {
                if (objectives[i].ActionType != actionType)
                    continue;

                if (objectives[i].TargetId != targetId)
                    continue;

                int before = objectiveProgress[i];
                int after = Mathf.Clamp(before + amount, 0, Mathf.Max(0, objectives[i].RequiredCount));
                if (after != before)
                {
                    objectiveProgress[i] = after;
                    changed = true;
                }
            }

            return changed;
        }
    }

    // Persisted container (v1.0): ActiveQuests[] (QuestRuntime) + CompletedQuestIds[]
    [Serializable]
    public sealed class PlayerQuestSave
    {
        [SerializeField] private QuestRuntime[] activeQuests;
        [SerializeField] private int[] completedQuestIds;

        public QuestRuntime[] ActiveQuests => activeQuests;
        public int[] CompletedQuestIds => completedQuestIds;

        public PlayerQuestSave(QuestRuntime[] activeQuests, int[] completedQuestIds)
        {
            this.activeQuests = activeQuests ?? Array.Empty<QuestRuntime>();
            this.completedQuestIds = completedQuestIds ?? Array.Empty<int>();
        }
    }

    // Runtime brain (v1.0): DB index + Active + Completed.
    // Rewards and UI wiring come later; this owns only quest state.
    public sealed class QuestLog
    {
        private QuestDB _db;

        private readonly Dictionary<int, QuestDefinition> _defsById = new();
        private readonly Dictionary<int, QuestRuntime> _activeById = new();
        private readonly HashSet<int> _completed = new();

        public event Action OnChanged;

        public bool HasDb => _db != null;

        public void Initialize(QuestDB db, PlayerQuestSave save = null)
        {
            _db = db;
            RebuildIndex();
            LoadSave(save);
        }

        public void RebuildIndex()
        {
            _defsById.Clear();

            if (_db == null)
                return;

            var list = _db.Quests;
            if (list == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                int id = list[i].QuestId;
                if (id <= 0)
                {
                    Debug.LogError($"[QuestLog] QuestDefinition has invalid QuestId at index={i} (id={id}).");
                    continue;
                }

                if (_defsById.ContainsKey(id))
                {
                    Debug.LogError($"[QuestLog] Duplicate QuestId found in QuestDB: {id}");
                    continue;
                }

                _defsById.Add(id, list[i]);
            }
        }

        public bool TryGetDefinition(int questId, out QuestDefinition def) => _defsById.TryGetValue(questId, out def);

        public bool IsActive(int questId) => _activeById.ContainsKey(questId);
        public bool IsCompleted(int questId) => _completed.Contains(questId);

        public bool TryGetActive(int questId, out QuestRuntime runtime) => _activeById.TryGetValue(questId, out runtime);

        public bool IsFinished(int questId)
        {
            if (!TryGetDefinition(questId, out var def))
                return false;

            if (!TryGetActive(questId, out var rt))
                return false;

            rt.ClampToDefinition(def);
            return rt.IsFinished(def);
        }

        public bool CanAccept(int questId)
        {
            if (!TryGetDefinition(questId, out var def))
                return false;

            if (IsActive(questId) || IsCompleted(questId))
                return false;

            var prereqs = def.PrereqQuestIds;
            if (prereqs != null)
            {
                for (int i = 0; i < prereqs.Length; i++)
                {
                    if (!_completed.Contains(prereqs[i]))
                        return false;
                }
            }

            return true;
        }

        public bool TryAccept(int questId)
        {
            if (!CanAccept(questId))
                return false;

            var def = _defsById[questId];
            var rt = new QuestRuntime(questId, def.Objectives != null ? def.Objectives.Length : 0);
            _activeById.Add(questId, rt);

            OnChanged?.Invoke();
            return true;
        }

        public bool TryTurnIn(int questId)
        {
            if (!IsFinished(questId))
                return false;

            if (!_activeById.Remove(questId))
                return false;

            _completed.Add(questId);

            OnChanged?.Invoke();
            return true;
        }

        public bool OnQuestEvent(QuestActionType actionType, int targetId, int amount = 1)
        {
            if (amount <= 0)
                return false;

            bool anyChanged = false;

            // Iterate snapshot to avoid collection modification risks.
            if (_activeById.Count == 0)
                return false;

            var keys = ListPool<int>.Get();
            try
            {
                keys.AddRange(_activeById.Keys);

                for (int k = 0; k < keys.Count; k++)
                {
                    int questId = keys[k];
                    if (!_activeById.TryGetValue(questId, out var rt))
                        continue;

                    if (!_defsById.TryGetValue(questId, out var def))
                        continue;

                    if (rt.TryAddProgress(def, actionType, targetId, amount))
                        anyChanged = true;
                }
            }
            finally
            {
                ListPool<int>.Release(keys);
            }

            if (anyChanged)
                OnChanged?.Invoke();

            return anyChanged;
        }

        public PlayerQuestSave BuildSave()
        {
            var active = new QuestRuntime[_activeById.Count];
            int idx = 0;

            foreach (var kv in _activeById)
            {
                int questId = kv.Key;
                var rt = kv.Value;

                if (_defsById.TryGetValue(questId, out var def))
                    rt.ClampToDefinition(def);

                active[idx++] = rt;
            }

            var completed = new int[_completed.Count];
            _completed.CopyTo(completed);

            return new PlayerQuestSave(active, completed);
        }

        public void LoadSave(PlayerQuestSave save)
        {
            _activeById.Clear();
            _completed.Clear();

            if (save != null)
            {
                if (save.CompletedQuestIds != null)
                {
                    for (int i = 0; i < save.CompletedQuestIds.Length; i++)
                        _completed.Add(save.CompletedQuestIds[i]);
                }

                if (save.ActiveQuests != null)
                {
                    for (int i = 0; i < save.ActiveQuests.Length; i++)
                    {
                        var rt = save.ActiveQuests[i];
                        if (rt == null)
                            continue;

                        int id = rt.QuestId;
                        if (id <= 0)
                            continue;

                        // Completed wins over Active.
                        if (_completed.Contains(id))
                            continue;

                        if (!_defsById.TryGetValue(id, out var def))
                            continue;

                        rt.ClampToDefinition(def);
                        _activeById[id] = rt;
                    }
                }
            }

            OnChanged?.Invoke();
        }

        // Local tiny pool to avoid GC in OnQuestEvent. Keep internal and minimal.
        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new();

            public static List<T> Get() => Pool.Count > 0 ? Pool.Pop() : new List<T>(16);

            public static void Release(List<T> list)
            {
                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
