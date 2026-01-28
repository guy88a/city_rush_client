using System;

namespace CityRush.Quests
{
    [Serializable]
    public sealed class QuestState
    {
        public int QuestId { get; }
        public bool Accepted { get; private set; }
        public bool ReadyToTurnIn { get; private set; }
        public bool Completed { get; private set; }

        private readonly int[] _counts;

        public QuestState(int questId, int objectiveCount)
        {
            QuestId = questId;
            _counts = objectiveCount <= 0 ? Array.Empty<int>() : new int[objectiveCount];
        }

        public int GetCount(int objectiveIndex)
        {
            if ((uint)objectiveIndex >= (uint)_counts.Length)
                return 0;

            return _counts[objectiveIndex];
        }

        public void Accept()
        {
            if (Accepted || Completed)
                return;

            Accepted = true;
            ReadyToTurnIn = false;

            for (int i = 0; i < _counts.Length; i++)
                _counts[i] = 0;
        }

        public void MarkReadyToTurnIn()
        {
            if (!Accepted || Completed)
                return;

            ReadyToTurnIn = true;
        }

        public void MarkCompleted()
        {
            if (!Accepted || Completed)
                return;

            Completed = true;
            ReadyToTurnIn = false;
        }

        public bool TryApplyProgress(in QuestDefinition def, in QuestEvent e, out bool becameReadyToTurnIn)
        {
            becameReadyToTurnIn = false;

            if (!Accepted || Completed || ReadyToTurnIn)
                return false;

            var objectives = def.Objectives;
            if (objectives == null || objectives.Length == 0)
                return false;

            bool changed = false;

            int len = _counts.Length;
            int objectiveLen = objectives.Length;
            int n = len < objectiveLen ? len : objectiveLen;

            for (int i = 0; i < n; i++)
            {
                ref readonly var obj = ref objectives[i];

                if (obj.ActionType != e.ActionType)
                    continue;

                if (obj.TargetId != e.TargetId)
                    continue;

                int required = obj.RequiredCount < 1 ? 1 : obj.RequiredCount;
                int before = _counts[i];
                int after = before + e.Count;
                if (after > required)
                    after = required;

                if (after != before)
                {
                    _counts[i] = after;
                    changed = true;
                }
            }

            if (!changed)
                return false;

            // Check completion
            for (int i = 0; i < n; i++)
            {
                int required = objectives[i].RequiredCount < 1 ? 1 : objectives[i].RequiredCount;
                if (_counts[i] < required)
                    return true; // progressed but not done
            }

            ReadyToTurnIn = true;
            becameReadyToTurnIn = true;
            return true;
        }
    }
}
