using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityRush.World.Buildings.Registry
{
    [Serializable]
    public struct WindowModuleEntry
    {
        public string Key;      // Example: "Window_Classic_Open"
        public GameObject Prefab;
    }

    [CreateAssetMenu(menuName = "CityRush/Registry/WindowRegistry")]
    public class WindowRegistry : ScriptableObject
    {
        public List<WindowModuleEntry> Entries = new List<WindowModuleEntry>();
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
