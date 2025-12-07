using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityRush.World.Buildings.Registry
{
    [Serializable]
    public struct WindowEntry
    {
        public string Type;     // Classic / Round / etc.
        public bool IsOpen;     // true = open prefab, false = closed
        public GameObject Prefab;
    }

    [CreateAssetMenu(menuName = "CityRush/Registry/WindowRegistry")]
    public class WindowRegistry : ScriptableObject
    {
        public List<WindowEntry> Windows = new List<WindowEntry>();

        public GameObject GetWindow(string type, bool open)
        {
            foreach (var entry in Windows)
            {
                if (entry.Type == type && entry.IsOpen == open)
                    return entry.Prefab;
            }

            return null;
        }
    }
}
