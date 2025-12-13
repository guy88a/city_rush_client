using UnityEngine;
using CityRush.World.Buildings.Data;
using CityRush.World.Buildings.Registry;

namespace CityRush.World.Buildings.Generation
{
    public class BuildingGenerator : MonoBehaviour
    {
        [Header("Building Definition")]
        public BuildingDefinition Definition;

        [Header("Prefabs")]
        public GameObject EntranceFloorPrefab;
        public GameObject RegularFloorPrefab;
        public GameObject RooftopPrefab;

        [Header("Registries")]
        public WallRegistry wallRegistry;
        public WindowRegistry windowRegistry;
        public DoorRegistry doorRegistry;
        public RoofRegistry roofRegistry;
        public SeparatorRegistry separatorRegistry;
        public RooftopSeparatorRegistry rooftopSeparatorRegistry;

        [Header("Generated Output (Read Only)")]
        public Transform GeneratedRoot;

        #region Metrics

        private static class Metrics
        {
            public const float PPU = 48f;

            public const float ModuleWidthPx = 160f;
            public const float FloorHeightPx = 260f;

            public const float ModuleWidth = ModuleWidthPx / PPU;
            public const float FloorHeight = FloorHeightPx / PPU;

            public const float RooftopWallHeightFactor = 0.2f;
            public const float RooftopWallWidthFactor = 0.9f;
            public const float RooftopDecorationWidthFactor = 1.05f;
        }

        #endregion

        #region Unity

        private void Start()
        {
            Generate();
        }

        private void Reset()
        {
            if (GeneratedRoot != null)
                return;

            GameObject go = new GameObject("Building");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            GeneratedRoot = go.transform;
        }

        #endregion

        #region Public API

        public void Generate()
        {
            ClearGenerated();

            if (EntranceFloorPrefab == null || RegularFloorPrefab == null)
                return;

            Transform parent = GeneratedRoot;

            SpawnEntranceFloor(parent);
            SpawnRegularFloors(parent);
            SpawnFloorSeparators(parent);
            SpawnRooftopSeparator(parent);
            SpawnRooftopWall(parent);
            SpawnRooftop(parent);
        }

        #endregion

        #region Floors

        private void SpawnEntranceFloor(Transform parent)
        {
            GameObject entrance = Instantiate(EntranceFloorPrefab, parent);
            entrance.transform.localPosition = Vector3.zero;

            FloorComponent fc = entrance.GetComponent<FloorComponent>();
            if (fc == null)
                return;

            AssignFloorRegistries(fc);
            fc.WidthModules = Definition.Width;
            fc.Initialize(Definition, true);
        }

        private void SpawnRegularFloors(Transform parent)
        {
            for (int i = 0; i < Definition.FloorsCount; i++)
            {
                float y = (i + 1) * Metrics.FloorHeight;

                GameObject floor = Instantiate(RegularFloorPrefab, parent);
                floor.transform.localPosition = new Vector3(0, y, 0);

                FloorComponent fc = floor.GetComponent<FloorComponent>();
                if (fc == null)
                    continue;

                AssignFloorRegistries(fc);
                fc.WidthModules = Definition.Width;
                fc.Initialize(Definition, false);
            }
        }

        private void AssignFloorRegistries(FloorComponent fc)
        {
            fc.wallRegistry = wallRegistry;
            fc.windowRegistry = windowRegistry;
            fc.doorRegistry = doorRegistry;
        }

        #endregion

        #region Separators

        private void SpawnFloorSeparators(Transform parent)
        {
            if (!Definition.UseSeparators || separatorRegistry == null)
                return;

            for (int i = 0; i < Definition.FloorsCount; i++)
            {
                float y = (i + 1) * Metrics.FloorHeight;
                SpawnSeparatorRow(parent, y);
            }
        }

