using UnityEngine;
using CityRush.World.Street.Data;

namespace CityRush.World.Street
{
    public class StreetComponent : MonoBehaviour
    {
        [TextArea(5, 20)]
        [SerializeField] private string streetJson;

        [SerializeField] private BuildingRowComponent buildingRow;

        private StreetData streetData;

        private void Start()
        {
            if (!string.IsNullOrEmpty(streetJson))
                BuildStreetFromJson(streetJson);
        }

        public void BuildStreetFromJson(string json)
        {
            streetJson = json;
            ParseStreetData();
            AssignBuildings();
        }

        private void ParseStreetData()
        {
            streetData = JsonUtility.FromJson<StreetData>(streetJson);
        }

        private void AssignBuildings()
        {
            if (buildingRow == null || streetData == null)
                return;

            buildingRow.SetBuildings(streetData.buildings);
        }
    }
}
