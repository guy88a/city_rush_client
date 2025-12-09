using UnityEngine;

namespace CityRush.World.Buildings.Data
{
    // BuildingDefinition
    // Pure data container describing a building.
    // No unicode, no special characters.

    [System.Serializable]
    public class BuildingDefinition
    {
        // Regular Floors - Walls
        public string WallType;
        public string WallColor;

        // Windows
        public string WindowType;
        public bool WindowsRandomPattern;
        public string WindowsForcedPattern; // example: "101" or "010"

        // Entrance
        public string EntranceType;      // entrance wall family
        public string EntranceColor;     // entrance wall color
        public bool EntranceDecoration;  // add side decorations

        // Entrance door / layout
        public bool EntranceTwoAssetsMode;  // special 2-assets entrance behaviour
        public bool EntranceEmbeddedDoor;   // door baked into wall at DoorIndex
        public int EntranceDoorIndex;       // module index for door / embedded door

        public bool EntranceAddDoor;     // false = baked in door in wall
        public string EntranceDoorType;  // only if EntranceAddDoor == true
        public string EntranceDoorColor; // only if EntranceAddDoor == true
        public string EntranceDoorSize;  // only if EntranceAddDoor == true

        // Rooftop
        public string RooftopType;       // chimney wall family
        public string RooftopColor;      // chimney wall color
        public int RooftopPattern;       // module index for rooftop access door or special layout

        // Building Shape
        public int Width;        // modules per row
        public int FloorsCount;  // number of regular floors
    }
}
