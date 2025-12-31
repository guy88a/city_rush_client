using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityRush.World.Buildings.Registry.Interior
{
    [Serializable]
    public struct InteriorFloorEntry
    {
        public string Key;
        public GameObject Prefab;
    }

    [CreateAssetMenu(menuName = "CityRush/Registry/Interior/FloorRegistry")]
    public class InteriorFloorRegistry : ScriptableObject
    {
        public List<InteriorFloorEntry> Entries = new List<InteriorFloorEntry>();
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
