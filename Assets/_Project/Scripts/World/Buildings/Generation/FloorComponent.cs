using UnityEngine;
using CityRush.World.Buildings.Data;

namespace CityRush.World.Buildings.Generation
{
    public class FloorComponent : MonoBehaviour
    {
        [Header("Wall Prefabs (Regular)")]
        public GameObject WallLeftPrefab;
        public GameObject WallMiddlePrefab;
        public GameObject WallRightPrefab;

        [Header("Wall Prefabs (Entrance)")]
        public GameObject EntranceWallLeftPrefab;
        public GameObject EntranceWallMiddlePrefab;
        public GameObject EntranceWallRightPrefab;

        [Header("Windows")]
        public GameObject WindowClosedPrefab;
        public GameObject WindowOpenPrefab;

        [Header("Door Prefabs")]
        public GameObject DoorPrefabWood;
        public GameObject DoorPrefabMetal;
        public GameObject DoorPrefabGlass;

        [Header("Entrance Decorations")]
        public GameObject EntranceSideDecorationLeft;
        public GameObject EntranceSideDecorationRight;

        [Header("Pattern Settings")]
        public int WidthModules = 3;

        public void Initialize(BuildingDefinition def, bool isEntrance)
        {
            ClearModules();
            Build(def, isEntrance);
        }

        private void Build(BuildingDefinition def, bool isEntrance)
        {
            float moduleWidth = 160f / 48f;

            // Window pattern resolve
            string pattern = "";

            if (def.WindowsRandomPattern)
            {
                for (int i = 0; i < WidthModules; i++)
                    pattern += (Random.Range(0, 2) == 1) ? "1" : "0";
            }
            else
            {
                pattern = def.WindowsForcedPattern;

                if (pattern.Length < WidthModules)
                    pattern = pattern.PadRight(WidthModules, '0');
                else if (pattern.Length > WidthModules)
                    pattern = pattern.Substring(0, WidthModules);
            }

            for (int i = 0; i < WidthModules; i++)
            {
                bool isLeft = (i == 0);
                bool isRight = (i == WidthModules - 1);

                GameObject wallPrefab = null;

                // ----------------------------------------------------------------------
                // WALL SELECTION
                // ----------------------------------------------------------------------
                if (isEntrance)
                {
                    if (isLeft) wallPrefab = EntranceWallLeftPrefab;
                    else if (isRight) wallPrefab = EntranceWallRightPrefab;
                    else wallPrefab = EntranceWallMiddlePrefab;
                }
                else
                {
                    if (isLeft) wallPrefab = WallLeftPrefab;
                    else if (isRight) wallPrefab = WallRightPrefab;
                    else wallPrefab = WallMiddlePrefab;
                }

                if (wallPrefab == null)
                    continue;

                GameObject wall = Instantiate(wallPrefab, transform);
                wall.transform.localPosition = new Vector3(i * moduleWidth, 0, 0);

                // ----------------------------------------------------------------------
                // WINDOWS (Entrance may or may not have windows)
                // ----------------------------------------------------------------------
                if (!isEntrance) // entrance handled separately later
                {
                    bool open = (pattern[i] == '1');
                    GameObject winPrefab = open ? WindowOpenPrefab : WindowClosedPrefab;

                    if (winPrefab != null)
                    {
                        GameObject window = Instantiate(winPrefab, transform);
                        window.transform.localPosition = new Vector3(i * moduleWidth, 0, 0);
                    }
                }

                // ----------------------------------------------------------------------
                // DOOR FOR ENTRANCE
                // ----------------------------------------------------------------------
                if (isEntrance && def.EntranceAddDoor)
                {
                    if (i == 1) // door always centered (middle module)
                    {
                        GameObject doorPrefab = null;

                        if (def.EntranceDoorType == "Wood") doorPrefab = DoorPrefabWood;
                        else if (def.EntranceDoorType == "Metal") doorPrefab = DoorPrefabMetal;
                        else if (def.EntranceDoorType == "Glass") doorPrefab = DoorPrefabGlass;

                        if (doorPrefab != null)
                        {
                            GameObject door = Instantiate(doorPrefab, transform);
                            door.transform.localPosition = new Vector3(i * moduleWidth, 0, 0);
                        }
                    }
                }
            }

            // --------------------------------------------------------------------------
            // ENTRANCE SIDE DECORATIONS
            // --------------------------------------------------------------------------
            if (isEntrance && def.EntranceDecoration)
            {
                if (EntranceSideDecorationLeft != null)
                {
                    GameObject leftDeco = Instantiate(EntranceSideDecorationLeft, transform);
                    leftDeco.transform.localPosition = Vector3.zero;
                }

                if (EntranceSideDecorationRight != null)
                {
                    GameObject rightDeco = Instantiate(EntranceSideDecorationRight, transform);
                    rightDeco.transform.localPosition = new Vector3((WidthModules - 1) * (160f / 48f), 0, 0);
                }
            }
        }

        private void ClearModules()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}
