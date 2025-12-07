using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityRush.World.Buildings.Registry
{
    public enum WallPosition { Left, Middle, Right }

    [Serializable]
    public struct WallEntry
    {
        public string Type;      // Brick / Panel / Concrete
        public string Color;     // Mauve / Red / etc.
        public WallPosition Position;
        public GameObject Prefab;
        public bool IsEntrance;  // true = entrance variant
    }

    [CreateAssetMenu(menuName = "CityRush/Registry/WallRegistry")]
    public class WallRegistry : ScriptableObject
    {
        public List<WallEntry> Walls = new List<WallEntry>();

        public GameObject GetWall(string type, string color, WallPosition position, bool entrance)
        {
            foreach (var entry in Walls)
            {
                if (entry.Type == type &&
                    entry.Color == color &&
                    entry.Position == position &&
                    entry.IsEntrance == entrance)
                {
                    return entry.Prefab;
                }
            }

            return null;
        }
    }
}
