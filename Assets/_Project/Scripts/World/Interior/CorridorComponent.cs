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
