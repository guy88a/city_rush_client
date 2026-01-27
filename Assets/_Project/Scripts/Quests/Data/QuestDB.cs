using System;
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

        [NonSerialized] private Dictionary<int, QuestDefinition> _byId;

        public bool TryGet(int questId, out QuestDefinition def)
        {
            BuildCacheIfNeeded();
            return _byId.TryGetValue(questId, out def);
        }

        public void BuildCacheIfNeeded()
        {
            if (_byId != null)
                return;

            _byId = new Dictionary<int, QuestDefinition>(quests != null ? quests.Count : 0);

            if (quests == null)
                return;

            for (int i = 0; i < quests.Count; i++)
            {
                var q = quests[i];
                _byId[q.QuestId] = q;
            }
        }
    }
}
