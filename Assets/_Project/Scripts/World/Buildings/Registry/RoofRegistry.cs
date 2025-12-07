using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityRush.World.Buildings.Registry
{
    [Serializable]
    public struct ChimneyEntry
    {
        public string Type;     // Brick / Pipe / etc.
        public string Color;    // Red / Gray / etc.
        public GameObject Prefab;
    }

    [CreateAssetMenu(menuName = "CityRush/Registry/RoofRegistry")]
    public class RoofRegistry : ScriptableObject
    {
        public List<ChimneyEntry> Chimneys = new List<ChimneyEntry>();

        public GameObject GetChimney(string type, string color)
        {
            foreach (var entry in Chimneys)
            {
                if (entry.Type == type && entry.Color == color)
                    return entry.Prefab;
            }

            return null;
        }
    }
}
