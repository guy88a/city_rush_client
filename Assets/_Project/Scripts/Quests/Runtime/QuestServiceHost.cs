using UnityEngine;

namespace CityRush.Quests
{
    [DisallowMultipleComponent]
    public sealed class QuestServiceHost : MonoBehaviour
    {
        [SerializeField] private QuestDB questDb;

        private IQuestService _service;
        public IQuestService Service => _service;

        private void Awake()
        {
            if (questDb == null)
            {
                Debug.LogError("[QuestServiceHost] QuestDB is not assigned.", this);
                return;
            }

            // Create the plain C# service instance.
            _service = new QuestService(questDb);
        }
    }
}
