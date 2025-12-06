using UnityEngine;
using CityRush.World.Buildings.Data;

namespace CityRush.World.Buildings.Generation
{
    // FloorComponent
    // Builds a floor using walls and window variants based on pattern.

    public class FloorComponent : MonoBehaviour
    {
        [Header("Module Prefabs")]
        public GameObject WallLeftPrefab;
        public GameObject WallMiddlePrefab;
        public GameObject WallRightPrefab;

        [Header("Window Prefabs")]
        public GameObject WindowClosedPrefab;
        public GameObject WindowOpenPrefab;

        [Header("Pattern Settings")]
        public int WidthModules = 3;

        public void Initialize(BuildingDefinition definition)
        {
            ClearModules();
            Build(definition);
        }

        private void Build(BuildingDefinition definition)
        {
            float moduleWidth = 160f / 48f;

            // Resolve window pattern
            string pattern = "";

            if (definition.WindowsRandomPattern)
            {
                for (int i = 0; i < WidthModules; i++)
                    pattern += (Random.Range(0, 2) == 1) ? "1" : "0";
            }
            else
            {
                pattern = definition.WindowsForcedPattern;

                // Safety: pad or trim pattern to match width
                if (pattern.Length < WidthModules)
                {
                    pattern = pattern.PadRight(WidthModules, '0');
                }
                else if (pattern.Length > WidthModules)
                {
                    pattern = pattern.Substring(0, WidthModules);
                }
            }

            for (int i = 0; i < WidthModules; i++)
            {
                bool isLeft = (i == 0);
                bool isRight = (i == WidthModules - 1);

                GameObject prefabToUse = null;

                if (isLeft)
                    prefabToUse = WallLeftPrefab;
                else if (isRight)
                    prefabToUse = WallRightPrefab;
                else
                    prefabToUse = WallMiddlePrefab;

                if (prefabToUse == null)
                    continue;

                // Wall module
                GameObject module = Instantiate(prefabToUse, transform);
                module.transform.localPosition = new Vector3(i * moduleWidth, 0, 0);

                // Window variant
                bool open = (pattern[i] == '1');
                GameObject winPrefab = open ? WindowOpenPrefab : WindowClosedPrefab;

                if (winPrefab != null)
                {
                    GameObject window = Instantiate(winPrefab, transform);
                    window.transform.localPosition = new Vector3(i * moduleWidth, 0, 0);
                }
            }
        }

        private void ClearModules()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
    }
}
