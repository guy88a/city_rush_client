using UnityEngine;
using CityRush.Items;
using CityRush.Units.Characters.Combat;

namespace CityRush.Items.World
{
    [DisallowMultipleComponent]
    public sealed class ItemPickup : MonoBehaviour
    {
        [Header("Runtime Data (debug)")]
        [SerializeField] private int itemId;
        [SerializeField] private int amount;

        [Header("Visual Refs (optional)")]
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private SpriteRenderer backgroundRenderer;

        [Header("Rarity Colors")]
        [SerializeField] private Color common = Color.white;
        [SerializeField] private Color uncommon = Color.green;
        [SerializeField] private Color rare = Color.cyan;
        [SerializeField] private Color epic = new Color(0.6f, 0.2f, 1f, 1f);
        [SerializeField] private Color legendary = new Color(1f, 0.6f, 0.1f, 1f);

        public int ItemId => itemId;
        public int Amount => amount;

        private void Awake()
        {
            CacheVisualRefsIfNeeded();
        }

        private void CacheVisualRefsIfNeeded()
        {
            if (iconRenderer == null)
            {
                Transform t = transform.Find("Icon");
                if (t != null) iconRenderer = t.GetComponent<SpriteRenderer>();
            }

            if (backgroundRenderer == null)
            {
                Transform t = transform.Find("Icon/Background");
                if (t != null) backgroundRenderer = t.GetComponent<SpriteRenderer>();
            }
        }

        // Visual-only setup.
        public void SetItem(ItemsDb db, int newItemId, int newAmount)
        {
            itemId = newItemId;
            amount = Mathf.Max(1, newAmount);

            CacheVisualRefsIfNeeded();

            if (db == null)
            {
                Debug.LogWarning("[ItemPickup] SetItem called with null ItemsDb.", this);
                return;
            }

            if (!db.TryGet(itemId, out var def))
            {
                Debug.LogWarning($"[ItemPickup] ItemId not found in ItemsDb: {itemId}", this);
                return;
            }

            // Icon
            if (iconRenderer != null)
            {
                if (!string.IsNullOrWhiteSpace(def.IconKey))
                {
                    Sprite icon = Resources.Load<Sprite>(def.IconKey);
                    if (icon != null) iconRenderer.sprite = icon;
                    else Debug.LogWarning($"[ItemPickup] Sprite not found at IconKey='{def.IconKey}' (ItemId={itemId})", this);
                }
                else
                {
                    Debug.LogWarning($"[ItemPickup] IconKey is empty for ItemId={itemId}", this);
                }
            }
            else
            {
                Debug.LogWarning("[ItemPickup] Missing Icon SpriteRenderer (expected child: Icon).", this);
            }

            // Rarity background tint
            if (backgroundRenderer != null)
                backgroundRenderer.color = ResolveRarityColor(def.Rarity);
        }

        public bool TryLoot(PlayerItemsRuntime playerItems)
        {
            if (playerItems == null) return false;

            ItemsDb db = playerItems.ItemsDb;
            if (db == null)
            {
                Debug.LogWarning("[ItemPickup] PlayerItemsRuntime has no ItemsDb.", this);
                return false;
            }

            if (!db.TryGet(itemId, out var def))
            {
                Debug.LogWarning($"[ItemPickup] ItemId not found in ItemsDb: {itemId}", this);
                return false;
            }

            // Tokens (v1: Coins only)
            if (def.Category.Trim().Equals("Token", System.StringComparison.OrdinalIgnoreCase))
            {
                // Use item name as wallet key ("Coins")
                bool ok = playerItems.AddToken(def.Name, amount);
                if (!ok) return false;

                Destroy(gameObject);
                return true;
            }

            // Weapons: equip immediately (no inventory)
            if (def.IsWeapon)
            {
                var weaponSet = playerItems.GetComponent<CharacterWeaponSet>();
                if (weaponSet == null)
                {
                    Debug.LogWarning("[ItemPickup] Weapon loot failed: player has no CharacterWeaponSet.", this);
                    return false;
                }

                bool equipped = weaponSet.TryEquipWeaponByDefinitionId(def.Weapon.WeaponDefinitionId);
                if (!equipped) return false;

                Destroy(gameObject);
                return true;
            }

            // Default: goes to inventory
            int remainder = playerItems.TryAddToInventory(itemId, amount);

            int taken = amount - remainder;
            if (taken <= 0)
                return false;

            if (remainder <= 0)
            {
                Destroy(gameObject);
                return true;
            }

            // Partial: keep pickup with remaining amount
            amount = remainder;
            return true;
        }

        private Color ResolveRarityColor(string rarity)
        {
            if (string.IsNullOrWhiteSpace(rarity))
                return common;

            switch (rarity.Trim().ToLowerInvariant())
            {
                case "common": return common;
                case "uncommon": return uncommon;
                case "rare": return rare;
                case "epic": return epic;
                case "legendary": return legendary;
                default: return common;
            }
        }
    }
}
