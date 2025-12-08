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
            // ROOFTOP
            // -----------------------------------------------------
            if (RooftopPrefab != null)
            {
                float y = (Definition.FloorsCount + 1) * floorHeight;

                GameObject roof = Instantiate(RooftopPrefab, parent);
                float halfWidth = (160f / 48f) * 0.5f; // ~1.6667
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

        private void ClearGenerated()
        {
            if (GeneratedRoot == null)
                return;

            for (int i = GeneratedRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(GeneratedRoot.GetChild(i).gameObject);
        }
    }
}
