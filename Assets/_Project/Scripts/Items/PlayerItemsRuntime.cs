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


        public Wallet Wallet => wallet;
        public InventoryGrid Inventory => inventory;


        private readonly Dictionary<WeaponDefinition, int> _weaponItemIdByDef = new(8);


        private void Awake()
        {
            if (wallet == null) wallet = GetComponent<Wallet>();
            if (inventory == null) inventory = GetComponent<InventoryGrid>();
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
    }
}