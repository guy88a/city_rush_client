using CityRush.Quests.Data;

namespace CityRush.Quests
{
    public readonly struct QuestEvent
    {
        public readonly QuestActionType ActionType;
        public readonly int TargetId;
        public readonly int Count;

        public QuestEvent(QuestActionType actionType, int targetId, int count = 1)
        {
            ActionType = actionType;
            TargetId = targetId;
            Count = count < 1 ? 1 : count;
        }
    }
}
