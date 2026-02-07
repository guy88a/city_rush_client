using UnityEngine;
using CityRush.Items;

namespace CityRush.Units
{
    [DisallowMultipleComponent]
    public sealed class NpcDbHost : MonoBehaviour
    {
        [SerializeField] private NpcDB npcDb;
        [SerializeField] private NpcVisualsDB npcVisualsDb;

        // Optional: if Player exists in the scene, you can drag it here.
        // If not, we will resolve it at runtime when the Player gets spawned.
        [SerializeField] private PlayerItemsRuntime playerItems;

        public static NpcDbHost Instance { get; private set; }

        public NpcDB NpcDb => npcDb;
        public NpcVisualsDB NpcVisualsDb => npcVisualsDb;

        public ItemsDb ItemsDb
        {
            get
            {
                EnsurePlayerItemsResolved();
                return playerItems != null ? playerItems.ItemsDb : null;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[NpcDbHost] Duplicate instance, destroying.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (npcDb == null)
                Debug.LogError("[NpcDbHost] NpcDB is not assigned.", this);

            if (npcVisualsDb == null)
                Debug.LogWarning("[NpcDbHost] NpcVisualsDB is not assigned.", this);

            EnsurePlayerItemsResolved();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void EnsurePlayerItemsResolved()
        {
            if (playerItems != null)
                return;

            // Player is spawned later -> resolve on-demand.
            playerItems = Object.FindAnyObjectByType<PlayerItemsRuntime>(FindObjectsInactive.Include);
        }
    }
}
