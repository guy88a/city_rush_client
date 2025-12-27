using System;
using System.Collections.Generic;
using UnityEngine;
using CityRush.World.Street.Data;

namespace CityRush.World.Map
{
    [Serializable]
    public class MapData
    {
        public string MapId;
        public Vector2Int PlayerSpawn; // Section (Y), Street (X)
        public List<ZoneData> Zones;
    }

    [Serializable]
    public class ZoneData
    {
        public string ZoneId;
        public ZoneType Type;
        public Vector2Int GridSize; // Rows (sections) x Cols (streets)
        public List<SectionRow> Structure; // Structure[row][col] = StreetData
    }

    public enum ZoneType
    {
        City,
        Countryside
    }

    [Serializable]
    public class SectionRow
    {
        public string ThemeId; // Visual/prefab style
        public List<StreetRef> Streets; // Streets in this section (row)
    }

    [Serializable]
    public class StreetRef
    {
        public string StreetId;
        public string JsonPath; // Resources path to StreetData JSON
    }
}
