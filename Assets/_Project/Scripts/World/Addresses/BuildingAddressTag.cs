using UnityEngine;

namespace CityRush.World.Addresses
{
    [DisallowMultipleComponent]
    public sealed class BuildingAddressTag : MonoBehaviour
    {
        [SerializeField] private StreetAddressTag street;

        [Header("Identity")]
        [SerializeField] private int buildingIndex;
        [SerializeField] private int buildingNumber;

        [Header("Building Metrics")]
        [SerializeField] private int apartmentsPerFloor;
        [SerializeField] private int floorsCount;

        public StreetAddressTag Street => street;

        public int BuildingIndex => buildingIndex;
        public int BuildingNumber => buildingNumber;

        public int ApartmentsPerFloor => apartmentsPerFloor;
        public int FloorsCount => floorsCount;

        public BuildingAddress Address
        {
            get
            {
                StreetAddress streetAddress = street != null ? street.Address : default;
                return new BuildingAddress(streetAddress, buildingIndex, buildingNumber);
            }
        }

        public void Set(
            StreetAddressTag streetTag,
            int index,
            int number,
            int apartmentsPerFloor,
            int floorsCount
        )
        {
            street = streetTag;
            buildingIndex = index;
            buildingNumber = number;

            this.apartmentsPerFloor = Mathf.Max(1, apartmentsPerFloor);
            this.floorsCount = Mathf.Max(0, floorsCount);
        }

        public string GetDisplayAddress()
        {
            string stName = street != null ? street.StreetName : "";
            return string.IsNullOrWhiteSpace(stName)
                ? $"Building {buildingNumber}"
                : $"{stName} {buildingNumber}";
        }

        public ApartmentAddress MakeApartmentAddress(int floorIndex, int apartmentIndexOnFloor)
        {
            return new ApartmentAddress(Address, floorIndex, apartmentIndexOnFloor, apartmentsPerFloor);
        }
    }
}