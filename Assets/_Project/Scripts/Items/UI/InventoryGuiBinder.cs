using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CityRush.Items.UI
{
    [DisallowMultipleComponent]
    public sealed class InventoryGuiBinder : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform slotsRoot;
        [SerializeField] private GameObject slotPrefab;

        [Header("Runtime (optional)")]
        [SerializeField] private PlayerItemsRuntime playerItems;

        private readonly List<SlotRefs> _slots = new(64);

        private sealed class SlotRefs
        {
            public GameObject Root;
            public Image Frame;
            public Image Bg;
            public Image Icon;
            public TMP_Text Count;
        }

        private void Awake()
        {
            if (playerItems == null)
                playerItems = FindFirstObjectByType<PlayerItemsRuntime>();
        }

        private void OnEnable()
        {
            RebuildSlots();
            Refresh();
        }

        public void SetPlayer(PlayerItemsRuntime items)
        {
            playerItems = items;
            RebuildSlots();
            Refresh();
        }

        public void RebuildSlots()
        {
            _slots.Clear();

            if (slotsRoot == null || slotPrefab == null || playerItems == null)
                return;

            InventoryGrid inv = playerItems.Inventory;
            if (inv == null)
                return;

            int capacity = inv.Capacity; // InventoryGrid drives capacity
            // Clear existing children
            for (int i = slotsRoot.childCount - 1; i >= 0; i--)
                Destroy(slotsRoot.GetChild(i).gameObject);

            for (int i = 0; i < capacity; i++)
            {
                GameObject go = Instantiate(slotPrefab, slotsRoot);

                var r = new SlotRefs();
                r.Root = go;
                r.Frame = FindImage(go.transform, "Frame");
                r.Bg = FindImage(go.transform, "BG");
                r.Icon = FindImage(go.transform, "Icon");
                r.Count = FindText(go.transform, "Count");

                _slots.Add(r);
            }
        }

        public void Refresh()
        {
            if (playerItems == null) return;
            ItemsDb db = playerItems.ItemsDb;
            InventoryGrid inv = playerItems.Inventory;

            if (db == null || inv == null) return;

            var stacks = inv.Slots;
            if (stacks == null) return;

            int n = Mathf.Min(stacks.Length, _slots.Count);

            for (int i = 0; i < n; i++)
            {
                var ui = _slots[i];
                var s = stacks[i];

                if (s.IsEmpty || s.ItemId <= 0)
                {
                    if (ui.Icon != null)
                    {
                        ui.Icon.sprite = null;
                        ui.Icon.enabled = false;
                    }

                    if (ui.Bg != null)
                        ui.Bg.color = CityRush.Items.ItemRarityColors.Common;

                    if (ui.Count != null)
                        ui.Count.gameObject.SetActive(false);

                    continue;
                }

                if (!db.TryGet(s.ItemId, out var def) || def == null)
                    continue;

                if (ui.Count != null)
                {
                    bool showCount = (def.MaxStack > 1) && (s.Count > 1);
                    ui.Count.gameObject.SetActive(showCount);

                    if (showCount)
                        ui.Count.text = s.Count.ToString();
                }

                // Icon
                if (ui.Icon != null)
                {
                    var icon = string.IsNullOrWhiteSpace(def.IconKey)
                        ? null
                        : Resources.Load<Sprite>(def.IconKey);

                    Debug.Log($"[InventoryGuiBinder] slot={i} itemId={s.ItemId} iconKey='{def.IconKey}' => sprite={(icon != null ? icon.name : "NULL")}");

                    ui.Icon.sprite = icon;
                    ui.Icon.enabled = (icon != null);
                    ui.Icon.gameObject.SetActive(icon != null);
                }

                // BG (only for weapons)
                if (ui.Bg != null)
                {
                    bool showBg = def.IsWeapon;

                    ui.Bg.gameObject.SetActive(showBg);
                    if (showBg)
                        ui.Bg.color = CityRush.Items.ItemRarityColors.Resolve(def.Rarity);
                }
            }
        }

        private static Image FindImage(Transform root, string childName)
        {
            // Direct child (most common)
            Transform t = root.Find(childName);

            // If prefab has an extra wrapper named "GridSlot"
            if (t == null)
                t = root.Find("GridSlot/" + childName);

            // Last resort: search by name anywhere under the slot
            if (t == null)
            {
                var imgs = root.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < imgs.Length; i++)
                {
                    if (imgs[i].name == childName)
                        return imgs[i];
                }

                return null;
            }

            return t.GetComponent<Image>();
        }

        private static TMP_Text FindText(Transform root, string childName)
        {
            // Direct child
            Transform t = root.Find(childName);

            // If prefab has an extra wrapper named "GridSlot"
            if (t == null)
                t = root.Find("GridSlot/" + childName);

            // If you placed Count under Frame: GridSlot/Frame/Count
            if (t == null)
                t = root.Find("Frame/" + childName);

            if (t == null)
                t = root.Find("GridSlot/Frame/" + childName);

            // Last resort: search by name anywhere under the slot
            if (t == null)
            {
                var texts = root.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i].name == childName)
                        return texts[i];
                }

                return null;
            }

            return t.GetComponent<TMP_Text>();
        }


    }
}
