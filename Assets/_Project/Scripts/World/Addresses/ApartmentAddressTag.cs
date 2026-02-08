using UnityEngine;

namespace CityRush.World.Addresses
{
    [DisallowMultipleComponent]
    public sealed class ApartmentAddressTag : MonoBehaviour
    {
        [SerializeField] private BuildingAddressTag building;

        [Header("Identity")]
        [SerializeField] private int floorIndex;
        [SerializeField] private int apartmentIndexOnFloor;
        [SerializeField] private int apartmentNumber;

        public BuildingAddressTag Building => building;

        public int FloorIndex => floorIndex;
        public int ApartmentIndexOnFloor => apartmentIndexOnFloor;
        public int ApartmentNumber => apartmentNumber;

        public ApartmentAddress Address
        {
            get
            {
                if (building == null)
                    return default;

                int apf = Mathf.Max(1, building.ApartmentsPerFloor);
                return new ApartmentAddress(building.Address, floorIndex, apartmentIndexOnFloor, apf);
            }
        }

        public void Set(BuildingAddressTag buildingTag, int floorIndex, int apartmentIndexOnFloor)
        {
            building = buildingTag;
            this.floorIndex = Mathf.Max(0, floorIndex);
            this.apartmentIndexOnFloor = Mathf.Max(0, apartmentIndexOnFloor);

            int apf = buildingTag != null ? Mathf.Max(1, buildingTag.ApartmentsPerFloor) : 1;
            apartmentNumber = (this.floorIndex * apf) + this.apartmentIndexOnFloor + 1;
        }

        public string GetDisplayAddress()
        {
            if (building == null)
                return $"Apartment {apartmentNumber}";

            return $"{building.GetDisplayAddress()}, apartment {apartmentNumber}";
        }
    }
}