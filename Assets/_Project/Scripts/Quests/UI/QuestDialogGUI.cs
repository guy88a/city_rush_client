using CityRush.UI;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace CityRush.Quests.UI
{
    [DisallowMultipleComponent]
    public sealed class QuestDialogGUI : MonoBehaviour
    {
        private IQuestService _quests;

        private GameObject _pageWelcome;
        private GameObject _pageQuest;

        private TMP_Text _titleText;     // “Mr. Detective”
        private TMP_Text _greetingText;  // “Greetings...”

        private Transform _availableContainer;
        private Transform _activeContainer;

        private GameObject _availableTemplate;
        private GameObject _activeTemplate;

        private TMP_Text _questTitleText;
        private TMP_Text _questDescText;

        private Button _btnClose;
        private Button _btnAccept;
        private Button _btnDecline;
        private Button _btnComplete;
        private Button _btnReturn;

        private readonly List<GameObject> _spawned = new();
        private readonly List<int> _tmp = new();

        private int _npcId = -1;
        private int _selectedQuestId = -1;

        private NpcDialogRuntime _runtime;

        public void Bind(IQuestService quests) => _quests = quests;

        private void Awake()
        {
            CacheRefs();

            _runtime = GetComponent<NpcDialogRuntime>();
            if (_runtime != null)
            {
                _runtime.OpenedForNpc += Open;
                _runtime.Closed += Close;
            }

            WireButtons();
            SetOpen(false);
        }

        public void Open(int npcId)
        {
            _npcId = npcId;
            _selectedQuestId = -1;

            SetOpen(true);
            ShowWelcome();
        }

        public void Close()
        {
            _npcId = -1;
            _selectedQuestId = -1;
            SetOpen(false);
        }

        public void Refresh()
        {
            if (!gameObject.activeSelf || _npcId < 0 || _quests == null)
                return;

            if (_pageWelcome.activeSelf)
                RefreshWelcome();
            else
                RefreshQuestDetails();
        }

        private void CacheRefs()
        {
            // Root structure: Header / Content / ButtonsBar
            var header = transform.Find("Header");
            var content = transform.Find("Content");
            var buttons = transform.Find("ButtonsBar");

            _btnClose = header.Find("Button_Close").GetComponent<Button>();

            // TitleBar contains TMP somewhere
            var titleBar = header.Find("TitleBar");
            _titleText = titleBar.GetComponentInChildren<TMP_Text>(true);

            _pageWelcome = content.Find("Page_Welcome").gameObject;
            _pageQuest = content.Find("Page_Quest").gameObject;

            _greetingText = _pageWelcome.transform.Find("Text_Greeting").GetComponent<TMP_Text>();

            _availableContainer = _pageWelcome.transform.Find("Quests_Available");
            _activeContainer = _pageWelcome.transform.Find("Quests_Active");

            _availableTemplate = _availableContainer.Find("QuestListItem").gameObject;
            _activeTemplate = _activeContainer.Find("QuestListItem").gameObject;

            _questTitleText = _pageQuest.transform.Find("Text_Title").GetComponent<TMP_Text>();
            _questDescText = _pageQuest.transform.Find("Text_Description").GetComponent<TMP_Text>();

            _btnAccept = buttons.Find("Accept").GetComponent<Button>();
            _btnDecline = buttons.Find("Decline").GetComponent<Button>();
            _btnComplete = buttons.Find("Complete").GetComponent<Button>();
            _btnReturn = buttons.Find("Return").GetComponent<Button>();

            // Templates should never show as “real” rows
            _availableTemplate.SetActive(false);
            _activeTemplate.SetActive(false);
        }

        private void WireButtons()
        {
            _btnClose.onClick.RemoveAllListeners();
            _btnClose.onClick.AddListener(() =>
            {
                if (_runtime != null) _runtime.Close();
                else Close();
            });

            _btnAccept.onClick.RemoveAllListeners();
            _btnAccept.onClick.AddListener(AcceptSelected);

            _btnDecline.onClick.RemoveAllListeners();
            _btnDecline.onClick.AddListener(ReturnFromQuest);

            _btnComplete.onClick.RemoveAllListeners();
            _btnComplete.onClick.AddListener(CompleteSelected);

            _btnReturn.onClick.RemoveAllListeners();
            _btnReturn.onClick.AddListener(() =>
            {
                // Return behaves differently per page:
                // - From Welcome -> close dialog
                // - From Quest   -> back to Welcome
                if (_pageQuest.activeSelf)
                    ReturnFromQuest();
                else
                    Close();
            });
        }

        private void OnDestroy()
        {
            if (_runtime != null)
            {
                _runtime.OpenedForNpc -= Open;
                _runtime.Closed -= Close;
            }
        }

        private void SetOpen(bool isOpen)
        {
            gameObject.SetActive(isOpen);
        }

        private void ShowWelcome()
        {
            _pageQuest.SetActive(false);
            _pageWelcome.SetActive(true);

            // Buttons state
            _btnAccept.gameObject.SetActive(false);
            _btnDecline.gameObject.SetActive(false);
            _btnComplete.gameObject.SetActive(false);
            _btnReturn.gameObject.SetActive(true);

            RefreshWelcome();
        }

        private void ShowQuest(int questId)
        {
            _selectedQuestId = questId;

            _pageWelcome.SetActive(false);
            _pageQuest.SetActive(true);

            RefreshQuestDetails();
        }

        private void RefreshWelcome()
        {
            if (_quests == null) return;

            if (_greetingText != null)
                _greetingText.text = "Greetings...";

            ClearSpawned();

            // Available offers
            _tmp.Clear();
            _quests.GetNpcQuestOffers(_npcId, _tmp);
            BuildList(_availableContainer, _availableTemplate, _tmp);

            // Active
            _tmp.Clear();
            _quests.GetNpcActiveQuests(_npcId, _tmp);
            BuildList(_activeContainer, _activeTemplate, _tmp);
        }

        private void RefreshQuestDetails()
        {
            if (_selectedQuestId < 0 || _quests == null)
                return;

            if (_quests.TryGetDefinition(_selectedQuestId, out var def))
            {
                _questTitleText.text = def.Title;

                var stage = _quests.GetStage(_selectedQuestId);

                _questDescText.text = stage switch
                {
                    QuestStage.Available => def.DescAvailable,
                    QuestStage.InProgress => def.DescInProgress,
                    QuestStage.ReadyToTurnIn => def.DescFinished,
                    QuestStage.Completed => def.DescFinished,
                    _ => def.DescAvailable, // Locked / fallback
                };

                bool canAccept = stage == QuestStage.Available;
                bool canComplete = stage == QuestStage.ReadyToTurnIn;

                _btnAccept.gameObject.SetActive(canAccept);
                _btnDecline.gameObject.SetActive(canAccept);
                _btnComplete.gameObject.SetActive(canComplete);
                _btnReturn.gameObject.SetActive(false);
            }
        }


        private void BuildList(Transform container, GameObject template, List<int> questIds)
        {
            // Hide section if empty
            container.gameObject.SetActive(questIds.Count > 0);

            for (int i = 0; i < questIds.Count; i++)
            {
                int questId = questIds[i];
                var go = Instantiate(template, container);
                go.name = "QuestListItem_Runtime";
                go.SetActive(true);

                SetupListItem(go.transform, questId);

                _spawned.Add(go);
            }
        }

        private void SetupListItem(Transform item, int questId)
        {
            // Children: Available / InProgress / Finished / Name
            var iconAvailable = item.Find("Available")?.gameObject;
            var iconInProgress = item.Find("InProgress")?.gameObject;
            var iconFinished = item.Find("Finished")?.gameObject;

            var nameText = item.Find("Name")?.GetComponent<TMP_Text>();

            if (_quests.TryGetDefinition(questId, out var def) && nameText != null)
                nameText.text = def.Title;

            var stage = _quests.GetStage(questId);

            if (iconAvailable) iconAvailable.SetActive(stage == QuestStage.Available);
            if (iconInProgress) iconInProgress.SetActive(stage == QuestStage.InProgress);
            if (iconFinished) iconFinished.SetActive(stage == QuestStage.ReadyToTurnIn || stage == QuestStage.Completed);

            // Make the whole row clickable
            var btn = item.GetComponent<Button>();
            if (btn == null) btn = item.gameObject.AddComponent<Button>();

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => ShowQuest(questId));
        }

        private void ClearSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++)
                Destroy(_spawned[i]);
            _spawned.Clear();
        }

        private void ReturnFromQuest()
        {
            _selectedQuestId = -1;
            ShowWelcome();
        }

        private void AcceptSelected()
        {
            if (_selectedQuestId < 0 || _quests == null)
                return;

            _quests.TryAccept(_selectedQuestId);
            ShowWelcome();
        }

        private void CompleteSelected()
        {
            if (_selectedQuestId < 0 || _quests == null)
                return;

            _quests.TryTurnIn(_selectedQuestId, _npcId);
            ShowWelcome();
        }
    }
}