        private void SpawnSeparatorRow(Transform parent, float y)
        {
            if (string.IsNullOrEmpty(Definition.SeparatorType))
                return;

            for (int i = 0; i < Definition.Width; i++)
            {
                Segment segment = ResolveSegment(i, Definition.Width);

                string suffix =
                    segment == Segment.Left ? "Left_WW" :
                    segment == Segment.Right ? "Right_WW" :
                    "Middle";

                string key = $"Separator_{Definition.SeparatorType}_{suffix}";
                GameObject prefab = separatorRegistry.Get(key);

                if (prefab == null)
                    continue;

                Transform sep = Instantiate(prefab, parent).transform;
                SpriteRenderer sr = sep.GetComponent<SpriteRenderer>();

                float sepWidth = sr.bounds.size.x;
                float x = segment == Segment.Left
                    ? -(sepWidth - Metrics.ModuleWidth)
                    : i * Metrics.ModuleWidth;

                sep.localPosition = new Vector3(x, y, 0f);
                sr.sortingOrder = BuildingSorting.Separators;
            }
        }

        private void SpawnRooftopSeparator(Transform parent)
        {
            if (!Definition.UseRooftopSeparator || rooftopSeparatorRegistry == null)
                return;

            float y = (Definition.FloorsCount + 1) * Metrics.FloorHeight;

            for (int i = 0; i < Definition.Width; i++)
            {
                Segment segment = ResolveSegment(i, Definition.Width);
                string baseKey = $"Rooftop_Separator_{Definition.RooftopSeparatorType}_";
                string key = null;

                if (segment == Segment.Left)
                {
                    key = baseKey + "Left_WW";
                    if (rooftopSeparatorRegistry.Get(key) == null)
                        key = baseKey + "Left";
                }
                else if (segment == Segment.Right)
                {
                    key = baseKey + "Right_WW";
                    if (rooftopSeparatorRegistry.Get(key) == null)
                        key = baseKey + "Right";
                }
                else
                {
                    key = baseKey + "Middle";
                }

                GameObject prefab = rooftopSeparatorRegistry.Get(key);
                if (prefab == null)
                    continue;

                Transform sep = Instantiate(prefab, parent).transform;
                SpriteRenderer sr = sep.GetComponent<SpriteRenderer>();

                float sepWidth = sr.bounds.size.x;
                float x = segment == Segment.Left
                    ? -(sepWidth - Metrics.ModuleWidth)
                    : i * Metrics.ModuleWidth;

                sep.localPosition = new Vector3(x, y, 0f);
                sr.sortingOrder = BuildingSorting.RoofSep;
            }
        }

        #endregion

        #region Rooftop Wall

        private void SpawnRooftopWall(Transform parent)
        {
            float y = (Definition.FloorsCount + 1) * Metrics.FloorHeight;

            string leftKey = $"Wall_{Definition.WallType}_{Definition.WallColor}_Left";
            string middleKey = $"Wall_{Definition.WallType}_{Definition.WallColor}_Middle";
            string rightKey = $"Wall_{Definition.WallType}_{Definition.WallColor}_Right";

            GameObject leftPrefab = wallRegistry.Get(leftKey);
            GameObject middlePrefab = wallRegistry.Get(middleKey);
            GameObject rightPrefab = wallRegistry.Get(rightKey);

            if (!leftPrefab || !middlePrefab || !rightPrefab)
                return;

            SpriteRenderer leftSrc = leftPrefab.GetComponent<SpriteRenderer>();
            SpriteRenderer middleSrc = middlePrefab.GetComponent<SpriteRenderer>();
            SpriteRenderer rightSrc = rightPrefab.GetComponent<SpriteRenderer>();

            float totalWidth = Definition.Width * Metrics.ModuleWidth;
            float rooftopWidth = totalWidth * Metrics.RooftopWallWidthFactor;
            float offsetX = (totalWidth - rooftopWidth) * 0.5f;

            float leftWidth = leftSrc.bounds.size.x;
            float rightWidth = rightSrc.bounds.size.x;
            float middleWidth = rooftopWidth - leftWidth - rightWidth;

            if (middleWidth <= 0f)
                return;

            float wallHeight = middleSrc.bounds.size.y * Metrics.RooftopWallHeightFactor;

            SpawnRooftopWallDecoration(parent, y + wallHeight, rooftopWidth);

            CreateTiledSegment("RooftopWall_Left", parent,
                new Vector3(offsetX, y, 0), leftSrc,
                new Vector2(leftWidth, wallHeight),
                BuildingSorting.RooftopWall);

            CreateTiledSegment("RooftopWall_Middle", parent,
                new Vector3(offsetX + leftWidth, y, 0), middleSrc,
                new Vector2(middleWidth, wallHeight),
                BuildingSorting.RooftopWall);

            CreateTiledSegment("RooftopWall_Right", parent,
                new Vector3(offsetX + leftWidth + middleWidth, y, 0), rightSrc,
                new Vector2(rightWidth, wallHeight),
                BuildingSorting.RooftopWall);
        }

