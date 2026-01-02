using CityRush.World.Buildings.Registry.Interior;
using UnityEngine;

namespace CityRush.World.Interior
{
    public sealed class CorridorComponent : MonoBehaviour
    {
        private const float PPU = 48f;

        private const string FLOOR_CHILD_NAME = "Floor";
        private const string WALL_CHILD_NAME = "Wall";
        private const string SKIRT_CHILD_NAME = "Skirt";
        private const string DOOR_FRAME_CHILD_NAME = "DoorFrame";
        private const string DOOR_LEAF_CHILD_NAME = "DoorLeaf";

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
        [SerializeField] private int hallwayApartments = 3;
        [SerializeField] private int appartmentWidth = 400;
        [SerializeField] private int hallwayWidthPx = 900;     // kept for inspector compatibility (not used yet)
        [SerializeField] private int hallwayBleedPx = 100;
        [SerializeField] private int hallwayHeightPx = 470;    // kept for inspector compatibility (not used yet)
        [SerializeField] private int floorHeightPx = 170;      // kept for inspector compatibility (not used yet)

        [Header("Floor")]
        [SerializeField] private string floorKey = "InteriorFloor_Brown_Solid";

        [Header("Wall")]
        [SerializeField] private string wallKey = "InteriorWall_Blue_LargeStripe";

        [Header("Pannel")]
        [SerializeField] private string skirtingKey = "InteriorSkirting_White_Solid";

        [Header("Door")]
        [SerializeField] private string doorFrameKey = "InteriorDoorFrame_White_Solid";
        [SerializeField] private string doorLeafKey = "InteriorDoor_Brown_Cube";

        [Header("Presentation")]
        [Min(0.01f)]
        [SerializeField] private float zoomScale = 0.23f;

        [SerializeField] private Vector3 localOffset = new Vector3(19f, -5f, 0f);

        [Header("Render")]
        [SerializeField] private int sortingBase = 15;

        // runtime refs
        private SpriteRenderer _floorRenderer;
        private SpriteRenderer _wallRenderer;
        private SpriteRenderer _skirtingRenderer;
        private BoxCollider2D _floorCollider;

        private float _floorTopLocalY;
        private float _corridorWidthWorldUnits;

        public string CorridorData => corridorData;

        // After zoom is applied, this stays the "real" world width (what you expect to see in-game).
        public float CorridorWidthWorldUnits => _corridorWidthWorldUnits;

        // Useful for GameLoop player spawn.
        public float FloorTopWorldY => _floorCollider != null ? _floorCollider.bounds.max.y : transform.position.y;
        public Bounds FloorBoundsWorld => _floorCollider != null ? _floorCollider.bounds : default;

        private void Start()
        {
            Rebuild();
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            ClearCorridor();
            ApplyPresentation();

            BuildFloor();
            ResolveFloorTopLocalY();

            BuildWall();
            BuildSkirting();
            BuildDoors();

            ApplyTilingAndCollider();
        }

        public void SetJson(string json) => corridorData = json;

        public void SetPresentation(float zoom, Vector3 offset)
        {
            zoomScale = Mathf.Max(0.01f, zoom);
            localOffset = offset;
        }

        // ------------------------------------------------------------
        // Build
        // ------------------------------------------------------------

        private void BuildFloor()
        {
            var prefab = floorRegistry != null ? floorRegistry.Get(floorKey) : null;
            if (prefab == null)
            {
                Debug.LogError($"[CorridorComponent] Floor key not found: {floorKey}", this);
                return;
            }

            Transform root = GetOrCreate(FLOOR_CHILD_NAME);

            GameObject inst = Instantiate(prefab, root);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            _floorRenderer = inst.GetComponent<SpriteRenderer>();
            if (_floorRenderer != null) _floorRenderer.sortingOrder = sortingBase;

            _floorCollider = inst.GetComponent<BoxCollider2D>();
        }

        private void BuildWall()
        {
            var prefab = wallRegistry != null ? wallRegistry.Get(wallKey) : null;
            if (prefab == null)
            {
                Debug.LogError($"[CorridorComponent] Wall key not found: {wallKey}", this);
                return;
            }

            Transform root = GetOrCreate(WALL_CHILD_NAME);

            GameObject inst = Instantiate(prefab, root);
            inst.transform.localPosition = new Vector3(0f, _floorTopLocalY, 0f);
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            _wallRenderer = inst.GetComponent<SpriteRenderer>();
            if (_wallRenderer != null) _wallRenderer.sortingOrder = sortingBase;
        }

        private void BuildSkirting()
        {
            var prefab = skirtingRegistry != null ? skirtingRegistry.Get(skirtingKey) : null;
            if (prefab == null)
            {
                Debug.LogError($"[CorridorComponent] Skirting key not found: {skirtingKey}", this);
                return;
            }

            Transform root = GetOrCreate(SKIRT_CHILD_NAME);

            GameObject inst = Instantiate(prefab, root);
            inst.transform.localPosition = new Vector3(0f, _floorTopLocalY, 0f);
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            _skirtingRenderer = inst.GetComponent<SpriteRenderer>();
            if (_skirtingRenderer != null) _skirtingRenderer.sortingOrder = sortingBase + 1;
        }

