using CityRush.World.Addresses;
using UnityEngine;

namespace CityRush.Units
{
    [DisallowMultipleComponent]
    public sealed class NpcHomeAddress : MonoBehaviour
    {
        [Header("Street")]
        [SerializeField] private string streetId;
        [SerializeField] private int zoneIndex;
        [SerializeField] private int row;
        [SerializeField] private int col;

        [Header("Home")]
        [SerializeField] private int buildingIndex;
        [SerializeField] private int floorIndex;
        [SerializeField] private int apartmentIndex;

        [Header("Display")]
        [SerializeField] private string formattedAddress;

        public string StreetId => streetId;
        public int BuildingIndex => buildingIndex;
        public int FloorIndex => floorIndex;
        public int ApartmentIndex => apartmentIndex;
        public string FormattedAddress => formattedAddress;

        public void Set(
            StreetAddressTag streetTag,
            int buildingIndex,
            int floorIndex,
            int apartmentIndex,
            string formattedOverride = null
        )
        {
            if (streetTag != null)
            {
                var streetAddr = streetTag.Address;
                streetId = streetAddr.StreetId;

                zoneIndex = streetAddr.Position.ZoneIndex;
                row = streetAddr.Position.Row;
                col = streetAddr.Position.Col;
            }
            else
            {
                streetId = string.Empty;
                zoneIndex = row = col = 0;
            }

            this.buildingIndex = buildingIndex;
            this.floorIndex = Mathf.Max(0, floorIndex);
            this.apartmentIndex = Mathf.Max(0, apartmentIndex);

            if (!string.IsNullOrWhiteSpace(formattedOverride))
            {
                formattedAddress = formattedOverride;
                return;
            }

            formattedAddress = "";

            if (streetTag != null && TryResolveBuildingTag(streetTag, buildingIndex, out var b) && b != null)
            {
                int apf = Mathf.Max(1, b.ApartmentsPerFloor);
                int aptNumber = (this.floorIndex * apf) + this.apartmentIndex + 1;
                formattedAddress = $"{b.GetDisplayAddress()}, apartment {aptNumber}";
            }
        }

        private static bool TryResolveBuildingTag(StreetAddressTag streetTag, int buildingIndex, out BuildingAddressTag buildingTag)
        {
            buildingTag = null;

            if (streetTag == null)
                return false;

            if (streetTag.TryGetBuildingByIndex(buildingIndex, out buildingTag) && buildingTag != null)
                return true;

            var all = streetTag.GetComponentsInChildren<BuildingAddressTag>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var b = all[i];
                if (b != null && b.BuildingIndex == buildingIndex)
                {
                    buildingTag = b;
                    return true;
                }
            }

            return false;
        }
    }
}
