using UnityEngine;
using CityRush.World.Buildings.Data;
using CityRush.World.Buildings.Generation;
using CityRush.World.Addresses;
using CityRush.World.Interior;

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

            StreetAddressTag streetTag = GetComponentInParent<StreetAddressTag>();

            float currentX = 0f;

            for (int i = 0; i < buildings.Length; i++)
            {
                BuildingDefinition definition = buildings[i];

                var instance = Instantiate(buildingGeneratorPrefab, buildingsRoot);
                instance.transform.localPosition = new Vector3(currentX, 0f, 0f);

                instance.Build(definition);

                // Address tag (deterministic by build order)
                BuildingAddressTag buildingTag = instance.GetComponent<BuildingAddressTag>();
                if (buildingTag == null)
                    buildingTag = instance.gameObject.AddComponent<BuildingAddressTag>();

                int buildingNumber = streetTag != null
                    ? streetTag.ResolveBuildingNumber(i)
                    : (i + 1);

                buildingTag.Set(
                    streetTag,
                    index: i,
                    number: buildingNumber,
                    apartmentsPerFloor: definition.Width,
                    floorsCount: definition.FloorsCount
                );

                streetTag?.RegisterBuilding(buildingTag);

                BuildingDoor[] buildingDoors = instance.GetComponentsInChildren<BuildingDoor>(true);
                for (int d = 0; d < buildingDoors.Length; d++)
                {
                    BuildingDoor bd = buildingDoors[d];
                    if (bd == null)
                        continue;

                    // Make sure the door itself has a BuildingAddressTag too (so GetComponentInParent works reliably)
                    BuildingAddressTag doorTag = bd.GetComponent<BuildingAddressTag>();
                    if (doorTag == null)
                        doorTag = bd.gameObject.AddComponent<BuildingAddressTag>();

                    doorTag.Set(
                        streetTag,
                        index: i,
                        number: buildingNumber,
                        apartmentsPerFloor: definition.Width,
                        floorsCount: definition.FloorsCount
                    );

                    // Populate serialized IDs
                    bd.SetBuildingId(buildingNumber.ToString());
                    bd.SetDoorId($"{streetTag.Address}|B{i}");
                }

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