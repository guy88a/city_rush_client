using System;


namespace CityRush.Items
{
    [Serializable]
    public sealed class ItemsDbDto
    {
        public int version;
        public ItemDto[] items;
    }


    [Serializable]
    public sealed class ItemDto
    {
        public int itemId;
        public string name;
        public string category;
        public string rarity;
        public string iconKey;
        public int maxStack;


        public WeaponDto weapon;
        public ConsumableDto consumable;
    }


    [Serializable]
    public sealed class WeaponDto
    {
        public string weaponDefinitionId;
    }

    public sealed class ConsumableDto
    {
        public string effectType; // "Heal"
        public int amount;        // 10
    }
}