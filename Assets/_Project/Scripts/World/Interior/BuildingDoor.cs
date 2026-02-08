using UnityEngine;

namespace CityRush.World.Interior
{
    public sealed class BuildingDoor : Door
    {
        [SerializeField] private string buildingId;

        public string BuildingId => buildingId;

        public void SetBuildingId(string id)
        {
            buildingId = id ?? string.Empty;
        }

        public override void Enter(GameObject player)
        {
            // TODO: Street -> Corridor
        }

        public string GetDisplayAddress()
        {
            var tag = GetComponent<CityRush.World.Addresses.BuildingAddressTag>();
            if (tag == null)
                tag = GetComponentInParent<CityRush.World.Addresses.BuildingAddressTag>();

            return tag != null ? tag.GetDisplayAddress() : string.Empty;
        }
    }
}