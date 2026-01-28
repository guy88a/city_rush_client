using System;
using System.Collections.Generic;

namespace CityRush.Quests
{
    public interface IQuestService
    {
        event Action<int> OnQuestAccepted;
        event Action<int> OnQuestReadyToTurnIn;
        event Action<int> OnQuestCompleted;
        event Action<int, QuestReward> OnQuestRewarded;
        event Action<int> OnQuestProgressChanged;

        bool TryGetDefinition(int questId, out QuestDefinition def);

        QuestStage GetStage(int questId);

        void GetNpcQuestOffers(int npcId, List<int> questIdsOut);
        void GetNpcQuestTurnIns(int npcId, List<int> questIdsOut);
        void GetActiveQuests(List<int> questIdsOut);
        void GetNpcActiveQuests(int npcId, List<int> questIdsOut);

        bool TryAccept(int questId);

        void SubmitEvent(in QuestEvent e);

        bool CanTurnIn(int questId, int npcId);
        bool TryTurnIn(int questId, int npcId);

        // Returns current progress count for a quest objective index (0-based).
        int GetObjectiveCount(int questId, int objectiveIndex);
    }
}
