using UnityEngine;
using CityRush.World.Buildings.Data;
using CityRush.World.Buildings.Registry;

namespace CityRush.World.Buildings.Generation
{
    public class FloorComponent : MonoBehaviour
    {
        public WallRegistry wallRegistry;
        public WindowRegistry windowRegistry;
        public DoorRegistry doorRegistry;

        public int WidthModules = 3;

        public void Initialize(BuildingDefinition definition, bool isEntrance)
        {
            ClearModules();
            Build(definition, isEntrance);
        }

        private void Build(BuildingDefinition definition, bool isEntrance)
        {
            float moduleWidth = 160f / 48f; // pixels / PPU

            for (int i = 0; i < WidthModules; i++)
            {
                // -----------------------
                // WALL TYPE + COLOR
                // -----------------------
                string wallType = isEntrance ? definition.EntranceType : definition.WallType;
                string wallColor = isEntrance ? definition.EntranceColor : definition.WallColor;

                // -----------------------
                // WALL POSITION (Left / Middle / Right)
                // -----------------------
                string position =
                    i == 0 ? "Left" :
                    i == WidthModules - 1 ? "Right" :
                    "Middle";

                // -----------------------
                // BUILD WALL KEY
                // -----------------------
                string wallKey = "Wall_" + wallType + "_" + wallColor + "_" + position;

                GameObject wallPrefab = wallRegistry.Get(wallKey);
                if (wallPrefab == null)
                    continue;

                // -----------------------
                // INSTANTIATE WALL
                // -----------------------
                Transform wall = Instantiate(wallPrefab, transform).transform;
                wall.localPosition = new Vector3(i * moduleWidth, 0f, 0f);

                // -----------------------
                // HANDLE WINDOWS (NOT FOR ENTRANCE FLOORS)
                // -----------------------
                if (!isEntrance && windowRegistry != null)
                {
                    bool isOpen = DetermineWindowOpenState(definition, i);
                    string windowType = definition.WindowType;
                    string windowKey = "Window_" + windowType + "_" + (isOpen ? "Open" : "Closed");

                    GameObject windowPrefab = windowRegistry.Get(windowKey);
                    if (windowPrefab != null)
                    {
                        Transform window = Instantiate(windowPrefab, wall).transform;
                        window.localPosition = Vector3.zero; // perfect overlap
                    }
                }

                // -----------------------
                // HANDLE DOOR (ONLY ON ENTRANCE FLOOR)
                // -----------------------
                if (isEntrance && definition.EntranceAddDoor)
                {
                    bool isLeftModule = (i == 0);
                    if (isLeftModule)
                    {
                        string doorKey =
                            "Door_" +
                            definition.EntranceDoorType + "_" +
                            definition.EntranceDoorColor + "_" +
                            definition.EntranceDoorSize;

                        GameObject doorPrefab = doorRegistry.Get(doorKey);
                        if (doorPrefab != null)
                        {
                            Transform door = Instantiate(doorPrefab, wall).transform;
                            door.localPosition = Vector3.zero; // full overlap
                        }
                    }
                }
            }
        }

        private bool DetermineWindowOpenState(BuildingDefinition def, int index)
        {
            if (def.WindowsForcedPattern != null && def.WindowsForcedPattern.Length == WidthModules)
            {
                return def.WindowsForcedPattern[index] == '1';
            }

            if (def.WindowsRandomPattern)
            {
                return Random.value > 0.5f;
            }

            return false;
        }

        private void ClearModules()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}
