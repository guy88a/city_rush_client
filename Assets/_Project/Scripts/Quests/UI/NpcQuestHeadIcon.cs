using System.Collections.Generic;
using CityRush.Units;
using UnityEngine;

namespace CityRush.Quests
{
    [DisallowMultipleComponent]
    public sealed class NpcQuestHeadIcon : MonoBehaviour
    {
        private IQuestService _quests;
        private NpcIdentity _identity;

        private GameObject _iconNew;
        private GameObject _iconProgress;
        private GameObject _iconDone;

        private readonly List<int> _tmp = new();

        private void Awake()
        {
            _identity = GetComponentInParent<NpcIdentity>();

            _iconNew = transform.Find("QuestNew")?.gameObject;
            _iconProgress = transform.Find("QuestProgress")?.gameObject;
            _iconDone = transform.Find("QuestDone")?.gameObject;

            SetAll(false, false, false);
        }

        private void OnEnable()
        {
            TryBindIfNeeded();
            SubscribeIfPossible();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void TryBindIfNeeded()
        {
            if (_quests != null)
                return;

            var host = Object.FindFirstObjectByType<QuestServiceHost>(FindObjectsInactive.Include);
            if (host != null && host.Service != null)
                _quests = host.Service;
        }

        private void SubscribeIfPossible()
        {
            if (_quests == null)
                return;

            _quests.OnQuestAccepted += HandleQuestChanged;
            _quests.OnQuestReadyToTurnIn += HandleQuestChanged;
            _quests.OnQuestCompleted += HandleQuestChanged;
            _quests.OnQuestProgressChanged += HandleQuestChanged;
        }

        private void Unsubscribe()
        {
            if (_quests == null)
                return;

            _quests.OnQuestAccepted -= HandleQuestChanged;
            _quests.OnQuestReadyToTurnIn -= HandleQuestChanged;
            _quests.OnQuestCompleted -= HandleQuestChanged;
            _quests.OnQuestProgressChanged -= HandleQuestChanged;
        }

        private void HandleQuestChanged(int questId)
        {
            // Refresh on any quest change (prereqs can unlock offers on other NPCs).
            Refresh();
        }

        public void Refresh()
        {
            TryBindIfNeeded();

            if (_identity == null || _quests == null)
            {
                SetAll(false, false, false);
                return;
            }

            int npcId = _identity.Id;

            // Priority A: has quest to finish + player finished (ReadyToTurnIn)
            _tmp.Clear();
            _quests.GetNpcQuestTurnIns(npcId, _tmp);
            if (_tmp.Count > 0)
            {
                SetAll(false, false, true);
                return;
            }

            // Priority B: has a new quest to offer (Available)
            _tmp.Clear();
            _quests.GetNpcQuestOffers(npcId, _tmp);
            if (_tmp.Count > 0)
            {
                SetAll(true, false, false);
                return;
            }

            // Priority C: has quest to finish BUT player did NOT finish (InProgress, ends here)
            _tmp.Clear();
            _quests.GetNpcActiveQuests(npcId, _tmp);

            bool showProgress = false;

            for (int i = 0; i < _tmp.Count; i++)
            {
                int questId = _tmp[i];

                if (!_quests.TryGetDefinition(questId, out var def))
                    continue;

                if (def.EndNpcId != npcId)
                    continue;

                if (_quests.GetStage(questId) == QuestStage.InProgress)
                {
                    showProgress = true;
                    break;
                }
            }

            SetAll(false, showProgress, false);
        }

        private void SetAll(bool isNew, bool isProgress, bool isDone)
        {
            if (_iconNew != null) _iconNew.SetActive(isNew);
            if (_iconProgress != null) _iconProgress.SetActive(isProgress);
            if (_iconDone != null) _iconDone.SetActive(isDone);
        }
    }
}
