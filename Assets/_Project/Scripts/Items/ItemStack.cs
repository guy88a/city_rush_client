using System;

namespace CityRush.Items
{
    [Serializable]
    public struct ItemStack
    {
        public int ItemId;
        public int Count;

        public bool IsEmpty => ItemId <= 0 || Count <= 0;

        public void Clear()
        {
            ItemId = 0;
            Count = 0;
        }
    }
}
