using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CityRush.Quests.UI
{
    [DisallowMultipleComponent]
    public sealed class QuestsTrackerGUI : MonoBehaviour
    {
        private IQuestService _service;

        private Transform _trackedQuests;

        private GameObject _questItemTemplate;
        private GameObject _objectiveItemTemplate;

        private readonly List<GameObject> _spawned = new();
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

            CacheRefsIfNeeded();

            _service.OnQuestAccepted += HandleQuestChanged;
            _service.OnQuestReadyToTurnIn += HandleQuestChanged;
            _service.OnQuestCompleted += HandleQuestChanged;
            _service.OnQuestProgressChanged += HandleQuestChanged;

            RefreshAll();
        }

        public void Unbind()
        {
            if (_service != null)
            {
                _service.OnQuestAccepted -= HandleQuestChanged;
                _service.OnQuestReadyToTurnIn -= HandleQuestChanged;
                _service.OnQuestCompleted -= HandleQuestChanged;
                _service.OnQuestProgressChanged -= HandleQuestChanged;
                _service = null;
            }

            ClearSpawned();
        }

        private void OnDestroy() => Unbind();

        private void HandleQuestChanged(int questId) => RefreshAll();

        private void CacheRefsIfNeeded()
        {
            if (_trackedQuests != null)
                return;

            _trackedQuests = transform.Find("TrackedQuests");

            _questItemTemplate = _trackedQuests.Find("QuestTrackerItem").gameObject;
            _questItemTemplate.SetActive(false);

            // Objective template is inside QuestTrackerItem/ObjectiveItem
            var objTemplate = _questItemTemplate.transform
                .Find("Objectives")
                .Find("ObjectiveItem");

            _objectiveItemTemplate = objTemplate.gameObject;
            _objectiveItemTemplate.SetActive(false);
        }

        private void RefreshAll()
        {
            _activeQuestIds.Clear();

            if (_service == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _service.GetActiveQuests(_activeQuestIds);

            ClearSpawned();
            BuildUI();

            gameObject.SetActive(_activeQuestIds.Count > 0);
        }

        private void ClearSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    Destroy(_spawned[i]);
            }

            _spawned.Clear();
        }

        private void BuildUI()
        {
            for (int i = 0; i < _activeQuestIds.Count; i++)
            {
                int questId = _activeQuestIds[i];

                if (!_service.TryGetDefinition(questId, out var def))
                    continue;

                var questGO = Instantiate(_questItemTemplate, _trackedQuests);
                questGO.name = $"Quest_{questId}";
                questGO.SetActive(true);
                _spawned.Add(questGO);

                // Header/Title
                var titleText = questGO.transform
                    .Find("Header")
                    .Find("Title")
                    .GetComponent<TMP_Text>();

                titleText.text = def.Title;

                // Header/Icon/InProgress + Finished
                var iconRoot = questGO.transform
                    .Find("Header")
                    .Find("Icon");

                var inProgressGO = iconRoot.Find("InProgress").gameObject;
                var finishedGO = iconRoot.Find("Finished").gameObject;

                var stage = _service.GetStage(questId);
                bool isFinished = stage == QuestStage.ReadyToTurnIn || stage == QuestStage.Completed;

                inProgressGO.SetActive(!isFinished);
                finishedGO.SetActive(isFinished);

                // Objectives list
                var objectivesRoot = questGO.transform.Find("Objectives");

                var objectives = def.Objectives;
                if (objectives == null)
                    continue;

                for (int objIndex = 0; objIndex < objectives.Length; objIndex++)
                {
                    var obj = objectives[objIndex];

                    var objGO = Instantiate(_objectiveItemTemplate, objectivesRoot);
                    objGO.name = $"Obj_{objIndex}";
                    objGO.SetActive(true);
                    _spawned.Add(objGO);

                    var descText = objGO.transform.Find("Description").GetComponent<TMP_Text>();
                    var countText = objGO.transform.Find("Count").GetComponent<TMP_Text>();

                    descText.text = obj.Text;

                    int current = _service.GetObjectiveCount(questId, objIndex);
                    int required = obj.RequiredCount;

                    if (current < 0) current = 0;
                    if (required < 0) required = 0;
                    if (current > required) current = required;

                    countText.text = $"{current}/{required}";
                }
            }
        }
    }
}
