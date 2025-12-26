using UnityEngine;
using CityRush.World.Street.Data;
using CityRush.World.Street.Generation;

namespace CityRush.World.Street
{
    public class StreetComponent : MonoBehaviour
    {
        private Camera _camera;

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

        public float LeftBoundX { get; private set; }
        public float RightBoundX { get; private set; }
        private const int BLEED_TILES = 2;

        private void Start()
        {
            //if (!string.IsNullOrEmpty(streetJson))
            //    BuildStreetFromJson(streetJson);
        }

        public void Initialize(Camera camera)
        {
            _camera = camera;
        }

        public void Build(StreetLoadRequest request)
        {
            BuildStreetFromJson(request.StreetJson);
        }

        public void BuildStreetFromJson(string json)
        {
            streetJson = json;
            ParseStreetData();

            EnsureRoots();
            BuildRoad(streetData);
            BuildPavement(streetData);

            SetBoundaries();

            AssignBuildings();
        }

        private void ParseStreetData()
        {
            streetData = JsonUtility.FromJson<StreetData>(streetJson);
        }

        private float GetRoadBaseY()
        {
            return _camera.transform.position.y - _camera.orthographicSize;
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

        private void SetBoundaries()
        {
            int totalTiles = streetData.street.GetStreetWidthInTiles();
            float totalWidth = totalTiles * TILE_WIDTH;

            LeftBoundX = transform.position.x;
            RightBoundX = transform.position.x + totalWidth;
        }

        public float SpawnX
        {
            get
            {
                if (streetData == null || streetData.spawn == null)
                    return 0f;

                return streetData.spawn.x;
            }
        }

        //public void SetCamera(Camera camera)
        //{
        //    _camera = camera;
        //}
    }
}
