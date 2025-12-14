using UnityEngine;
using CityRush.World.Buildings.Data;
using CityRush.World.Buildings.Generation;

namespace CityRush.World.Street
{
    public class BuildingRowComponent : MonoBehaviour
    {
        [SerializeField] private BuildingGenerator buildingGeneratorPrefab;
        [SerializeField] private Transform buildingsRoot;

        private BuildingDefinition[] buildings;

        private const float ModuleWidth = 160f / 48f;

        public void SetBuildings(BuildingDefinition[] buildingDefinitions)
        {
            buildings = buildingDefinitions;
            RebuildRow();
        }

        private void RebuildRow()
        {
            Clear();

            if (buildings == null || buildingGeneratorPrefab == null)
                return;

            float currentX = 0f;

            foreach (var definition in buildings)
            {
                var instance = Instantiate(buildingGeneratorPrefab, buildingsRoot);
                instance.transform.localPosition = new Vector3(currentX, 0f, 0f);

                instance.Build(definition);

                currentX += definition.Width * ModuleWidth;
            }
        }

        private void Clear()
        {
            if (buildingsRoot == null)
                return;

            for (int i = buildingsRoot.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(buildingsRoot.GetChild(i).gameObject);
            }
        }
    }
}