        private void BuildDoors()
        {
            var framePrefab = doorFrameRegistry != null ? doorFrameRegistry.Get(doorFrameKey) : null;
            var leafPrefab = doorRegistry != null ? doorRegistry.Get(doorLeafKey) : null;

            if (framePrefab == null || leafPrefab == null)
            {
                Debug.LogError($"[CorridorComponent] Missing door prefabs. Frame={doorFrameKey}, Leaf={doorLeafKey}", this);
                return;
            }

            Transform frameRoot = GetOrCreate(DOOR_FRAME_CHILD_NAME);
            Transform leafRoot = GetOrCreate(DOOR_LEAF_CHILD_NAME);

            float startXPx = (appartmentWidth * 0.5f) + (hallwayBleedPx * 0.5f);

            for (int i = 0; i < hallwayApartments; i++)
            {
                float xPx = startXPx + i * appartmentWidth;
                float xUnitsLocal = xPx / PPU / zoomScale;

                // Frame
                {
                    GameObject frame = Instantiate(framePrefab, frameRoot);
                    frame.transform.localPosition = new Vector3(xUnitsLocal, _floorTopLocalY, 0f);
                    frame.transform.localRotation = Quaternion.identity;
                    frame.transform.localScale = Vector3.one;

                    var r = frame.GetComponent<SpriteRenderer>();
                    if (r != null) r.sortingOrder = sortingBase + 3;
                }

                // Leaf
                {
                    GameObject leaf = Instantiate(leafPrefab, leafRoot);
                    leaf.transform.localPosition = new Vector3(xUnitsLocal, _floorTopLocalY, 0f);
                    leaf.transform.localRotation = Quaternion.identity;
                    leaf.transform.localScale = Vector3.one;

                    var r = leaf.GetComponent<SpriteRenderer>();
                    if (r != null) r.sortingOrder = sortingBase + 4;
                }
            }
        }

        // ------------------------------------------------------------
        // Layout / Sizing
        // ------------------------------------------------------------

        private void ApplyTilingAndCollider()
        {
            int corridorWidthPx = hallwayApartments * appartmentWidth;
            _corridorWidthWorldUnits = (corridorWidthPx + hallwayBleedPx) / PPU;

            // IMPORTANT:
            // We scale the whole corridor (zoomScale), so tiling width must be divided by zoomScale,
            // otherwise it becomes huge/small in-world.
            float widthUnitsLocal = _corridorWidthWorldUnits / zoomScale;

            if (_floorRenderer != null) _floorRenderer.size = new Vector2(widthUnitsLocal, _floorRenderer.size.y);
            if (_wallRenderer != null) _wallRenderer.size = new Vector2(widthUnitsLocal, _wallRenderer.size.y);
            if (_skirtingRenderer != null) _skirtingRenderer.size = new Vector2(widthUnitsLocal, _skirtingRenderer.size.y);

            if (_floorCollider != null)
            {
                _floorCollider.size = new Vector2(widthUnitsLocal, _floorCollider.size.y);
                _floorCollider.offset = new Vector2(widthUnitsLocal * 0.5f, _floorCollider.offset.y);
            }
        }

        private void ResolveFloorTopLocalY()
        {
            // Need local-space height, not world-space bounds.
            // bounds includes parent scaling, so divide by zoomScale.
            if (_floorRenderer != null)
            {
                float worldH = _floorRenderer.bounds.size.y;
                _floorTopLocalY = worldH / zoomScale;
                return;
            }

            if (_floorCollider != null)
            {
                float worldH = _floorCollider.bounds.size.y;
                _floorTopLocalY = worldH / zoomScale;
                return;
            }

            _floorTopLocalY = 0f;
        }

        // ------------------------------------------------------------
        // Presentation / Helpers
        // ------------------------------------------------------------

        private void ApplyPresentation()
        {
            transform.localPosition = localOffset;
            transform.localScale = Vector3.one * zoomScale;
        }

        private Transform GetOrCreate(string name)
        {
            Transform t = transform.Find(name);
            if (t == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(transform, false);
                t = go.transform;
            }

            for (int i = t.childCount - 1; i >= 0; i--)
                Destroy(t.GetChild(i).gameObject);

            return t;
        }

        private void ClearCorridor()
        {
            ClearChild(FLOOR_CHILD_NAME);
            ClearChild(WALL_CHILD_NAME);
            ClearChild(SKIRT_CHILD_NAME);
            ClearChild(DOOR_FRAME_CHILD_NAME);
            ClearChild(DOOR_LEAF_CHILD_NAME);

            _floorRenderer = null;
            _wallRenderer = null;
            _skirtingRenderer = null;
            _floorCollider = null;

            _floorTopLocalY = 0f;
            _corridorWidthWorldUnits = 0f;
        }

        private void ClearChild(string name)
        {
            Transform t = transform.Find(name);
            if (t == null) return;

            for (int i = t.childCount - 1; i >= 0; i--)
                Destroy(t.GetChild(i).gameObject);
        }
    }
}
