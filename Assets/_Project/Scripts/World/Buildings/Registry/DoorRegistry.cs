using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityRush.World.Buildings.Registry
{
    [Serializable]
    public struct DoorEntry
    {
        public string Type;    // Small / Panel / Fancy
        public string Color;   // Metal / Cyan / Brown
        public string Size;    // "", "WW", "HH", "BIG"
        public GameObject Prefab;
    }

    [CreateAssetMenu(menuName = "CityRush/Registry/DoorRegistry")]
    public class DoorRegistry : ScriptableObject
    {
        public List<DoorEntry> Doors = new List<DoorEntry>();

        public GameObject GetDoor(string type, string color, string size)
        {
            foreach (var entry in Doors)
            {
                if (entry.Type == type &&
                    entry.Color == color)
                {
                    // if both sides treat size as empty -> match
                    if (string.IsNullOrEmpty(size) && string.IsNullOrEmpty(entry.Size))
                        return entry.Prefab;

                    // if both have size and they match -> match
                    if (!string.IsNullOrEmpty(size) && entry.Size == size)
                        return entry.Prefab;
                }
            }

            return null;
        }
    }
}
