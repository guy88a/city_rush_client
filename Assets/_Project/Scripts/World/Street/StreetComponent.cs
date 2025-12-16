using UnityEngine;
using CityRush.World.Street.Data;
using CityRush.World.Street.Generation;

namespace CityRush.World.Street
{
    public class StreetComponent : MonoBehaviour
    {
        [Header("Road")]
        [SerializeField] private GameObject[] roadTiles;
        [Header("Pavement")]
        [SerializeField] private GameObject[] pavementTiles;

        private const float TILE_WIDTH = 160f / 48f;
        private const float ROAD_HEIGHT = 253f / 48f;

        [TextArea(5, 20)]
        [SerializeField] private string streetJson;

        [SerializeField] private BuildingRowComponent buildingRow;

        private StreetData streetData;

        private Transform roadsRoot;
        private Transform pavementsRoot;

        private void Start()
        {
            if (!string.IsNullOrEmpty(streetJson))
                BuildStreetFromJson(streetJson);
        }

        public void BuildStreetFromJson(string json)
        {
            streetJson = json;
            ParseStreetData();

            EnsureRoots();
            BuildRoad(streetData);
            BuildPavement(streetData);

            AssignBuildings();
        }

        private void ParseStreetData()
        {
            streetData = JsonUtility.FromJson<StreetData>(streetJson);
        }
        
        private float GetRoadBaseY()
        {
            var cam = Camera.main;
            return cam.transform.position.y - cam.orthographicSize;
        }

        private void EnsureRoots()
        {
            roadsRoot = GetOrCreateChild("Roads");
            pavementsRoot = GetOrCreateChild("Pavements");
        }

        private Transform GetOrCreateChild(string name)
        {
            var child = transform.Find(name);
            if (child != null)
                return child;

            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        private void BuildRoad(StreetData data)
        {
            if (data == null || data.street == null || data.street.road == null)
                return;
            var builder = new RoadBuilder(
                roadsRoot,
                roadTiles,
                TILE_WIDTH,
                GetRoadBaseY()
            );
            builder.Build(data.street.road);
        }

        private void BuildPavement(StreetData data)
        {
            if (data == null || data.street == null || data.street.pavements == null)
                return;

            float pavementBaseY = GetRoadBaseY() + ROAD_HEIGHT;

            var builder = new PavementBuilder(
                pavementsRoot,
                pavementTiles,
                TILE_WIDTH,
                pavementBaseY
            );

            builder.Build(data.street.pavements);
        }

        private void AssignBuildings()
        {
            if (buildingRow == null || streetData == null)
                return;

            buildingRow.SetBuildings(streetData.buildings);
        }
    }
}
