using System;
using UnityEngine;

namespace CityRush.World.Street.Data
{
    [Serializable]
    public class StreetPatternData
    {
        // Indexes into the tile prefab array
        public int[] pattern;

        // How many times this pattern repeats horizontally
        // (optional, can be ignored for now)
        public int repeat;

        // For width & boundaries calculations
        public int TotalTiles => pattern.Length * Mathf.Max(1, repeat);
    }
}
