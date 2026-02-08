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
        public StreetNpcsData npcs;
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

    [Serializable]
    public class StreetNpcsData
    {
        public StreetPedestriansSpawnData pedestrians;
        public StreetNpcAddressEntry[] residents;
    }

    [Serializable]
    public class StreetPedestriansSpawnData
    {
        public int maxCount;
        public int[] npcIds;
    }

    [Serializable]
    public class StreetNpcAddressEntry
    {
        public int npcId;
        public StreetNpcAddressData address;

        // Optional. Can be empty and computed later from tags.
        public string formattedAddress;
    }

    [Serializable]
    public class StreetNpcAddressData
    {
        public int buildingIndex;    // 0-based, index into StreetData.buildings[]
        public int floorIndex;       // 0-based
        public int apartmentIndex;   // 0-based
    }

}
