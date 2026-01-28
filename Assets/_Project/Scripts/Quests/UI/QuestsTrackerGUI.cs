using System.Collections.Generic;
using UnityEngine;

namespace CityRush.Quests.UI
{
    [DisallowMultipleComponent]
    public sealed class QuestsTrackerGUI : MonoBehaviour
    {
        private IQuestService _service;
        private readonly List<int> _activeQuestIds = new();

        public void Bind(IQuestService service)
        {
            Unbind();

            _service = service;

            if (_service == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _service.OnQuestAccepted += HandleQuestChanged;
            _service.OnQuestCompleted += HandleQuestChanged;

            RefreshVisible();
        }

        public void Unbind()
        {
            if (_service != null)
            {
                _service.OnQuestAccepted -= HandleQuestChanged;
                _service.OnQuestCompleted -= HandleQuestChanged;
                _service = null;
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void HandleQuestChanged(int questId)
        {
            RefreshVisible();
        }

        private void RefreshVisible()
        {
            _activeQuestIds.Clear();

            if (_service == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _service.GetActiveQuests(_activeQuestIds);
            gameObject.SetActive(_activeQuestIds.Count > 0);
        }
    }
}
