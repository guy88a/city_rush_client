using System.Collections.Generic;
using CityRush.World.Map.Runtime;
using UnityEngine;

namespace CityRush.World.Addresses
{
    [DisallowMultipleComponent]
    public sealed class StreetAddressTag : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string streetId;
        [SerializeField] private int zoneIndex;
        [SerializeField] private int row;
        [SerializeField] private int col;

        [Header("Display")]
        [SerializeField] private string streetName;

        [Header("Building Numbering")]
        [SerializeField] private int buildingNumberBase = 1;
        [SerializeField] private int buildingNumberStep = 1;

        private readonly List<BuildingAddressTag> _buildings = new List<BuildingAddressTag>(32);

        public StreetAddress Address => new StreetAddress(new MapPosition(zoneIndex, row, col), streetId);

        public string StreetId => streetId;
        public string StreetName => streetName;

        public int BuildingNumberBase => buildingNumberBase;
        public int BuildingNumberStep => buildingNumberStep;

        public IReadOnlyList<BuildingAddressTag> Buildings => _buildings;

        public void Set(
            MapPosition position,
            string id,
            string displayName = null,
            int buildingBase = 1,
            int buildingStep = 1
        )
        {
            streetId = id ?? string.Empty;
            zoneIndex = position.ZoneIndex;
            row = position.Row;
            col = position.Col;

            streetName = string.IsNullOrWhiteSpace(displayName) ? streetId : displayName;

            buildingNumberBase = buildingBase;
            buildingNumberStep = buildingStep == 0 ? 1 : buildingStep;

            _buildings.Clear();
        }

        public int ResolveBuildingNumber(int buildingIndex)
        {
            return buildingNumberBase + (buildingIndex * buildingNumberStep);
        }

        public void RegisterBuilding(BuildingAddressTag tag)
        {
            if (tag == null)
                return;

            _buildings.Add(tag);
        }

        public bool TryGetBuildingByIndex(int buildingIndex, out BuildingAddressTag tag)
        {
            tag = null;

            if (buildingIndex < 0)
                return false;

            for (int i = 0; i < _buildings.Count; i++)
            {
                BuildingAddressTag b = _buildings[i];
                if (b != null && b.BuildingIndex == buildingIndex)
                {
                    tag = b;
                    return true;
                }
            }

            return false;
        }
    }
}