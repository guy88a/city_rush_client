using UnityEngine;

namespace CityRush.Items
{
    [DisallowMultipleComponent]
    public sealed class PlayerItemsRuntime : MonoBehaviour
    {
        public ItemsDb ItemsDb { get; private set; }
        public bool HasItemsDb => ItemsDb != null;

        public Wallet Wallet { get; private set; }
        public InventoryGrid Inventory { get; private set; }

        private void Awake()
        {
            // Player root owns the runtime containers (Option 1).
            Wallet = GetComponent<Wallet>();
            if (Wallet == null)
                Wallet = gameObject.AddComponent<Wallet>();

            Inventory = GetComponent<InventoryGrid>();
            if (Inventory == null)
                Inventory = gameObject.AddComponent<InventoryGrid>();
        }

        public void Init(ItemsDb db)
        {
            ItemsDb = db;
        }

        // Convenience wrapper (returns remainder).
        public int TryAddToInventory(int itemId, int amount)
        {
            if (Inventory == null || ItemsDb == null)
                return amount;

            return Inventory.TryAdd(itemId, amount, ItemsDb);
        }

        public void AddToWallet(string tokenKey, int amount)
        {
            Wallet?.Add(tokenKey, amount);
        }
    }
}
