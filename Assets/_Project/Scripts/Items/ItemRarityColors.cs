using UnityEngine;

namespace CityRush.Items
{
    public static class ItemRarityColors
    {
        // Keep these identical to ItemPickup defaults.
        public static readonly Color Common = Color.white;
        public static readonly Color Uncommon = Color.green;
        public static readonly Color Rare = Color.cyan;
        public static readonly Color Epic = new Color(0.784f, 0.274f, 0.925f, 1f);
        public static readonly Color Legendary = new Color(1f, 0.6f, 0.1f, 1f);

        public static Color Resolve(string rarity)
        {
            if (string.IsNullOrWhiteSpace(rarity))
                return Common;

            switch (rarity.Trim().ToLowerInvariant())
            {
                case "common": return Common;
                case "uncommon": return Uncommon;
                case "rare": return Rare;
                case "epic": return Epic;
                case "legendary": return Legendary;
                default: return Common;
            }
        }
    }
}
