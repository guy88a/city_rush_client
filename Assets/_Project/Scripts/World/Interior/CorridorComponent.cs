using CityRush.World.Buildings.Registry.Interior;
using UnityEngine;

namespace CityRush.World.Interior
{
    public sealed class CorridorComponent : MonoBehaviour
    {
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
        [SerializeField] private int hallwayHeightPx = 500;
        [SerializeField] private int floorHeightPx = 320;

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
                Debug.LogError("[CorridorComponent] Floor prefab not found for key '" + floorKey + "'.", this);
                return;
            }

            var floorTransform = transform.Find("Floor");
            if (floorTransform != null)
                Destroy(floorTransform.gameObject);

            var floorInstance = Instantiate(prefab, transform);
            floorInstance.name = "Floor";

            var sr = floorInstance.GetComponentInChildren<SpriteRenderer>();
            if (sr == null || sr.sprite == null)
            {
                Debug.LogError("[CorridorComponent] Floor prefab must have a SpriteRenderer with a Sprite.", floorInstance);
                return;
            }

            sr.drawMode = SpriteDrawMode.Tiled;

            float ppu = sr.sprite.pixelsPerUnit;
            float targetWidthUnits = hallwayWidthPx / ppu;
            float targetHeightUnits = floorHeightPx / ppu;

            sr.size = new Vector2(targetWidthUnits, targetHeightUnits);
            sr.sortingOrder = 1000;

            // Ensure collider is driven by the tiled size (SpriteRenderer.size does NOT resize colliders).
            // NOTE: floor prefabs may ship with colliders on various children; keep ONE BoxCollider2D and disable the rest.
            BoxCollider2D col = null;
            var colliders = floorInstance.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null)
                    continue;

                // Prefer a BoxCollider2D on the same GameObject as the SpriteRenderer (same local space).
                if (col == null && colliders[i] is BoxCollider2D bc && bc.gameObject == sr.gameObject)
                {
                    col = bc;
                    col.enabled = true;
                    continue;
                }

                colliders[i].enabled = false;
            }

            if (col == null)
                col = sr.gameObject.AddComponent<BoxCollider2D>();

            col.enabled = true;

            float colliderHeightUnits = sr.size.y * 0.8f;
            col.size = new Vector2(sr.size.x, colliderHeightUnits);

            // Bottom-aligned in world space (handles non-centered pivots):
            // colliderCenterY = spriteBottomY + (colliderHeight/2)
            var desiredWorldCenter = sr.bounds.center;
            desiredWorldCenter.y = sr.bounds.min.y + (colliderHeightUnits * 0.5f);
            var localCenter = sr.transform.InverseTransformPoint(desiredWorldCenter);
            col.offset = new Vector2(localCenter.x, localCenter.y);

            float panelHalfHeightUnits = (hallwayHeightPx * 0.5f) / ppu;
            float floorHalfHeightUnits = (floorHeightPx * 0.5f) / ppu;
            float y = -panelHalfHeightUnits + floorHalfHeightUnits;

            floorInstance.transform.localPosition = new Vector3(0f, y, 0f);
            floorInstance.transform.localRotation = Quaternion.identity;
            floorInstance.transform.localScale = Vector3.one;
        }

        public void SetJson(string json)
        {
            CorridorData = json;
        }
    }
}
