using UnityEngine;

namespace CityRush.Items
{
    [DisallowMultipleComponent]
    public sealed class InventoryGrid : MonoBehaviour
    {
        public const int Columns = 5;

        [SerializeField, Min(0)]
        private int capacity = 25;

        [SerializeField]
        private ItemStack[] slots;

        public int Capacity => capacity;
        public ItemStack[] Slots => slots;

        private void Awake()
        {
            EnsureSlots();
        }

        private void OnValidate()
        {
            EnsureSlots();
        }

        private void EnsureSlots()
        {
            if (capacity < 0) capacity = 0;

            if (slots == null || slots.Length != capacity)
            {
                var newSlots = new ItemStack[capacity];

                if (slots != null)
                {
                    int copy = Mathf.Min(slots.Length, newSlots.Length);
                    for (int i = 0; i < copy; i++)
                        newSlots[i] = slots[i];
                }

                slots = newSlots;
            }
        }

        // Returns remainder (0 means fully added).
        public int TryAdd(int itemId, int amount, ItemsDb db)
        {
            if (amount <= 0)
                return 0;

            if (itemId <= 0)
                return amount;

            if (db == null || !db.TryGet(itemId, out var def))
                return amount;

            int maxStack = def.MaxStack <= 0 ? 1 : def.MaxStack;

            EnsureSlots();

            // 1) merge into existing stacks
            if (maxStack > 1)
            {
                for (int i = 0; i < slots.Length && amount > 0; i++)
                {
                    ref ItemStack s = ref slots[i];
                    if (s.IsEmpty) continue;
                    if (s.ItemId != itemId) continue;

                    int space = maxStack - s.Count;
                    if (space <= 0) continue;

                    int take = amount < space ? amount : space;
                    s.Count += take;
                    amount -= take;
                }
            }

            // 2) fill empty slots
            for (int i = 0; i < slots.Length && amount > 0; i++)
            {
                ref ItemStack s = ref slots[i];
                if (!s.IsEmpty) continue;

                int take = amount < maxStack ? amount : maxStack;

                s.ItemId = itemId;
                s.Count = take;
                amount -= take;
            }

            return amount; // remainder
        }

        // Returns remainder (0 means fully removed).
        public int TryRemove(int itemId, int amount)
        {
            if (amount <= 0)
                return 0;

            if (itemId <= 0)
                return amount;

            EnsureSlots();

            for (int i = 0; i < slots.Length && amount > 0; i++)
            {
                ref ItemStack s = ref slots[i];
                if (s.IsEmpty) continue;
                if (s.ItemId != itemId) continue;

                int take = s.Count < amount ? s.Count : amount;
                s.Count -= take;
                amount -= take;

                if (s.Count <= 0)
                    s.Clear();
            }

            return amount; // remainder
        }

    }
}
