using System;
using System.Collections.Generic;

namespace CityRush.World.Buildings.Data
{
    [Serializable]
    public class BuildingPropsGrid
    {
        // Floors index includes entrance floor
        public List<FloorPropsRow> Floors = new();
    }

    [Serializable]
    public class FloorPropsRow
    {
        // One entry per module (WidthModules)
        public List<string> Modules = new();
        // string = prop key, empty or null = no prop
    }
}
