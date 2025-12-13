using System.Collections.Generic;
using UnityEngine;

namespace CityRush.World.Buildings.Registry
{
    [CreateAssetMenu(
        fileName = "BuildingPropsRegistry",
        menuName = "CityRush/Registry/Props Registry"
    )]
    public class PropsRegistry : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string Key;
            public GameObject Prefab;
        }

        [SerializeField]
        private List<Entry> entries = new();

        private Dictionary<string, GameObject> _lookup;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, GameObject>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Key) || entry.Prefab == null)
                    continue;

                if (_lookup.ContainsKey(entry.Key))
                {
                    Debug.LogWarning(
                        $"[PropsRegistry] Duplicate key detected: {entry.Key}",
                        this
                    );
                    continue;
                }

                _lookup.Add(entry.Key, entry.Prefab);
            }
        }

        public GameObject Get(string key)
        {
            if (_lookup == null)
                BuildLookup();

            if (_lookup.TryGetValue(key, out var prefab))
                return prefab;

            Debug.LogWarning(
                $"[PropsRegistry] Missing prop for key: {key}",
                this
            );
            return null;
        }
    }
}
