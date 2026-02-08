using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityRush.Units
{
    [Serializable]
    public sealed class NpcVisualDefinition
    {
        [SerializeField] private string key;
        [SerializeField] private Sprite sprite;
        [SerializeField] private AnimatorOverrideController overrideController;

        public string Key => key;
        public Sprite Sprite => sprite;
        public AnimatorOverrideController OverrideController => overrideController;
    }

    [CreateAssetMenu(menuName = "CityRush/Units/NPC Visuals DB", fileName = "NpcVisualsDB")]
    public sealed class NpcVisualsDB : ScriptableObject
    {
        [SerializeField] private List<NpcVisualDefinition> visuals = new();

        [NonSerialized] private Dictionary<string, NpcVisualDefinition> _byKey;
        [NonSerialized] private bool _cacheBuilt;

        public List<NpcVisualDefinition> Visuals => visuals;

        public void BuildCacheIfNeeded()
        {
            if (_cacheBuilt)
                return;

            _cacheBuilt = true;

            if (_byKey == null)
                _byKey = new Dictionary<string, NpcVisualDefinition>(visuals != null ? visuals.Count : 0);
            else
                _byKey.Clear();

            if (visuals == null)
                return;

            for (int i = 0; i < visuals.Count; i++)
            {
                var def = visuals[i];
                if (def == null)
                    continue;

                string key = def.Key;
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (_byKey.ContainsKey(key))
                {
                    Debug.LogWarning($"[NpcVisualsDB] Duplicate Key '{key}' in '{name}' (index {i}). Keeping first.", this);
                    continue;
                }

                _byKey.Add(key, def);
            }
        }

        public bool TryGet(string key, out NpcVisualDefinition def)
        {
            def = null;

            if (string.IsNullOrWhiteSpace(key))
                return false;

            BuildCacheIfNeeded();
            return _byKey != null && _byKey.TryGetValue(key, out def);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _cacheBuilt = false;
            BuildCacheIfNeeded();
        }
#endif
    }
}
