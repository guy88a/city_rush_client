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
    }


    [Serializable]
    public sealed class WeaponDto
    {
        public string weaponDefinitionId;
    }
}