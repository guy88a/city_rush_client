using UnityEngine;

namespace CityRush.World.Interior
{
    public sealed class ApartmentDoor : Door
    {
        [SerializeField] private string apartmentId;

        public string ApartmentId => apartmentId;

        public void SetApartmentId(string id)
        {
            apartmentId = id ?? string.Empty;
        }

        public override void Enter(GameObject player)
        {
            // TODO: Corridor -> Apartment
        }

        public string GetDisplayAddress()
        {
            var tag = GetComponent<CityRush.World.Addresses.ApartmentAddressTag>();
            if (tag == null)
                tag = GetComponentInParent<CityRush.World.Addresses.ApartmentAddressTag>();

            return tag != null ? tag.GetDisplayAddress() : string.Empty;
        }
    }
}