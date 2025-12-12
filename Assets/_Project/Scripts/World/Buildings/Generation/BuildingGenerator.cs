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

        const float WALL_MODULE_WIDTH = 160f / 48f;
        private const float ROOFTOP_WALL_HEIGHT_FACTOR = 0.5f;

        [Header("Generated Output (Read Only)")]
        public Transform GeneratedRoot;

        private void Start()
        {
            Generate();
        }

        private void Reset()
        {
            if (GeneratedRoot == null)
            {
                GameObject go = new GameObject("Building");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                GeneratedRoot = go.transform;
            }
        }

        public void Generate()
        {
            ClearGenerated();

            if (EntranceFloorPrefab == null || RegularFloorPrefab == null)
                return;

            float floorHeight = 260f / 48f;
            float moduleWidth = 160f / 48f;
            Transform parent = GeneratedRoot;

            // -----------------------------------------------------
            // ENTRANCE FLOOR
            // -----------------------------------------------------
            GameObject entrance = Instantiate(EntranceFloorPrefab, parent);
            entrance.transform.localPosition = Vector3.zero;

            FloorComponent entranceFloor = entrance.GetComponent<FloorComponent>();
            if (entranceFloor != null)
            {
                entranceFloor.wallRegistry = wallRegistry;
                entranceFloor.windowRegistry = windowRegistry;
                entranceFloor.doorRegistry = doorRegistry;

                entranceFloor.WidthModules = Definition.Width;
                entranceFloor.Initialize(Definition, true); // isEntrance = true
            }

            // -----------------------------------------------------
            // REGULAR FLOORS
            // -----------------------------------------------------
            for (int i = 0; i < Definition.FloorsCount; i++)
            {
                float y = (i + 1) * floorHeight;

                GameObject floor = Instantiate(RegularFloorPrefab, parent);
                floor.transform.localPosition = new Vector3(0, y, 0);

                FloorComponent fc = floor.GetComponent<FloorComponent>();
                if (fc != null)
                {
                    fc.wallRegistry = wallRegistry;
                    fc.windowRegistry = windowRegistry;
                    fc.doorRegistry = doorRegistry;

                    fc.WidthModules = Definition.Width;
                    fc.Initialize(Definition, false); // isEntrance = false
                }
            }

            // -----------------------------------------------------
            // FLOOR SEPARATORS (between floors)
            // -----------------------------------------------------
            if (Definition.UseSeparators && separatorRegistry != null)
            {
                // between: entrance <-> floor0, floor0 <-> floor1, ..., floorN-2 <-> floorN-1
                for (int i = 0; i < Definition.FloorsCount; i++)
                {
                    float y = (i + 1) * floorHeight;
                    SpawnSeparatorRow(Definition, parent, y, moduleWidth);
                }
            }

            // -----------------------------------------------------
            // ROOFTOP SEPARATOR
            // -----------------------------------------------------
            if (Definition.UseRooftopSeparator && rooftopSeparatorRegistry != null)
            {
                float y = (Definition.FloorsCount + 1) * floorHeight;
                SpawnRooftopSeparatorRow(Definition, parent, y, moduleWidth);
            }

            // -----------------------------------------------------
            // ROOFTOP WALL
            // -----------------------------------------------------
            SpawnRooftopWall(Definition, parent, (Definition.FloorsCount + 1) * floorHeight);

            // -----------------------------------------------------
            // ROOFTOP
            // -----------------------------------------------------
            if (RooftopPrefab != null)
            {
                float y = (Definition.FloorsCount + 1) * floorHeight;

                GameObject roof = Instantiate(RooftopPrefab, parent);
                float halfWidth = moduleWidth * 0.5f; // ~1.6667
                roof.transform.localPosition = new Vector3(halfWidth, y, 0);

                RooftopComponent rc = roof.GetComponent<RooftopComponent>();
                if (rc != null)
                {
                    rc.roofRegistry = roofRegistry;
                    rc.WidthModules = Definition.Width;
                    rc.Initialize(Definition);
                }
            }
        }

        private void SpawnSeparatorRow(BuildingDefinition def, Transform parent, float y, float moduleWidth)
        {
            if (string.IsNullOrEmpty(def.SeparatorType))
                return;

            for (int i = 0; i < def.Width; i++)
            {
                string positionSuffix;

                if (i == 0)
                    positionSuffix = "Left_WW";
                else if (i == def.Width - 1)
                    positionSuffix = "Right_WW";
                else
                    positionSuffix = "Middle";

                string key = "Separator_" + def.SeparatorType + "_" + positionSuffix;

                GameObject sepPrefab = separatorRegistry.Get(key);
                if (sepPrefab == null)
                    continue;

                Transform sep = Instantiate(sepPrefab, parent).transform;
                SpriteRenderer sr = sep.GetComponent<SpriteRenderer>();
                float sepWidth = sr.bounds.size.x;
                float wallWidth = moduleWidth;

                float x = 0f;

                if (i == 0) // LEFT separator
                {
                    x = -(sepWidth - wallWidth);
                }
                else // Middle separators
                {
                    x = i * wallWidth;
                }

                sep.localPosition = new Vector3(x, y, 0f);

                // Sorting: between walls and windows
                //sr = sep.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // assuming walls at 0, windows at +2 (see tweak below)
                    sr.sortingOrder = BuildingSorting.Separators;
                }
            }
        }

        private void SpawnRooftopSeparatorRow(BuildingDefinition def, Transform parent, float y, float moduleWidth)
        {
            if (string.IsNullOrEmpty(def.RooftopSeparatorType))
                return;

            for (int i = 0; i < def.Width; i++)
            {
                string baseKey = "Rooftop_Separator_" + def.RooftopSeparatorType + "_";

                string key = null;

                if (i == 0) // LEFT
                {
                    key = baseKey + "Left_WW";

                    if (rooftopSeparatorRegistry.Get(key) == null)
                        key = baseKey + "Left";
                }
                else if (i == def.Width - 1) // RIGHT
                {
                    key = baseKey + "Right_WW";

                    if (rooftopSeparatorRegistry.Get(key) == null)
                        key = baseKey + "Right";
                }
                else // MIDDLE
                {
                    key = baseKey + "Middle";
                }

                GameObject prefab = rooftopSeparatorRegistry.Get(key);
                if (prefab == null)
                    continue;

                Transform sep = Instantiate(prefab, parent).transform;

                // --- X placement (KNOWN GOOD LOGIC) ---
                float x;
                SpriteRenderer sr = sep.GetComponent<SpriteRenderer>();
                float sepWidth = sr.bounds.size.x;

                if (i == 0) // LEFT
                {
                    x = -(sepWidth - moduleWidth);
                }
                else
                {
                    x = i * moduleWidth;
                }

                sep.localPosition = new Vector3(x, y, 0f);

                // --- Sorting ---
                if (sr != null)
                {
                    sr.sortingOrder = BuildingSorting.RoofSep;
                }
            }
        }

        private void SpawnRooftopWall(BuildingDefinition def, Transform parent, float y)
        {
            const float WALL_MODULE_WIDTH = 160f / 48f;
            const float ROOFTOP_WALL_HEIGHT_FACTOR = 0.3f;

            // -------------------------------------------------
            // Resolve wall prefabs
            // -------------------------------------------------
            string leftKey = "Wall_" + def.WallType + "_" + def.WallColor + "_Left";
            string middleKey = "Wall_" + def.WallType + "_" + def.WallColor + "_Middle";
            string rightKey = "Wall_" + def.WallType + "_" + def.WallColor + "_Right";

            GameObject leftPrefab = wallRegistry.Get(leftKey);
            GameObject middlePrefab = wallRegistry.Get(middleKey);
            GameObject rightPrefab = wallRegistry.Get(rightKey);

            if (leftPrefab == null || middlePrefab == null || rightPrefab == null)
                return;

            SpriteRenderer leftSrc = leftPrefab.GetComponent<SpriteRenderer>();
            SpriteRenderer middleSrc = middlePrefab.GetComponent<SpriteRenderer>();
            SpriteRenderer rightSrc = rightPrefab.GetComponent<SpriteRenderer>();

            if (leftSrc == null || middleSrc == null || rightSrc == null)
                return;

            // -------------------------------------------------
            // Dimensions
            // -------------------------------------------------
            float totalWidth = def.Width * WALL_MODULE_WIDTH;

            float leftWidth = leftSrc.bounds.size.x;
            float rightWidth = rightSrc.bounds.size.x;

            float middleWidth = totalWidth - leftWidth - rightWidth;
            if (middleWidth <= 0f)
                return;

            float wallHeight = middleSrc.bounds.size.y * ROOFTOP_WALL_HEIGHT_FACTOR;

            // -------------------------------------------------
            // LEFT
            // -------------------------------------------------
            GameObject left = new GameObject("RooftopWall_Left");
            left.transform.SetParent(parent);
            left.transform.localPosition = new Vector3(0f, y, 0f);

            SpriteRenderer srLeft = left.AddComponent<SpriteRenderer>();
            srLeft.sprite = leftSrc.sprite;
            srLeft.sortingLayerID = leftSrc.sortingLayerID;
            srLeft.sortingOrder = BuildingSorting.RooftopWall;
            srLeft.drawMode = SpriteDrawMode.Tiled;
            srLeft.size = new Vector2(leftWidth, wallHeight);

            // -------------------------------------------------
            // MIDDLE (tiled)
            // -------------------------------------------------
            GameObject middle = new GameObject("RooftopWall_Middle");
            middle.transform.SetParent(parent);
            middle.transform.localPosition = new Vector3(leftWidth, y, 0f);

            SpriteRenderer srMiddle = middle.AddComponent<SpriteRenderer>();
            srMiddle.sprite = middleSrc.sprite;
            srMiddle.sortingLayerID = middleSrc.sortingLayerID;
            srMiddle.sortingOrder = BuildingSorting.RooftopWall;
            srMiddle.drawMode = SpriteDrawMode.Tiled;
            srMiddle.size = new Vector2(middleWidth, wallHeight);

            // -------------------------------------------------
            // RIGHT
            // -------------------------------------------------
            GameObject right = new GameObject("RooftopWall_Right");
            right.transform.SetParent(parent);
            right.transform.localPosition = new Vector3(leftWidth + middleWidth, y, 0f);

            SpriteRenderer srRight = right.AddComponent<SpriteRenderer>();
            srRight.sprite = rightSrc.sprite;
            srRight.sortingLayerID = rightSrc.sortingLayerID;
            srRight.sortingOrder = BuildingSorting.RooftopWall;
            srRight.drawMode = SpriteDrawMode.Tiled;
            srRight.size = new Vector2(rightWidth, wallHeight);
        }



        private void ClearGenerated()
        {
            if (GeneratedRoot == null)
                return;

            for (int i = GeneratedRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(GeneratedRoot.GetChild(i).gameObject);
        }
    }
}
