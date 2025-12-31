using UnityEngine;

namespace CityRush.World.Interior
{
    public sealed class CorridorComponent : MonoBehaviour
    {
        [Header("Shell Sprites (pivot: bottom-left)")]
        [SerializeField] private Sprite floorSprite;
        [SerializeField] private Sprite wallSprite;
        [SerializeField] private Sprite skirtSprite; // wall-floor separator / panel

        [Header("Sizing")]
        [SerializeField] private int sidePaddingPixels = 50;

        // Step-1 test input (temporary)
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private float testInnerWidthWorld = 12f;

        [Header("Sorting (optional)")]
        [SerializeField] private int wallSortingOrder = 0;
        [SerializeField] private int skirtSortingOrder = 1;
        [SerializeField] private int floorSortingOrder = 2;

        private SpriteRenderer _floor;
        private SpriteRenderer _wall;
        private SpriteRenderer _skirt;

        private void Awake()
        {
            _floor = EnsureLayer("Floor");
            _wall = EnsureLayer("Wall");
            _skirt = EnsureLayer("Skirt");
        }

        private void Start()
        {
            if (buildOnStart)
                Build(testInnerWidthWorld);
        }

        public void Build(float innerWidthWorld)
        {
            if (floorSprite == null || wallSprite == null || skirtSprite == null)
                return;

            float ppu = floorSprite.pixelsPerUnit;
            float paddingWorld = sidePaddingPixels / Mathf.Max(1f, ppu);

            float width = Mathf.Max(0.01f, innerWidthWorld + (paddingWorld * 2f));

            float floorH = SpriteHeightWorld(floorSprite);
            float skirtH = SpriteHeightWorld(skirtSprite);
            float wallH = SpriteHeightWorld(wallSprite);

            ConfigureLayer(_floor, floorSprite, width, floorH, floorSortingOrder);
            ConfigureLayer(_skirt, skirtSprite, width, skirtH, skirtSortingOrder);
            ConfigureLayer(_wall, wallSprite, width, wallH, wallSortingOrder);

            // Bottom-left pivots: localPosition is the bottom-left of each stretched layer.
            _floor.transform.localPosition = Vector3.zero;
            _skirt.transform.localPosition = new Vector3(0f, floorH, 0f);
            _wall.transform.localPosition = new Vector3(0f, floorH + skirtH, 0f);
        }

        private static float SpriteHeightWorld(Sprite s)
            => (s.rect.height / Mathf.Max(1f, s.pixelsPerUnit));

        private static void ConfigureLayer(SpriteRenderer r, Sprite s, float width, float height, int sortingOrder)
        {
            r.sprite = s;
            r.drawMode = SpriteDrawMode.Tiled;
            r.size = new Vector2(width, height);
            r.sortingOrder = sortingOrder;
        }

        private SpriteRenderer EnsureLayer(string name)
        {
            Transform t = transform.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                t = go.transform;
                t.SetParent(transform, false);
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
            }

            var r = t.GetComponent<SpriteRenderer>();
            if (r == null)
                r = t.gameObject.AddComponent<SpriteRenderer>();

            return r;
        }
    }
}
