using CityRush.World.Buildings.Registry.Interior;
using UnityEngine;

namespace CityRush.World.Interior
{
    public sealed class CorridorComponent : MonoBehaviour
    {
        private const string FLOOR_CHILD_NAME = "Floor";

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

        public InteriorWallRegistry WallRegistry => wallRegistry;
        public InteriorFloorRegistry FloorRegistry => floorRegistry;
        public InteriorSkirtingRegistry SkirtingRegistry => skirtingRegistry;
        public InteriorDoorRegistry DoorRegistry => doorRegistry;
        public InteriorDoorFrameRegistry DoorFrameRegistry => doorFrameRegistry;

        public string CorridorData
        {
            get => corridorData;
            private set => corridorData = value;
        }

        private void Start()
        {
            BuildFloor();
        }

        public void BuildFloor()
        {
            var prefab = floorRegistry != null ? floorRegistry.Get(floorKey) : null;
            if (prefab == null)
            {
                Debug.LogError($"[CorridorComponent] Floor prefab not found for key '{floorKey}'.", this);
                return;
            }

            var existing = transform.Find(FLOOR_CHILD_NAME);
            if (existing != null)
                Destroy(existing.gameObject);

            var floorInstance = Instantiate(prefab, transform);
            floorInstance.name = FLOOR_CHILD_NAME;

            var sr = floorInstance.GetComponentInChildren<SpriteRenderer>();
            if (sr == null || sr.sprite == null)
            {
                Debug.LogError("[CorridorComponent] Floor prefab must have a SpriteRenderer with a Sprite.", floorInstance);
                return;
            }

            sr.drawMode = SpriteDrawMode.Tiled;

            float ppu = sr.sprite.pixelsPerUnit;
            float invPpu = 1f / ppu;

            var floorSize = new Vector2(hallwayWidthPx * invPpu, floorHeightPx * invPpu);
            sr.size = floorSize;
            sr.sortingOrder = 16;

            SetupFloorCollider(floorInstance, sr, floorSize);

            float panelHalfHeightUnits = (hallwayHeightPx * 0.5f) * invPpu;
            float floorHalfHeightUnits = (floorHeightPx * 0.5f) * invPpu;
            float y = -panelHalfHeightUnits + floorHalfHeightUnits;

            floorInstance.transform.localPosition = new Vector3(0f, y, 0f);
            floorInstance.transform.localRotation = Quaternion.identity;
            floorInstance.transform.localScale = Vector3.one;
        }

        private static void SetupFloorCollider(GameObject floorInstance, SpriteRenderer sr, Vector2 floorSize)
        {
            // Ensure collider is driven by the tiled size (SpriteRenderer.size does NOT resize colliders).
            // NOTE: floor prefabs may ship with colliders on various children; keep ONE BoxCollider2D and disable the rest.
            BoxCollider2D box = null;
            var colliders = floorInstance.GetComponentsInChildren<Collider2D>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                var c = colliders[i];
                if (c == null)
                    continue;

                if (box == null && c is BoxCollider2D bc && bc.gameObject == sr.gameObject)
                {
                    box = bc;
                    box.enabled = true;
                }
                else
                {
                    c.enabled = false;
                }
            }

            if (box == null)
                box = sr.gameObject.AddComponent<BoxCollider2D>();

            box.enabled = true;

            float colliderHeight = floorSize.y * 0.8f;
            box.size = new Vector2(floorSize.x, colliderHeight);

            // Bottom-aligned in world space (handles non-centered pivots).
            var desiredWorldCenter = sr.bounds.center;
            desiredWorldCenter.y = sr.bounds.min.y + (colliderHeight * 0.5f);

            var localCenter = sr.transform.InverseTransformPoint(desiredWorldCenter);
            box.offset = new Vector2(localCenter.x, localCenter.y);
        }

        public void SetJson(string json)
        {
            CorridorData = json;
        }
    }
}
