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
        [SerializeField] private Color epic = new Color(0.784f, 0.274f, 0.925f, 1f);
        [SerializeField] private Color legendary = new Color(1f, 0.6f, 0.1f, 1f);

        public int ItemId => itemId;
        public int Amount => amount;

        [SerializeField] private Transform visualRoot;
        private GameObject _visualInstance;

        private void Awake()
        {
            CacheVisualRefsIfNeeded();

            if (visualRoot == null)
                visualRoot = transform;
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
                Debug.LogWarning($"[ItemPickup] SetItem failed: itemId not in ItemsDb: {itemId}", this);
                return;
            }

            if (_visualInstance != null)
            {
                Destroy(_visualInstance);
                _visualInstance = null;
            }

            // Pickup visual prefab (optional)
            if (!string.IsNullOrWhiteSpace(def.PickupPrefabKey))
            {
                GameObject prefab = Resources.Load<GameObject>(def.PickupPrefabKey);

                if (prefab != null)
                {
                    _visualInstance = Instantiate(prefab, transform);
                    _visualInstance.transform.localPosition = Vector3.zero;
                    _visualInstance.transform.localRotation = Quaternion.identity;
                    _visualInstance.transform.localScale = Vector3.one;
                }
                else
                {
                    Debug.LogWarning($"[ItemPickup] pickupPrefabKey not found: {def.PickupPrefabKey}", this);
                }
            }

            bool hasPrefab = _visualInstance != null;

            if (backgroundRenderer != null)
                backgroundRenderer.gameObject.SetActive(!hasPrefab);

            // Icon (fallback when no prefab visual)
            if (iconRenderer != null)
            {
                if (_visualInstance == null)
                {
                    Sprite s = Resources.Load<Sprite>(def.IconKey);
                    iconRenderer.sprite = s;
                }
                else
                {
                    iconRenderer.sprite = null; // hide icon when prefab drives visuals
                }
            }

            // Rarity background tint (only when using Background)
            if (backgroundRenderer != null && !hasPrefab)
            {
                backgroundRenderer.color = ItemRarityColors.Resolve(def.Rarity);
            }
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

                string newWeaponDefinitionId = def.Weapon.WeaponDefinitionId;
                if (string.IsNullOrWhiteSpace(newWeaponDefinitionId))
                {
                    Debug.LogWarning($"[ItemPickup] Weapon loot failed: missing WeaponDefinitionId for ItemId={itemId}.", this);
                    return false;
                }

                WeaponDefinition newWeapon = Resources.Load<WeaponDefinition>(newWeaponDefinitionId);
                if (newWeapon == null)
                {
                    Debug.LogWarning($"[ItemPickup] Weapon loot failed: WeaponDefinition not found at '{newWeaponDefinitionId}'.", this);
                    return false;
                }

                // Replace-on-loot: if slot already has a weapon of the same type, drop the currently equipped weapon.
                WeaponDefinition oldWeapon = null;
                switch (newWeapon.Type)
                {
                    case WeaponType.Uzi: oldWeapon = weaponSet.UziWeapon; break;
                    case WeaponType.Shotgun: oldWeapon = weaponSet.ShotgunWeapon; break;
                    case WeaponType.Sniper: oldWeapon = weaponSet.SniperWeapon; break;
                }

                if (oldWeapon != null && playerItems.TryGetWeaponItemId(oldWeapon, out int oldItemId))
                {
                    GameObject prefab = Resources.Load<GameObject>("Items/Prefabs/ItemPickup");
                    if (prefab == null)
                    {
                        Debug.LogWarning("[Items] Missing pickup prefab at Resources/Items/Prefabs/ItemPickup");
                    }
                    else
                    {
                        Transform parent = transform.parent != null ? transform.parent : null;

                        GameObject go = Instantiate(prefab, parent);
                        go.transform.position = transform.position;

                        ItemPickup dropped = go.GetComponent<ItemPickup>();
                        if (dropped != null)
                            dropped.SetItem(db, oldItemId, 1);
                        else
                            Debug.LogWarning("[Items] Dropped ItemPickup prefab has no ItemPickup component.");
                    }
                }

                bool equipped = weaponSet.TryEquipWeapon(newWeapon);
                if (!equipped) return false;

                Destroy(gameObject);
                return true;
            }

            // Default: goes to inventory
            int remainder = playerItems.TryAddToInventory(itemId, amount);

            int taken = amount - remainder;

            Debug.Log(
                $"[Loot] INV itemId={itemId} name='{def.Name}' cat='{def.Category}' amount={amount} taken={taken} remainder={remainder}",
                this
            );

            if (taken <= 0)
            {
                Debug.LogWarning(
                    $"[Loot] INV FAILED (took 0). Inventory likely full / cannot stack. itemId={itemId} amount={amount}",
                    this
                );
                return false;
            }
            Debug.Log("asdasdasd");
            var ui = playerItems.GetComponent<CityRush.UI.PlayerUIController>();
            if (ui != null && ui.IsInventoryOpen) ui.SetInventoryOpen(true); // refresh while open

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
