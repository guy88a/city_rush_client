using UnityEngine;
using CityRush.World.Buildings.Data;

namespace CityRush.World.Street
{
    public class BuildingRowComponent : MonoBehaviour
    {
        // Ordered building definitions (left > right)
        private BuildingDefinition[] buildings;

        // Entry point (called by StreetComponent later)
        public void SetBuildings(BuildingDefinition[] buildingDefinitions)
        {
            buildings = buildingDefinitions;
        }
    }
}