        private void SpawnRooftopWallDecoration(Transform parent, float y, float rooftopWidth)
        {
            string leftKey = $"WallDecoration_Top_{Definition.RooftopWallDecoration}_Left_WW";
            string middleKey = $"WallDecoration_Top_{Definition.RooftopWallDecoration}_Middle";
            string rightKey = $"WallDecoration_Top_{Definition.RooftopWallDecoration}_Right_WW";

            GameObject leftPrefab = wallRegistry.Get(leftKey);
            GameObject middlePrefab = wallRegistry.Get(middleKey);
            GameObject rightPrefab = wallRegistry.Get(rightKey);

            if (!leftPrefab || !middlePrefab || !rightPrefab)
                return;

            SpriteRenderer leftSrc = leftPrefab.GetComponent<SpriteRenderer>();
            SpriteRenderer middleSrc = middlePrefab.GetComponent<SpriteRenderer>();
            SpriteRenderer rightSrc = rightPrefab.GetComponent<SpriteRenderer>();

            float totalWidth = Definition.Width * Metrics.ModuleWidth;
            float decoWidth = rooftopWidth * Metrics.RooftopDecorationWidthFactor;
            float offsetX = (totalWidth - decoWidth) * 0.5f;

            float leftWidth = leftSrc.bounds.size.x;
            float rightWidth = rightSrc.bounds.size.x;
            float middleWidth = decoWidth - leftWidth - rightWidth;

            if (middleWidth <= 0f)
                return;

            CreateSimpleSegment("RooftopDeco_Left", parent,
                new Vector3(offsetX, y, 0), leftSrc, BuildingSorting.RoofSep);

            CreateTiledSegment("RooftopDeco_Middle", parent,
                new Vector3(offsetX + leftWidth, y, 0), middleSrc,
                new Vector2(middleWidth, middleSrc.bounds.size.y),
                BuildingSorting.RoofSep);

            CreateSimpleSegment("RooftopDeco_Right", parent,
                new Vector3(offsetX + leftWidth + middleWidth, y, 0),
                rightSrc, BuildingSorting.RoofSep);
        }

        #endregion

        #region Rooftop

        private void SpawnRooftop(Transform parent)
        {
            if (RooftopPrefab == null)
                return;

            float y = (Definition.FloorsCount + 1) * Metrics.FloorHeight;
            float halfWidth = Metrics.ModuleWidth * 0.5f;

            GameObject roof = Instantiate(RooftopPrefab, parent);
            roof.transform.localPosition = new Vector3(halfWidth, y, 0);

            RooftopComponent rc = roof.GetComponent<RooftopComponent>();
            if (rc == null)
                return;

            rc.roofRegistry = roofRegistry;
            rc.WidthModules = Definition.Width;
            rc.Initialize(Definition);
        }

        #endregion

        #region Helpers

        private enum Segment
        {
            Left,
            Middle,
            Right
        }

        private Segment ResolveSegment(int index, int width)
        {
            if (index == 0) return Segment.Left;
            if (index == width - 1) return Segment.Right;
            return Segment.Middle;
        }

        private void CreateSimpleSegment(
            string name,
            Transform parent,
            Vector3 position,
            SpriteRenderer source,
            int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = position;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.sortingLayerID = source.sortingLayerID;
            sr.sortingOrder = sortingOrder;
        }

        private void CreateTiledSegment(
            string name,
            Transform parent,
            Vector3 position,
            SpriteRenderer source,
            Vector2 size,
            int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = position;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.sortingLayerID = source.sortingLayerID;
            sr.sortingOrder = sortingOrder;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
        }

        private void ClearGenerated()
        {
            if (GeneratedRoot == null)
                return;

            for (int i = GeneratedRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(GeneratedRoot.GetChild(i).gameObject);
        }

        #endregion
    }
}
