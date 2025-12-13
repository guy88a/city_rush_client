using System.Collections.Generic;
using UnityEngine;

namespace CityRush.World.Buildings.Props
{
    public static class WallPropOffsets
    {
        // Offsets are applied ONLY when prop is placed on a WALL.
        // Values are relative to the module origin (bottom-left of module).
        // All props use Bottom-Center pivot.

        private static readonly Dictionary<string, Vector2> Offsets =
            new()
            {
                // Antennas / Dishes
                { "Prop_Dish_Simple",           new Vector2(0f, 150f) },

                // Utilities
                { "Prop_ElectrictyBox_Normal",  new Vector2(0f, 180f) },
                { "Prop_Vent_Normal",           new Vector2(0f, 120f) },

                // Posters
                { "Prop_Poster_Big",            new Vector2(0f, 120f) },

                // Plants (wall-mounted / decorative)
                { "Prop_Plant_Normal",          new Vector2(0f, 200f) },
                { "Prop_Plant_FlowerA",         new Vector2(0f, 200f) },
                { "Prop_Plant_FlowerB",         new Vector2(0f, 200f) },
                { "Prop_Plant_FlowerC",         new Vector2(0f, 200f) },
            };

        public static Vector2 Get(string propKey)
        {
            if (Offsets.TryGetValue(propKey, out var offset))
                return offset;

            return Vector2.zero;
        }
    }
}
