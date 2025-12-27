using System;
using CityRush.World.Buildings.Data;

namespace CityRush.World.Street.Data
{
    [Serializable]
    public class StreetSpawnData
    {
        public float x;
    }

    [Serializable]
    public class StreetData
    {
        public StreetSpawnData spawn;
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

        public int GetStreetWidthInTiles()
        {
            return road != null ? road.TotalTiles : 0;
        }
    }

    [Serializable]
    public class PedestriansData
    {
        // reserved for future use
    }
}
