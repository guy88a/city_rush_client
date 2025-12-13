using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityRush.World.Buildings.Registry
{
    [Serializable]
    public struct RooftopSeparatorEntry
    {
        public string Key;       // Example: "Rooftop_Separator_Fancy_Left_WW"
        public GameObject Prefab;
    }

    [CreateAssetMenu(menuName = "CityRush/Registry/RooftopSeparatorRegistry")]
    public class RooftopSeparatorRegistry : ScriptableObject
    {
        public List<RooftopSeparatorEntry> Entries = new List<RooftopSeparatorEntry>();
        private Dictionary<string, GameObject> map;

        private void OnEnable()
        {
            map = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in Entries)
            {
                if (string.IsNullOrWhiteSpace(e.Key) || e.Prefab == null)
                    continue;

                if (!map.ContainsKey(e.Key))
                    map.Add(e.Key, e.Prefab);
            }
        }

        public GameObject Get(string key)
        {
            if (map != null && map.TryGetValue(key, out var prefab))
                return prefab;

            return null;
        }
    }
}
