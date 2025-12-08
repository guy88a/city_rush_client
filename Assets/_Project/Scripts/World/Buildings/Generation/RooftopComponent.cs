using UnityEngine;
using CityRush.World.Buildings.Data;
using CityRush.World.Buildings.Registry;

namespace CityRush.World.Buildings.Generation
{
    public class RooftopComponent : MonoBehaviour
    {
        public RoofRegistry roofRegistry;
        public int WidthModules = 3;

        public void Initialize(BuildingDefinition definition)
        {
            ClearModules();
            Build(definition);
        }

        private void Build(BuildingDefinition definition)
        {
            if (roofRegistry == null)
                return;

            // No chimney requested
            if (string.IsNullOrEmpty(definition.RooftopType) ||
                string.IsNullOrEmpty(definition.RooftopColor))
                return;

            // Build key
            string key = "Chimney_" +
                         definition.RooftopType + "_" +
                         definition.RooftopColor;

            GameObject prefab = roofRegistry.Get(key);
            if (prefab == null)
                return;

            // Clamp index safety
            int index = Mathf.Clamp(definition.RooftopPattern, 0, WidthModules - 1);

            float moduleWidth = 160f / 48f;
            float x = index * moduleWidth;

            // Spawn chimney
            Transform chimney = Instantiate(prefab, transform).transform;
            chimney.localPosition = new Vector3(x, 0f, 0f);
        }

        private void ClearModules()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}
