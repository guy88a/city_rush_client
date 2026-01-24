using UnityEngine;

namespace CityRush.Items
{
    [DisallowMultipleComponent]
    public sealed class PlayerItemsRuntime : MonoBehaviour
    {
        public ItemsDb ItemsDb { get; private set; }
        public bool HasItemsDb => ItemsDb != null;

        public void Init(ItemsDb db)
        {
            ItemsDb = db;
        }
    }
}
