using UnityEngine;

namespace CityRush.Items.World
{
    [DisallowMultipleComponent]
    public sealed class ItemPickup : MonoBehaviour
    {
        [Header("Runtime Data (debug)")]
        [SerializeField] private int itemId;
        [SerializeField] private int amount;

        private SpriteRenderer _sr;

        public int ItemId => itemId;
        public int Amount => amount;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null)
                Debug.LogError("[ItemPickup] Missing SpriteRenderer on root.", this);
        }

        // Visual-only setup. Loot logic comes later.
        public void SetItem(ItemsDb db, int newItemId, int newAmount)
        {
            itemId = newItemId;
            amount = Mathf.Max(1, newAmount);

            if (_sr == null)
                _sr = GetComponent<SpriteRenderer>();

            if (db == null)
            {
                Debug.LogWarning("[ItemPickup] SetItem called with null ItemsDb.", this);
                return;
            }

            if (!db.TryGet(itemId, out var def))
            {
                Debug.LogWarning($"[ItemPickup] ItemId not found in ItemsDb: {itemId}", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(def.IconKey))
            {
                Debug.LogWarning($"[ItemPickup] IconKey is empty for ItemId={itemId}", this);
                return;
            }

            Sprite icon = Resources.Load<Sprite>(def.IconKey);
            if (icon == null)
            {
                Debug.LogWarning($"[ItemPickup] Sprite not found at IconKey='{def.IconKey}' (ItemId={itemId})", this);
                return;
            }

            _sr.sprite = icon;
        }
    }
}
