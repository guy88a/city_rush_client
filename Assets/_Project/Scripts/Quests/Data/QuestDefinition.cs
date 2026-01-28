using System;
using UnityEngine;

namespace CityRush.Quests
{
    [Serializable]
    public struct QuestDefinition
    {
        [Header("Identity")]
        [SerializeField] private int questId;

        [Header("NPC")]
        [SerializeField] private int startNpcId;
        [SerializeField] private int endNpcId;

        [Header("Text")]
        [SerializeField] private string title;
        [TextArea(2, 6)][SerializeField] private string descAvailable;
        [TextArea(2, 6)][SerializeField] private string descInProgress;
        [TextArea(2, 6)][SerializeField] private string descFinished;

        [Header("Rules")]
        [SerializeField] private int[] prereqQuestIds;
        [SerializeField] private QuestObjective[] objectives;

        [Header("Reward")]
        [SerializeField] private QuestReward reward;

        public int QuestId => questId;
        public int StartNpcId => startNpcId;
        public int EndNpcId => endNpcId;

        public string Title => title;
        public string DescAvailable => descAvailable;
        public string DescInProgress => descInProgress;
        public string DescFinished => descFinished;

        public int[] PrereqQuestIds => prereqQuestIds;
        public QuestObjective[] Objectives => objectives;

        public QuestReward Reward => reward;
    }
}
