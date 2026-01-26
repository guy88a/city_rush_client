using System.Collections.Generic;
using UnityEngine;

namespace CityRush.Quests
{
    [CreateAssetMenu(
        fileName = "QuestDB",
        menuName = "CityRush/Quests/Quest DB"
    )]
    public sealed class QuestDB : ScriptableObject
    {
        [SerializeField] private List<QuestDefinition> quests = new();

        public IReadOnlyList<QuestDefinition> Quests => quests;
    }
}
