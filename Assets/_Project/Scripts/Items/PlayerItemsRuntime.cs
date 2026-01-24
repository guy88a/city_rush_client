using System.Collections.Generic;
using UnityEngine;
using CityRush.Units.Characters.Combat;


namespace CityRush.Items
{
    [DisallowMultipleComponent]
    public sealed class PlayerItemsRuntime : MonoBehaviour
    {
        public ItemsDb ItemsDb { get; private set; }
        public bool HasItemsDb => ItemsDb != null;


        [Header("Runtime Refs (optional)")]
        [SerializeField] private Wallet wallet;
        [SerializeField] private InventoryGrid inventory;

        [SerializeField] private CityRush.Units.Characters.Combat.Health health;


        public Wallet Wallet => wallet;
        public InventoryGrid Inventory => inventory;


        private readonly Dictionary<WeaponDefinition, int> _weaponItemIdByDef = new(8);


        private void Awake()
        {
            if (wallet == null) wallet = GetComponent<Wallet>();
            if (inventory == null) inventory = GetComponent<InventoryGrid>();
            if (health == null) health = GetComponent<CityRush.Units.Characters.Combat.Health>();
        }


        public void Init(ItemsDb db)
        {
            ItemsDb = db;
            BuildWeaponReverseCache();
        }


        public bool TryGetWeaponItemId(WeaponDefinition weaponDef, out int itemId)
        {
            itemId = 0;
            if (weaponDef == null) return false;
            return _weaponItemIdByDef.TryGetValue(weaponDef, out itemId);
        }


        private void BuildWeaponReverseCache()
        {
            _weaponItemIdByDef.Clear();


            if (ItemsDb == null)
                return;


            foreach (KeyValuePair<int, ItemDefinition> kv in ItemsDb.EnumerateAll())
            {
                ItemDefinition itemDef = kv.Value;
                if (itemDef == null || !itemDef.IsWeapon)
                    continue;


                string weaponDefinitionId = itemDef.Weapon.WeaponDefinitionId;
                if (string.IsNullOrWhiteSpace(weaponDefinitionId))
                    continue;


                WeaponDefinition weaponDef = Resources.Load<WeaponDefinition>(weaponDefinitionId);
                if (weaponDef == null)
                    continue;


                if (!_weaponItemIdByDef.ContainsKey(weaponDef))
                    _weaponItemIdByDef.Add(weaponDef, kv.Key);
            }
        }


        public bool AddToken(string tokenKey, int amount)
        {
            if (wallet == null) return false;
            if (amount <= 0) return false;


            wallet.Add(tokenKey, amount);
            return true;
        }


        // Returns remainder (0 = fully added).
        public int TryAddToInventory(int itemId, int amount)
        {
            if (inventory == null) return amount;
            if (ItemsDb == null) return amount;


            return inventory.TryAdd(itemId, amount, ItemsDb);
        }

        public bool TryUseHealingPotion(int healingPotionItemId)
        {
            if (ItemsDb == null) return false;
            if (inventory == null) return false;
            if (health == null) return false;

            if (!ItemsDb.TryGet(healingPotionItemId, out var def))
                return false;

            // v1 rule: identify by ItemId + category (no consumable schema yet).
            if (!def.Category.Trim().Equals("Consumable", System.StringComparison.OrdinalIgnoreCase))
                return false;

            if (!TryConsumeFromInventory(healingPotionItemId, 1))
                return false;

            health.Heal(10);
            Debug.Log($"[Consumable] Used '{def.Name}' (+10 HP).", this);
            return true;
        }

        private bool TryConsumeFromInventory(int itemId, int count)
        {
            if (count <= 0) return true;
            ItemStack[] slots = inventory.Slots;
            if (slots == null) return false;

            for (int i = 0; i < slots.Length && count > 0; i++)
            {
                ItemStack s = slots[i];
                if (s.IsEmpty) continue;
                if (s.ItemId != itemId) continue;

                int take = (s.Count < count) ? s.Count : count;
                s.Count -= take;
                count -= take;

                if (s.Count <= 0)
                    s.Clear();

                slots[i] = s; // IMPORTANT: struct write-back
            }

            return count == 0;
        }


        public void DebugPrintInventory()
        {
            if (inventory == null)
            {
                Debug.Log("[Inventory] inventory ref is null.", this);
                return;
            }

            ItemStack[] slots = inventory.Slots;
            if (slots == null)
            {
                Debug.Log("[Inventory] slots is null.", this);
                return;
            }

            Debug.Log($"[Inventory] capacity={slots.Length}", this);

            for (int i = 0; i < slots.Length; i++)
            {
                ItemStack s = slots[i];
                if (s.IsEmpty) continue;

                string name = ItemsDb != null && ItemsDb.TryGet(s.ItemId, out var def) ? def.Name : "<?>";

                Debug.Log($"[Inventory] slot[{i}] itemId={s.ItemId} name={name} count={s.Count}", this);
            }
        }

    }
}