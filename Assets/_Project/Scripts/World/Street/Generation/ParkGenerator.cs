using UnityEngine;
using CityRush.World.Street.Data;
using CityRush.World.Street.Registry;

namespace CityRush.World.Street.Generation
{
    public class ParkGenerator : MonoBehaviour
    {
        [Header("Park Definition")]
        public ParkDefinition Definition;

        [Header("Registry")]
        public ParkRegistry parkRegistry;

        [Header("Metrics")]
        [SerializeField] private float moduleWidth = 160f / 48f;
        [SerializeField] private int entranceGapBlocks = 1;

        [Header("Props Placement")]
        [SerializeField] private float propsY = 0.5f;
        [SerializeField] private float propsXStep = 1f;
        [SerializeField] private float propsEdgePadding = -1f;

        [Header("Prefab Structure (Auto-Filled)")]
        [SerializeField] private SpriteRenderer groundRenderer;
        [SerializeField] private SpriteRenderer fenceLeftRenderer;
        [SerializeField] private SpriteRenderer fenceRightRenderer;
        [SerializeField] private Transform propsRoot;

        private void Reset()
        {
            CacheRefs();
        }

        private void Awake()
        {
            CacheRefs();
        }

        private void CacheRefs()
        {
            if (groundRenderer == null)
                groundRenderer = FindSpriteRenderer("Ground");

            if (fenceLeftRenderer == null)
                fenceLeftRenderer = FindSpriteRenderer("Fence/Left");

            if (fenceRightRenderer == null)
                fenceRightRenderer = FindSpriteRenderer("Fence/Right");

            if (propsRoot == null)
                propsRoot = transform.Find("Props");
        }

        private SpriteRenderer FindSpriteRenderer(string path)
        {
            Transform t = transform.Find(path);
            return t != null ? t.GetComponent<SpriteRenderer>() : null;
        }

        public void Build(ParkDefinition definition = null)
        {
            if (definition != null)
                Definition = definition;

            Generate();
        }

        public void Generate()
        {
            if (Definition == null || parkRegistry == null)
                return;

            float totalWidth = Mathf.Max(0, Definition.WidthBlocks) * moduleWidth;

            ApplyGround(totalWidth);
            ApplyFences(totalWidth);
            SpawnProps(totalWidth);
        }

        private void ApplyGround(float totalWidth)
        {
            if (groundRenderer == null)
                return;

            if (!string.IsNullOrWhiteSpace(Definition.GroundKey))
            {
                GameObject prefab = parkRegistry.GetGround(Definition.GroundKey);
                ApplySpriteRendererTemplate(groundRenderer, prefab);
            }

            groundRenderer.drawMode = SpriteDrawMode.Tiled;
            groundRenderer.tileMode = SpriteTileMode.Continuous;

            Vector2 size = groundRenderer.size;
            size.x = totalWidth;
            groundRenderer.size = size;
            groundRenderer.sortingOrder = StreetSorting.Ground;
        }

        private void ApplyFences(float totalWidth)
        {
            if (fenceLeftRenderer == null || fenceRightRenderer == null)
                return;

            if (!string.IsNullOrWhiteSpace(Definition.FenceKey))
            {
                GameObject prefab = parkRegistry.GetFence(Definition.FenceKey);
                ApplySpriteRendererTemplate(fenceLeftRenderer, prefab);
                ApplySpriteRendererTemplate(fenceRightRenderer, prefab);
            }

            fenceLeftRenderer.drawMode = SpriteDrawMode.Tiled;
            fenceLeftRenderer.tileMode = SpriteTileMode.Continuous;

            fenceRightRenderer.drawMode = SpriteDrawMode.Tiled;
            fenceRightRenderer.tileMode = SpriteTileMode.Continuous;

            if (!Definition.HasEntrance)
            {
                fenceRightRenderer.gameObject.SetActive(false);

                Vector2 leftSize = fenceLeftRenderer.size;
                leftSize.x = totalWidth;
                fenceLeftRenderer.size = leftSize;

                fenceLeftRenderer.transform.localPosition = Vector3.zero;
                return;
            }

            // Visual entrance gap only (navigation will be handled later)
            fenceRightRenderer.gameObject.SetActive(true);

            int widthBlocks = Mathf.Max(0, Definition.WidthBlocks);
            int gapBlocks = Mathf.Max(1, entranceGapBlocks);

            int leftBlocks = Mathf.CeilToInt((widthBlocks - gapBlocks) / 2f);
            leftBlocks = Mathf.Clamp(leftBlocks, 0, widthBlocks);

            int rightBlocks = widthBlocks - gapBlocks - leftBlocks;
            rightBlocks = Mathf.Max(0, rightBlocks);

            float leftWidth = leftBlocks * moduleWidth;
            float gapWidth = gapBlocks * moduleWidth;
            float rightWidth = rightBlocks * moduleWidth;

            Vector2 leftSize2 = fenceLeftRenderer.size;
            leftSize2.x = leftWidth;
            fenceLeftRenderer.size = leftSize2;

            Vector2 rightSize2 = fenceRightRenderer.size;
            rightSize2.x = rightWidth;
            fenceRightRenderer.size = rightSize2;

            fenceLeftRenderer.transform.localPosition = Vector3.zero;
            fenceRightRenderer.transform.localPosition = new Vector3(leftWidth + gapWidth, 0f, 0f);

            fenceLeftRenderer.sortingOrder = StreetSorting.Fences;
            fenceRightRenderer.sortingOrder = StreetSorting.Fences;

            if (rightBlocks <= 0)
                fenceRightRenderer.gameObject.SetActive(false);
        }

        private void SpawnProps(float totalWidth)
        {
            if (propsRoot == null)
                return;

            ClearChildren(propsRoot);

            if (Definition.PropKeys == null || Definition.PropKeys.Length == 0)
                return;

            int count = Definition.PropKeys.Length;

            // If positions are missing/mismatched, we still spawn but default X = 0
            bool hasPositions = Definition.PropPosition != null && Definition.PropPosition.Length == count;

            float xMin = propsEdgePadding;
            float xMax = totalWidth - propsEdgePadding;

            for (int i = 0; i < count; i++)
            {
                string key = Definition.PropKeys[i];
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                GameObject prefab = parkRegistry.GetProp(key);
                if (prefab == null)
                    continue;

                Transform t = Instantiate(prefab, propsRoot).transform;

                // PropPosition is in "block steps" (0..WidthBlocks). Convert to world units.
                float x = hasPositions ? (Definition.PropPosition[i] * moduleWidth) : 0f;

                // Keep deterministic but safe
                if (xMax >= xMin)
                    x = Mathf.Clamp(x, xMin, xMax);

                t.localPosition = new Vector3(x, propsY, 0f);

                ApplySortingToProp(t);
            }
        }

        private void ApplySortingToProp(Transform t)
        {
            var srs = t.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < srs.Length; i++)
                srs[i].sortingOrder = StreetSorting.Props;
        }

        private void ApplySpriteRendererTemplate(SpriteRenderer target, GameObject prefab)
        {
            if (target == null || prefab == null)
                return;

            SpriteRenderer src = prefab.GetComponent<SpriteRenderer>();
            if (src == null)
                return;

            target.sprite = src.sprite;
            target.color = src.color;
            target.sharedMaterial = src.sharedMaterial;
            target.flipX = src.flipX;
            target.flipY = src.flipY;

            target.sortingLayerID = src.sortingLayerID;
            target.sortingOrder = src.sortingOrder;
        }

        private void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }
}
