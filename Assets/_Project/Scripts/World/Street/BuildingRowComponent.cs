using System.Collections.Generic;
using UnityEngine;
using CityRush.World.Buildings.Data;
using CityRush.World.Buildings.Generation;
using CityRush.World.Addresses;
using CityRush.World.Interior;
using CityRush.World.Street.Data;
using CityRush.World.Street.Generation;

namespace CityRush.World.Street
{
    public class BuildingRowComponent : MonoBehaviour
    {
        [SerializeField] private BuildingGenerator buildingGeneratorPrefab;
        [SerializeField] private ParkGenerator parkGeneratorPrefab;
        [SerializeField] private Transform buildingsRoot;

        private BuildingDefinition[] buildings;
        private ParkDefinition[] parks;

        private const float ModuleWidth = 160f / 48f;

        public void SetBuildings(BuildingDefinition[] buildingDefinitions)
        {
            SetBuildings(buildingDefinitions, null);
        }

        public void SetBuildings(BuildingDefinition[] buildingDefinitions, ParkDefinition[] parkDefinitions)
        {
            buildings = buildingDefinitions;
            parks = parkDefinitions;
            RebuildRow();
        }

        private void RebuildRow()
        {
            Clear();

            if (buildings == null || buildingGeneratorPrefab == null)
                return;

            StreetAddressTag streetTag = GetComponentInParent<StreetAddressTag>();

            float currentX = 0f;

            Dictionary<int, List<ParkDefinition>> parksByAfterIndex = BuildParksLookup();

            // Optional: parks before the first building (AfterBuildingIndex = -1)
            if (parksByAfterIndex != null && parksByAfterIndex.TryGetValue(-1, out var preParks))
                SpawnParks(preParks, ref currentX);

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

                // Parks after this building index
                if (parksByAfterIndex != null && parksByAfterIndex.TryGetValue(i, out var afterParks))
                    SpawnParks(afterParks, ref currentX);
            }
        }

        private Dictionary<int, List<ParkDefinition>> BuildParksLookup()
        {
            if (parks == null || parks.Length == 0 || parkGeneratorPrefab == null)
                return null;

            var map = new Dictionary<int, List<ParkDefinition>>();

            for (int i = 0; i < parks.Length; i++)
            {
                ParkDefinition p = parks[i];
                if (p == null)
                    continue;

                if (p.WidthBlocks <= 0)
                    continue;

                if (!map.TryGetValue(p.AfterBuildingIndex, out var list))
                {
                    list = new List<ParkDefinition>();
                    map.Add(p.AfterBuildingIndex, list);
                }

                list.Add(p);
            }

            return map;
        }

        private void SpawnParks(List<ParkDefinition> list, ref float currentX)
        {
            if (list == null || list.Count == 0)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                ParkDefinition p = list[i];
                if (p == null || p.WidthBlocks <= 0)
                    continue;

                var park = Instantiate(parkGeneratorPrefab, buildingsRoot);
                park.transform.localPosition = new Vector3(currentX, 0f, 0f);

                park.Build(p);

                currentX += p.WidthBlocks * ModuleWidth;
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
