using UnityEngine;
using CityRush.World.Buildings.Data;
using CityRush.World.Buildings.Registry;

namespace CityRush.World.Buildings.Generation
{
    public class BuildingGenerator : MonoBehaviour
    {
        [Header("Building Definition")]
        public BuildingDefinition Definition;

        [Header("Prefabs")]
        public GameObject EntranceFloorPrefab;
        public GameObject RegularFloorPrefab;
        public GameObject RooftopPrefab;

        [Header("Registries")]
        public WallRegistry wallRegistry;
        public WindowRegistry windowRegistry;
        public DoorRegistry doorRegistry;
        public RoofRegistry roofRegistry;
        public SeparatorRegistry separatorRegistry;   // NEW

        [Header("Generated Output (Read Only)")]
        public Transform GeneratedRoot;

        private void Start()
        {
            Generate();
        }

        private void Reset()
        {
            if (GeneratedRoot == null)
            {
                GameObject go = new GameObject("Building");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                GeneratedRoot = go.transform;
            }
        }

        public void Generate()
        {
            ClearGenerated();

            if (EntranceFloorPrefab == null || RegularFloorPrefab == null)
                return;

            float floorHeight = 260f / 48f;
            float moduleWidth = 160f / 48f;
            Transform parent = GeneratedRoot;

            // -----------------------------------------------------
            // ENTRANCE FLOOR
            // -----------------------------------------------------
            GameObject entrance = Instantiate(EntranceFloorPrefab, parent);
            entrance.transform.localPosition = Vector3.zero;

            FloorComponent entranceFloor = entrance.GetComponent<FloorComponent>();
            if (entranceFloor != null)
            {
                entranceFloor.wallRegistry = wallRegistry;
                entranceFloor.windowRegistry = windowRegistry;
                entranceFloor.doorRegistry = doorRegistry;

                entranceFloor.WidthModules = Definition.Width;
                entranceFloor.Initialize(Definition, true); // isEntrance = true
            }

            // -----------------------------------------------------
            // REGULAR FLOORS
            // -----------------------------------------------------
            for (int i = 0; i < Definition.FloorsCount; i++)
            {
                float y = (i + 1) * floorHeight;

                GameObject floor = Instantiate(RegularFloorPrefab, parent);
                floor.transform.localPosition = new Vector3(0, y, 0);

                FloorComponent fc = floor.GetComponent<FloorComponent>();
                if (fc != null)
                {
                    fc.wallRegistry = wallRegistry;
                    fc.windowRegistry = windowRegistry;
                    fc.doorRegistry = doorRegistry;

                    fc.WidthModules = Definition.Width;
                    fc.Initialize(Definition, false); // isEntrance = false
                }
            }

            // -----------------------------------------------------
            // FLOOR SEPARATORS (between floors)
            // -----------------------------------------------------
            if (Definition.UseSeparators && separatorRegistry != null)
            {
                // between: entrance <-> floor0, floor0 <-> floor1, ..., floorN-2 <-> floorN-1
                for (int i = 0; i < Definition.FloorsCount; i++)
                {
                    float y = (i + 1) * floorHeight;
                    SpawnSeparatorRow(Definition, parent, y, moduleWidth);
                }
            }

            // -----------------------------------------------------
            // ROOFTOP
            // -----------------------------------------------------
            if (RooftopPrefab != null)
            {
                float y = (Definition.FloorsCount + 1) * floorHeight;

                GameObject roof = Instantiate(RooftopPrefab, parent);
                float halfWidth = moduleWidth * 0.5f; // ~1.6667
                roof.transform.localPosition = new Vector3(halfWidth, y, 0);

                RooftopComponent rc = roof.GetComponent<RooftopComponent>();
                if (rc != null)
                {
                    rc.roofRegistry = roofRegistry;
                    rc.WidthModules = Definition.Width;
                    rc.Initialize(Definition);
                }
            }
        }

        private void SpawnSeparatorRow(BuildingDefinition def, Transform parent, float y, float moduleWidth)
        {
            if (string.IsNullOrEmpty(def.SeparatorType))
                return;

            for (int i = 0; i < def.Width; i++)
            {
                string positionSuffix;

                if (i == 0)
                    positionSuffix = "Left_WW";
                else if (i == def.Width - 1)
                    positionSuffix = "Right_WW";
                else
                    positionSuffix = "Middle";

                string key = "Separator_" + def.SeparatorType + "_" + positionSuffix;

                GameObject sepPrefab = separatorRegistry.Get(key);
                if (sepPrefab == null)
                    continue;

                Transform sep = Instantiate(sepPrefab, parent).transform;
                SpriteRenderer sr = sep.GetComponent<SpriteRenderer>();
                float sepWidth = sr.bounds.size.x;
                float wallWidth = moduleWidth;

                float x = 0f;

                if (i == 0) // LEFT separator
                {
                    x = -(sepWidth - wallWidth);
                }
                else // Middle separators
                {
                    x = i * wallWidth;
                }

                sep.localPosition = new Vector3(x, y, 0f);

                // Sorting: between walls and windows
                //sr = sep.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // assuming walls at 0, windows at +2 (see tweak below)
                    sr.sortingOrder = 1;
                }
            }
        }

        private void ClearGenerated()
        {
            if (GeneratedRoot == null)
                return;

            for (int i = GeneratedRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(GeneratedRoot.GetChild(i).gameObject);
        }
    }
}
