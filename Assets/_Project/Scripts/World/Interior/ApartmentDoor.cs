using UnityEngine;

namespace CityRush.World.Interior
{
    public sealed class ApartmentDoor : Door
    {
        [SerializeField] private string apartmentId;

        public string ApartmentId => apartmentId;

        public override void Enter(GameObject player)
        {
            // TODO: Corridor -> Apartment
        }
    }
}
