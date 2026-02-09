using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityRush.World.Street.Registry
{
    [Serializable]
    public struct ParkGroundEntry
    {
        public string Key;
        public GameObject Prefab;
    }

    [Serializable]
    public struct ParkFenceEntry
    {
        public string Key;
        public GameObject Prefab;
    }

    [Serializable]
    public struct ParkPropEntry
    {
        public string Key;
        public GameObject Prefab;
    }

    [CreateAssetMenu(menuName = "CityRush/Registry/ParkRegistry")]
    public class ParkRegistry : ScriptableObject
    {
        public List<ParkGroundEntry> Grounds = new List<ParkGroundEntry>();
        public List<ParkFenceEntry> Fences = new List<ParkFenceEntry>();
        public List<ParkPropEntry> Props = new List<ParkPropEntry>();

        private Dictionary<string, GameObject> _groundsMap;
        private Dictionary<string, GameObject> _fencesMap;
        private Dictionary<string, GameObject> _propsMap;

        private void OnEnable()
        {
            _groundsMap = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
            _fencesMap = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
            _propsMap = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < Grounds.Count; i++)
            {
                var e = Grounds[i];
                if (string.IsNullOrWhiteSpace(e.Key) || e.Prefab == null)
                    continue;

                if (!_groundsMap.ContainsKey(e.Key))
                    _groundsMap.Add(e.Key, e.Prefab);
            }

            for (int i = 0; i < Fences.Count; i++)
            {
                var e = Fences[i];
                if (string.IsNullOrWhiteSpace(e.Key) || e.Prefab == null)
                    continue;

                if (!_fencesMap.ContainsKey(e.Key))
                    _fencesMap.Add(e.Key, e.Prefab);
            }

            for (int i = 0; i < Props.Count; i++)
            {
                var e = Props[i];
                if (string.IsNullOrWhiteSpace(e.Key) || e.Prefab == null)
                    continue;

                if (!_propsMap.ContainsKey(e.Key))
                    _propsMap.Add(e.Key, e.Prefab);
            }
        }

        public GameObject GetGround(string key)
        {
            if (_groundsMap != null && _groundsMap.TryGetValue(key, out var prefab))
                return prefab;

            return null;
        }

        public GameObject GetFence(string key)
        {
            if (_fencesMap != null && _fencesMap.TryGetValue(key, out var prefab))
                return prefab;

            return null;
        }

        public GameObject GetProp(string key)
        {
            if (_propsMap != null && _propsMap.TryGetValue(key, out var prefab))
                return prefab;

            return null;
        }
    }
}
