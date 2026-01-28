using CityRush.Quests.Data;
using System;
using UnityEngine;

namespace CityRush.Quests
{
    [Serializable]
    public struct QuestObjective
    {
        [SerializeField] private QuestActionType actionType;
        [SerializeField] private int targetId;
        [SerializeField] private int requiredCount;
        [SerializeField] private string text;

        public QuestActionType ActionType => actionType;
        public int TargetId => targetId;
        public int RequiredCount => requiredCount;
        public string Text => text;
    }
}
