using CityRush.World.Buildings.Registry.Interior;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CityRush.World.Interior
{
    public sealed class CorridorComponent : MonoBehaviour
    {
        private const float PPU = 48;
        private const string FLOOR_CHILD_NAME = "Floor";
        private const string WALL_CHILD_NAME = "Wall";
        private const string SKIRT_CHILD_NAME = "Skirt";

        [Header("Registries")]
        [SerializeField] private InteriorWallRegistry wallRegistry;
        [SerializeField] private InteriorFloorRegistry floorRegistry;
        [SerializeField] private InteriorSkirtingRegistry skirtingRegistry;
        [SerializeField] private InteriorDoorRegistry doorRegistry;
        [SerializeField] private InteriorDoorFrameRegistry doorFrameRegistry;

        [Header("Data")]
        [TextArea(5, 20)]
        [SerializeField] private string corridorData;

        [Header("Layout (Pixels)")]
        [SerializeField] private int hallwayWidthPx = 700;
        [SerializeField] private int hallwayHeightPx = 470;
        [SerializeField] private int floorHeightPx = 170;

        [Header("Floor")]
        [SerializeField] private string floorKey = "InteriorFloor_Brown_Solid";

        [Header("Wall")]
        [SerializeField] private string wallKey = "InteriorWall_Blue_LargeStripe";

        [Header("Pannel")]
        [SerializeField] private string skirtingKey = "InteriorSkirting_White_Solid";

        [Header("Zooms")]
        [SerializeField] private const float ZOOM_SMALL = 0.23f;
        [SerializeField] private const float ZOOM_MEDIUM = 0.6f;
        [SerializeField] private const float ZOOM_FULL = 1f;

        // Sprite Renderer Properties
        SpriteRenderer floorRenderer;
        SpriteRenderer wallRenderer;
        SpriteRenderer skirtingRenderer;
        private float floorHeight;
        private const int SORTING_ORDER = 16;

        // Collider Properties
        BoxCollider2D floorCollider;

        public string CorridorData
        {
            get => corridorData;
            private set => corridorData = value;
        }

        private void Awake()
        {
            floorRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            ClearCorridor();

            BuildFloor();
            BuildWall();
            ApplyWallSkirting();

            SetComponentsGlobals();
            ScaleCorridor(ZOOM_SMALL);
        }

        // ------------------------------------------------------------
        // FLOOR
        // ------------------------------------------------------------
        public void BuildFloor()
        {
            // resolve prefab from registry
            GameObject floorPrefab = floorRegistry.Get(floorKey);
            if (floorPrefab == null)
            {
                Debug.LogError($"[CorridorComponent] Floor key not found: {floorKey}");
                return;
            }

            // find or create Floor container
            Transform floorRoot = transform.Find(FLOOR_CHILD_NAME);
            if (floorRoot == null)
            {
                GameObject root = new GameObject(FLOOR_CHILD_NAME);
                root.transform.SetParent(transform, false);
                floorRoot = root.transform;
            }

            // clear previous
            for (int i = floorRoot.childCount - 1; i >= 0; i--)
                Destroy(floorRoot.GetChild(i).gameObject);

            // instantiate
            GameObject floorInstance = Instantiate(floorPrefab, floorRoot);
            floorInstance.transform.localRotation = Quaternion.identity;
            floorInstance.transform.localScale = Vector3.one;
            // sprite renderer
            floorRenderer = floorInstance.GetComponent<SpriteRenderer>();
            floorRenderer.sortingOrder = SORTING_ORDER;

            // store required values
            floorHeight = floorRenderer.bounds.size.y;

            // collider
            floorCollider = floorInstance.GetComponent<BoxCollider2D>();

        }


        // ------------------------------------------------------------
        // WALL
        // ------------------------------------------------------------
        public void BuildWall()
        {
            // resolve prefab from registry
            GameObject wallPrefab = wallRegistry.Get(wallKey);
            if (wallPrefab == null)
            {
                Debug.LogError($"[CorridorComponent] Wall key not found: {wallKey}");
                return;
            }

            // find or create Wall container
            Transform wallRoot = transform.Find(WALL_CHILD_NAME);
            if (wallRoot == null)
            {
                GameObject root = new GameObject(WALL_CHILD_NAME);
                root.transform.SetParent(transform, false);
                wallRoot = root.transform;
            }

            // clear previous
            for (int i = wallRoot.childCount - 1; i >= 0; i--)
                Destroy(wallRoot.GetChild(i).gameObject);

            // instantiate
            GameObject wallInstance = Instantiate(wallPrefab, wallRoot);
            wallInstance.transform.localRotation = Quaternion.identity;
            wallInstance.transform.localPosition = new Vector3(0f, floorHeight, 0f);
            // sprite renderer
            wallRenderer = wallInstance.GetComponent<SpriteRenderer>();
            wallRenderer.sortingOrder = SORTING_ORDER;
        }

        public void ApplyWallSkirting()
        {
            // resolve prefab from registry
            GameObject skirtingPrefab = skirtingRegistry.Get(skirtingKey);
            if (skirtingPrefab == null)
            {
                Debug.LogError($"[CorridorComponent] Skirting key not found: {skirtingKey}");
                return;
            }

            // find or create Skirting container
            Transform skirtingRoot = transform.Find(SKIRT_CHILD_NAME);
            if (skirtingRoot == null)
            {
                GameObject root = new GameObject(SKIRT_CHILD_NAME);
                root.transform.SetParent(transform, false);
                skirtingRoot = root.transform;
            }

            // clear previous
            for (int i = skirtingRoot.childCount - 1; i >= 0; i--)
                Destroy(skirtingRoot.GetChild(i).gameObject);

            // instantiate
            GameObject skirtingInstance = Instantiate(skirtingPrefab, skirtingRoot);
            skirtingInstance.transform.localRotation = Quaternion.identity;
            skirtingInstance.transform.localPosition = new Vector3(0f, floorHeight, 0f);

            // sprite renderer
            skirtingRenderer = skirtingInstance.GetComponent<SpriteRenderer>();
            skirtingRenderer.sortingOrder = SORTING_ORDER + 1;
        }

        // ------------------------------------------------------------
        // HELPERS
        // ------------------------------------------------------------

        public void SetJson(string json)
        {
            corridorData = json;
        }

        private void ScaleCorridor(float zoomScale)
        {
            transform.localPosition = new Vector3(19f, -5f, 0f); ;
            transform.localScale = Vector3.one * zoomScale;
        }

        private void SetComponentsGlobals()
        {
            float widthUnits = hallwayWidthPx / PPU;

            floorRenderer.size = new Vector2(widthUnits, floorRenderer.size.y);
            wallRenderer.size = new Vector2(widthUnits, wallRenderer.size.y);
            skirtingRenderer.size = new Vector2(widthUnits, skirtingRenderer.size.y);

            floorCollider.size = new Vector2(
                widthUnits,
                floorCollider.size.y
            );

            floorCollider.offset = new Vector2(
                widthUnits * 0.5f,
                floorCollider.offset.y
            );
        }

        private void ClearCorridor()
        {
            ClearChild(FLOOR_CHILD_NAME);
            ClearChild(WALL_CHILD_NAME);
            ClearChild(SKIRT_CHILD_NAME);

            floorRenderer = null;
            wallRenderer = null;
            skirtingRenderer = null;
            floorCollider = null;
            floorHeight = 0f;
        }

        private void ClearChild(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
                return;

            for (int i = child.childCount - 1; i >= 0; i--)
                Destroy(child.GetChild(i).gameObject);
        }
    }
}
