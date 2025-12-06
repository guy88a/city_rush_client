using UnityEngine;
using CityRush.World.Buildings.Data;

namespace CityRush.World.Buildings.Generation
{
    // BuildingGenerator
    // Runtime generator that stacks floors into a full building.

    public class BuildingGenerator : MonoBehaviour
    {
        [Header("Building Definition")]
        public BuildingDefinition Definition;

        [Header("Prefabs")]
        public GameObject EntranceFloorPrefab;
        public GameObject RegularFloorPrefab;
        public GameObject RooftopPrefab;

        [Header("Generated Output (Read Only)")]
        public Transform GeneratedRoot;

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

            // ENTRANCE FLOOR ---------------------------------------------------
            GameObject entrance = Instantiate(EntranceFloorPrefab, parent);
            entrance.transform.localPosition = Vector3.zero;

            FloorComponent entranceFloor = entrance.GetComponent<FloorComponent>();
            if (entranceFloor != null)
            {
                entranceFloor.WidthModules = Definition.Width;
                entranceFloor.Initialize(Definition, true);
            }

            // REGULAR FLOORS ---------------------------------------------------
            for (int i = 0; i < Definition.FloorsCount; i++)
            {
                GameObject floor = Instantiate(RegularFloorPrefab, parent);

                float y = (i + 1) * floorHeight;
                floor.transform.localPosition = new Vector3(0, y, 0);

                FloorComponent fc = floor.GetComponent<FloorComponent>();
                if (fc != null)
                {
                    fc.WidthModules = Definition.Width;
                    fc.Initialize(Definition, false);
                }
            }

            // ROOFTOP ----------------------------------------------------------
            if (RooftopPrefab != null)
            {
                float rooftopY = (Definition.FloorsCount + 1) * floorHeight;
                GameObject roof = Instantiate(RooftopPrefab, parent);
                roof.transform.localPosition = new Vector3(0, rooftopY, 0);
            }
        }

        private void ClearGenerated()
        {
            if (GeneratedRoot == null)
                return;

            for (int i = GeneratedRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = GeneratedRoot.GetChild(i);
                DestroyImmediate(child.gameObject);
            }
        }
    }
}
