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
    }
}