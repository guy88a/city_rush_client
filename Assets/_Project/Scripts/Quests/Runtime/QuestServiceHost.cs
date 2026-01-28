using CityRush.Items;
using UnityEngine;

namespace CityRush.Quests
{
    [DisallowMultipleComponent]
    public sealed class QuestServiceHost : MonoBehaviour
    {
        [Header("Quest DB")]
        [SerializeField] private QuestDB questDb;

        [Header("Runtime Refs (optional)")]
        [SerializeField] private PlayerItemsRuntime playerItems;

        private QuestService _service;
        public IQuestService Service => _service;

        private void Awake()
        {
            if (questDb == null)
            {
                Debug.LogError("[QuestServiceHost] QuestDB is not assigned.", this);
                return;
            }

            if (playerItems == null)
                playerItems = FindFirstObjectByType<PlayerItemsRuntime>();

            _service = new QuestService(questDb);
            _service.OnQuestRewarded += HandleQuestRewarded;
        }

        private void OnDestroy()
        {
            if (_service != null)
                _service.OnQuestRewarded -= HandleQuestRewarded;
        }

        private void HandleQuestRewarded(int questId, QuestReward reward)
        {
            if (playerItems == null)
                playerItems = Object.FindAnyObjectByType<PlayerItemsRuntime>(FindObjectsInactive.Include);

            if (playerItems == null)
            {
                Debug.LogWarning($"[QuestReward] questId={questId} ignored (no PlayerItemsRuntime found).", this);
                return;
            }

            // Tokens => Coins
            if (reward.Tokens > 0)
            {
                playerItems.AddToken("Coins", reward.Tokens);
                Debug.Log($"[QuestReward] questId={questId} +Coins(tokens)={reward.Tokens}", this);
            }

            // Optional: reward items (non-token only, amount=1 each)
            var ids = reward.RewardItemIds;
            if (ids == null || ids.Length == 0)
                return;

            var db = playerItems.ItemsDb;

            for (int i = 0; i < ids.Length; i++)
            {
                int itemId = ids[i];
                if (itemId <= 0)
                    continue;

                // Skip token items here to avoid double-granting (coins are handled by Tokens above)
                if (db != null && db.TryGet(itemId, out var def) && def != null)
                {
                    if (def.Category.Trim().Equals("Token", System.StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                int remainder = playerItems.TryAddToInventory(itemId, 1);
                Debug.Log($"[QuestReward] questId={questId} +Item itemId={itemId} amount=1 remainder={remainder}", this);
            }
        }
    }
}
