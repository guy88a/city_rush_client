using CityRush.World.Buildings.Data;
using CityRush.World.Buildings.Registry;
using UnityEngine;

namespace CityRush.World.Buildings.Generation
{
    public class FloorComponent : MonoBehaviour
    {
        SpriteRenderer wallSR;
        SpriteRenderer winSR;
        SpriteRenderer doorSR;

        public WallRegistry wallRegistry;
        public WindowRegistry windowRegistry;
        public DoorRegistry doorRegistry;

        public int WidthModules = 3;
        float halfWidth = (160f / 48f) * 0.5f; // ~1.6667

        public void Initialize(BuildingDefinition definition, bool isEntrance)
        {
            ClearModules();
            Build(definition, isEntrance);
        }

        private void Build(BuildingDefinition def, bool isEntrance)
        {
            float moduleWidth = 160f / 48f;

            for (int i = 0; i < WidthModules; i++)
            {
                // ================
                // SELECT WALL KEY
                // ================
                string wallKey = null;
                string position = GetWallPosition(i);

                if (!isEntrance)
                {
                    // Regular floor wall: unchanged
                    wallKey = "Wall_" + def.WallType + "_" + def.WallColor + "_" + position;
                }
                else
                {
                    // ============================================================
                    // ENTRANCE FLOOR LOGIC – NEW SYSTEM
                    // ============================================================

                    bool isDoorIndex = (i == def.EntranceDoorIndex);

                    // ---------------------------
                    // CASE 1 — Embedded Door
                    // ---------------------------
                    if (def.EntranceEmbeddedDoor && isDoorIndex)
                    {
                        // Instead of Wall_Left/Middle/Right, use generic Door panel
                        wallKey = "Wall_" + def.EntranceType + "_" + def.EntranceColor + "_Door";
                    }
                    else
                    {
                        // ---------------------------
                        // CASE 2 — Two Assets Mode
                        // ---------------------------
                        if (def.EntranceTwoAssetsMode)
                        {
                            if (isDoorIndex)
                            {
                                // Fallback to regular wall type/color at door index
                                wallKey = "Wall_" + def.WallType + "_" + def.WallColor + "_" + position;
                            }
                            else
                            {
                                // Other modules use entrance wall style
                                wallKey = "Wall_" + def.EntranceType + "_" + def.EntranceColor + "_" + position;
                            }
                        }
                        else
                        {
                            // ---------------------------
                            // CASE 3 — Normal Entrance
                            // ---------------------------
                            wallKey = "Wall_" + def.EntranceType + "_" + def.EntranceColor + "_" + position;
                        }
                    }
                }

                // ================
                // GET WALL PREFAB
                // ================
                GameObject wallPrefab = wallRegistry.Get(wallKey);
                if (wallPrefab == null)
                    continue;

                // ================
                // SPAWN WALL
                // ================
                Transform wall = Instantiate(wallPrefab, transform).transform;
                wall.localPosition = new Vector3(i * moduleWidth, 0f, 0f);
                SpriteRenderer wallSR = wall.GetComponent<SpriteRenderer>();

                // ====================================
                // WINDOWS (ONLY FOR NON-ENTRANCE FLOORS)
                // ====================================
                if (!isEntrance && windowRegistry != null)
                {
                    bool isOpen = DetermineWindowOpenState(def, i);
                    string windowKey = "Window_" + def.WindowType + "_" + (isOpen ? "Open" : "Closed");

                    GameObject windowPrefab = windowRegistry.Get(windowKey);
                    if (windowPrefab != null)
                    {
                        Transform window = Instantiate(windowPrefab, wall).transform;
                        window.localPosition = new Vector3(halfWidth, 0f, 0f);

                        SpriteRenderer winSR = window.GetComponent<SpriteRenderer>();
                        if (winSR != null && wallSR != null)
                        {
                            winSR.sortingLayerID = wallSR.sortingLayerID;
                            winSR.sortingOrder = wallSR.sortingOrder + 1;
                        }
                    }
                }

                // ====================================
                // DOOR MODULE (NON-EMBEDDED DOORS ONLY)
                // ====================================
                if (isEntrance &&
                    !def.EntranceEmbeddedDoor &&                 // Not embedded
                    def.EntranceAddDoor &&                       // Door should spawn
                    i == def.EntranceDoorIndex)                  // Correct index
                {
                    string doorKey =
                        "Door_" +
                        def.EntranceDoorType + "_" +
                        def.EntranceDoorColor + "_" +
                        def.EntranceDoorSize;

                    GameObject doorPrefab = doorRegistry.Get(doorKey);
                    if (doorPrefab != null)
                    {
                        Transform door = Instantiate(doorPrefab, wall).transform;
                        door.localPosition = new Vector3(halfWidth, 0f, 0f);

                        SpriteRenderer doorSR = door.GetComponent<SpriteRenderer>();
                        if (doorSR != null && wallSR != null)
                        {
                            doorSR.sortingLayerID = wallSR.sortingLayerID;
                            doorSR.sortingOrder = wallSR.sortingOrder + 10;
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
                return Random.value > 0.5f;

            return false;
        }

        private string GetWallPosition(int index)
        {
            if (index == 0)
                return "Left";

            if (index == WidthModules - 1)
                return "Right";

            return "Middle";
        }

        private void ClearModules()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}
