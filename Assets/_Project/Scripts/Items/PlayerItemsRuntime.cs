using UnityEngine;

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

        private void Awake()
        {
            if (wallet == null) wallet = GetComponent<Wallet>();
            if (inventory == null) inventory = GetComponent<InventoryGrid>();
        }

        public void Init(ItemsDb db)
        {
            ItemsDb = db;
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
