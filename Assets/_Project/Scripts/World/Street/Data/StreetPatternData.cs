using System;

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
    }
}
