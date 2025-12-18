using System;
using CityRush.World.Buildings.Data;

namespace CityRush.World.Street.Data
{
    [Serializable]
    public class StreetData
    {
        public StreetVisualData street;
        public BuildingDefinition[] buildings;
        public PedestriansData pedestrians;
    }

    [Serializable]
    public class StreetVisualData
    {
        public StreetPatternData pavements;
        public StreetPatternData road;
        public object props;
    }

    [Serializable]
    public class PedestriansData
    {
        // reserved for future use
    }
}
