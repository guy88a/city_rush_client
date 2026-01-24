namespace CityRush.Items
{
    public sealed class ItemDefinition
    {
        public int ItemId { get; }
        public string Name { get; }
        public string Category { get; }
        public string Rarity { get; }
        public string IconKey { get; }
        public int MaxStack { get; }


        public WeaponData Weapon { get; }
        public bool IsWeapon => Weapon != null;

        public ConsumableData Consumable { get; }
        public bool IsConsumable => Consumable != null;


        public ItemDefinition(
        int itemId,
        string name,
        string category,
        string rarity,
        string iconKey,
        int maxStack,
        WeaponData weapon,
        ConsumableData consumable
        )
        {
            ItemId = itemId;
            Name = name ?? string.Empty;
            Category = category ?? string.Empty;
            Rarity = rarity ?? string.Empty;
            IconKey = iconKey ?? string.Empty;
            MaxStack = maxStack;
            Weapon = weapon;
            Consumable = consumable;
        }


        public sealed class WeaponData
        {
            public string WeaponDefinitionId { get; }


            public WeaponData(string weaponDefinitionId)
            {
                WeaponDefinitionId = weaponDefinitionId ?? string.Empty;
            }
        }

        public sealed class ConsumableData
        {
            public string EffectType { get; }
            public int Amount { get; }

            public ConsumableData(string effectType, int amount)
            {
                EffectType = effectType ?? string.Empty;
                Amount = amount;
            }
        }
    }
}