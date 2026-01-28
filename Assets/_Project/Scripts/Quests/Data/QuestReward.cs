using System;
using UnityEngine;

namespace CityRush.Quests
{
    [Serializable]
    public struct QuestReward
    {
        [SerializeField] private int tokens;
        [SerializeField] private int[] rewardItemIds;

        public int Tokens => tokens;
        public int[] RewardItemIds => rewardItemIds;
    }
}
