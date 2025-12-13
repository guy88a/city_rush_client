using UnityEngine;
using CityRush.World.Street.Data;

namespace CityRush.World.Street
{
    public class StreetComponent : MonoBehaviour
    {
        [TextArea(5, 20)]
        [SerializeField] private string streetJson;

        private StreetData streetData;

        public void BuildStreetFromJson(string json)
        {
            streetJson = json;
            ParseStreetData();
        }

        private void ParseStreetData()
        {
            streetData = JsonUtility.FromJson<StreetData>(streetJson);
        }
    }
}
