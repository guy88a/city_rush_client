using System;
using System.Collections.Generic;

namespace CityRush.Quests
{
    public sealed class QuestService : IQuestService
    {
        public event Action<int> OnQuestAccepted;
        public event Action<int> OnQuestReadyToTurnIn;
        public event Action<int> OnQuestCompleted;
        public event Action<int, QuestReward> OnQuestRewarded;
        public event Action<int> OnQuestProgressChanged;

        private readonly QuestDB _db;
        private readonly Dictionary<int, QuestState> _states = new();

        public QuestService(QuestDB db)
        {
            _db = db;

            if (_db == null)
                return;

            _db.BuildCacheIfNeeded();

            var quests = _db.Quests;
            if (quests == null)
                return;

            for (int i = 0; i < quests.Count; i++)
            {
                var def = quests[i];
                if (_states.ContainsKey(def.QuestId))
                    continue;

                int objectivesCount = def.Objectives != null ? def.Objectives.Length : 0;
                _states.Add(def.QuestId, new QuestState(def.QuestId, objectivesCount));
            }
        }

        public bool TryGetDefinition(int questId, out QuestDefinition def)
        {
            def = default;
            return _db != null && _db.TryGet(questId, out def);
        }

        public QuestStage GetStage(int questId)
        {
            if (!TryGetDefinition(questId, out var def))
                return QuestStage.Locked;

            if (!_states.TryGetValue(questId, out var st))
                return QuestStage.Locked;

            if (st.Completed)
                return QuestStage.Completed;

            if (st.ReadyToTurnIn)
                return QuestStage.ReadyToTurnIn;

            if (st.Accepted)
                return QuestStage.InProgress;

            return ArePrereqsMet(def) ? QuestStage.Available : QuestStage.Locked;
        }

        public int GetObjectiveCount(int questId, int objectiveIndex)
        {
            if (!_states.TryGetValue(questId, out var st))
                return 0;

            return st.GetCount(objectiveIndex);
        }

        public void GetNpcQuestOffers(int npcId, List<int> questIdsOut)
        {
            if (questIdsOut == null)
                return;

            questIdsOut.Clear();

            if (_db == null)
                return;

            var quests = _db.Quests;
            if (quests == null)
                return;

            for (int i = 0; i < quests.Count; i++)
            {
                var def = quests[i];

                if (def.StartNpcId != npcId)
                    continue;

                var stage = GetStage(def.QuestId);
                if (stage == QuestStage.Available)
                    questIdsOut.Add(def.QuestId);
            }
        }

        public void GetNpcQuestTurnIns(int npcId, List<int> questIdsOut)
        {
            if (questIdsOut == null)
                return;

            questIdsOut.Clear();

            if (_db == null)
                return;

            var quests = _db.Quests;
            if (quests == null)
                return;

            for (int i = 0; i < quests.Count; i++)
            {
                var def = quests[i];

                if (def.EndNpcId != npcId)
                    continue;

                var stage = GetStage(def.QuestId);
                if (stage == QuestStage.ReadyToTurnIn)
                    questIdsOut.Add(def.QuestId);
            }
        }

        public void GetActiveQuests(List<int> questIdsOut)
        {
            if (questIdsOut == null)
                return;

            questIdsOut.Clear();

            if (_db == null)
                return;

            var quests = _db.Quests;
            if (quests == null)
                return;

            for (int i = 0; i < quests.Count; i++)
            {
                int id = quests[i].QuestId;
                var stage = GetStage(id);

                if (stage == QuestStage.InProgress || stage == QuestStage.ReadyToTurnIn)
                    questIdsOut.Add(id);
            }
        }

        public bool TryAccept(int questId)
        {
            if (!TryGetDefinition(questId, out var def))
                return false;

            if (!_states.TryGetValue(questId, out var st))
                return false;

            if (st.Accepted || st.Completed)
                return false;

            if (!ArePrereqsMet(def))
                return false;

            st.Accept();
            OnQuestAccepted?.Invoke(questId);
            return true;
        }

        public void SubmitEvent(in QuestEvent e)
        {
            if (_db == null)
                return;

            var quests = _db.Quests;
            if (quests == null)
                return;

            for (int i = 0; i < quests.Count; i++)
            {
                var def = quests[i];
                if (!_states.TryGetValue(def.QuestId, out var st))
                    continue;

                if (!st.Accepted || st.Completed || st.ReadyToTurnIn)
                    continue;

                if (st.TryApplyProgress(def, e, out bool becameReady))
                {
                    OnQuestProgressChanged?.Invoke(def.QuestId);

                    if (becameReady)
                        OnQuestReadyToTurnIn?.Invoke(def.QuestId);
                }
            }
        }

        public bool CanTurnIn(int questId, int npcId)
        {
            if (!TryGetDefinition(questId, out var def))
                return false;

            if (def.EndNpcId != npcId)
                return false;

            return GetStage(questId) == QuestStage.ReadyToTurnIn;
        }

        public bool TryTurnIn(int questId, int npcId)
        {
            if (!CanTurnIn(questId, npcId))
                return false;

            if (!_states.TryGetValue(questId, out var st))
                return false;

            if (!TryGetDefinition(questId, out var def))
                return false;

            st.MarkCompleted();

            // Reward is signaled (UI / gameplay systems decide how to grant items/tokens).
            OnQuestRewarded?.Invoke(questId, def.Reward);
            OnQuestCompleted?.Invoke(questId);

            return true;
        }

        private bool ArePrereqsMet(in QuestDefinition def)
        {
            var prereqs = def.PrereqQuestIds;
            if (prereqs == null || prereqs.Length == 0)
                return true;

            for (int i = 0; i < prereqs.Length; i++)
            {
                int prereqId = prereqs[i];

                if (!_states.TryGetValue(prereqId, out var prereq))
                    return false;

                if (!prereq.Completed)
                    return false;
            }

            return true;
        }

        public void GetNpcActiveQuests(int npcId, List<int> questIdsOut)
        {
            questIdsOut.Clear();

            if (_db == null || _db.Quests == null)
                return;

            // “Active” here means: already started and relevant to this NPC
            // (either started here or ends here).
            for (int i = 0; i < _db.Quests.Count; i++)
            {
                var def = _db.Quests[i];

                if (!_states.ContainsKey(def.QuestId))
                    continue;

                var stage = GetStage(def.QuestId);
                if (stage != QuestStage.InProgress && stage != QuestStage.ReadyToTurnIn)
                    continue;

                if (def.StartNpcId == npcId || def.EndNpcId == npcId)
                    questIdsOut.Add(def.QuestId);
            }
        }

    }
}
