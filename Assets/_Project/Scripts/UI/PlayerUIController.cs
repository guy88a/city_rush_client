using CityRush.Items;
using CityRush.Items.UI;
using CityRush.Quests;
using CityRush.Quests.UI;
using CityRush.Units;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CityRush.UI
{
    [DisallowMultipleComponent]
    public sealed class PlayerUIController : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private string inventoryGuiResourcePath = "UI/Prefabs/GUIs/InventoryGUI";
        [SerializeField] private string dialogGuiResourcePath = "UI/Prefabs/GUIs/DialogGUI";

        [Header("Optional")]
        [Tooltip("Parent UI under an existing Canvas root (recommended: InGameUI). If null, controller will try to auto-find a Canvas.")]
        [SerializeField] private Transform uiRoot;

        private PlayerItemsRuntime _playerItems;

        private GameObject _inventoryGuiInstance;
        private InventoryGuiBinder _inventoryBinder;

        private GameObject _dialogGuiInstance;
        private Button _dialogCloseButton;

        private readonly List<NpcIdentity> _npcsInRange = new();
        private NpcDialogRuntime _npcDialogRuntime;
        private QuestDialogGUI _questGui;

        [Header("Quest NPC Overlap")]
        [SerializeField] private int _questNpcOverlapCount;

        private readonly HashSet<int> _overlappingNpcIds = new();
        public int CurrentNpcId { get; private set; } = -1;

        public bool IsInventoryOpen => _inventoryGuiInstance != null && _inventoryGuiInstance.activeSelf;
        public bool IsDialogOpen => _dialogGuiInstance != null && _dialogGuiInstance.activeSelf;

        private void Awake()
        {
            _playerItems = GetComponent<PlayerItemsRuntime>();

            EnsureInventoryGuiSpawned();
            SetInventoryOpen(false);

            EnsureDialogGuiSpawned();
            SetDialogOpen(false);
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.iKey.wasPressedThisFrame)
                ToggleInventory();

            if (Keyboard.current.eKey.wasPressedThisFrame && _npcsInRange.Count > 0)
            {
                var nearest = GetNearestNpcInRange();
                if (nearest != null && _npcDialogRuntime != null)
                {
                    CurrentNpcId = nearest.Id;
                    _npcDialogRuntime.OpenForNpc(CurrentNpcId);
                    _questGui.Open(CurrentNpcId);
                }
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame && IsDialogOpen)
                SetDialogOpen(false);
        }

        private NpcIdentity GetNearestNpcInRange()
        {
            var p = transform.position;
            NpcIdentity best = null;
            float bestSqr = float.PositiveInfinity;

            for (int i = 0; i < _npcsInRange.Count; i++)
            {
                var npc = _npcsInRange[i];
                if (npc == null) continue;

                float sqr = (npc.transform.position - p).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = npc;
                }
            }

            return best;
        }


        public void ToggleInventory()
        {
            SetInventoryOpen(!IsInventoryOpen);
        }

        public void SetInventoryOpen(bool open)
        {
            EnsureInventoryGuiSpawned();

            if (_inventoryGuiInstance == null)
                return;

            _inventoryGuiInstance.SetActive(open);

            if (open)
                _inventoryBinder?.Refresh();
        }

        public void SetDialogOpen(bool open)
        {
            EnsureDialogGuiSpawned();

            if (_dialogGuiInstance == null)
                return;

            _dialogGuiInstance.SetActive(open);
        }

        private void EnsureInventoryGuiSpawned()
        {
            if (_inventoryGuiInstance != null)
                return;

            if (_playerItems == null)
                _playerItems = GetComponent<PlayerItemsRuntime>();

            string path = SanitizeResourcesPath(inventoryGuiResourcePath);
            var prefab = Resources.Load<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogError($"[PlayerUIController] InventoryGUI prefab not found at Resources/{path}");
                return;
            }

            Transform parent = uiRoot != null ? uiRoot : TryFindUiRoot();

            _inventoryGuiInstance = Instantiate(prefab);

            if (parent != null)
            {
                _inventoryGuiInstance.transform.SetParent(parent, false);

                if (_inventoryGuiInstance.transform is RectTransform rt)
                {
                    rt.anchoredPosition = new Vector2(700, -150);
                    rt.localScale = Vector3.one * 0.8f;
                }
            }

            _inventoryBinder = _inventoryGuiInstance.GetComponent<InventoryGuiBinder>();
            if (_inventoryBinder == null)
                _inventoryBinder = _inventoryGuiInstance.GetComponentInChildren<InventoryGuiBinder>(true);

            if (_inventoryBinder == null)
            {
                Debug.LogError("[PlayerUIController] InventoryGuiBinder not found on InventoryGUI instance.");
                return;
            }

            if (_playerItems == null)
            {
                Debug.LogError("[PlayerUIController] PlayerItemsRuntime not found on Player.");
                return;
            }

            _inventoryBinder.SetPlayer(_playerItems);
        }

        private void EnsureDialogGuiSpawned()
        {
            if (_dialogGuiInstance != null)
                return;

            string path = SanitizeResourcesPath(dialogGuiResourcePath);
            var prefab = Resources.Load<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogError($"[PlayerUIController] DialogGUI prefab not found at Resources/{path}");
                return;
            }

            Transform parent = uiRoot != null ? uiRoot : TryFindUiRoot();

            _dialogGuiInstance = Instantiate(prefab);
            _npcDialogRuntime = _dialogGuiInstance.GetComponent<NpcDialogRuntime>();
            var questGui = _dialogGuiInstance.GetComponent<QuestDialogGUI>();
            _questGui = questGui;

            if (_questGui != null)
            {
                var host = Object.FindFirstObjectByType<QuestServiceHost>(FindObjectsInactive.Include);
                if (host != null && host.Service != null)
                    _questGui.Bind(host.Service);
                else
                    Debug.LogError("[UI] QuestServiceHost not found or Service not ready (QuestDialogGUI.Bind).");
            }

            if (parent != null)
                _dialogGuiInstance.transform.SetParent(parent, false);

            // Wire close button once.
            var closeTf = _dialogGuiInstance.transform.Find("Header/Button_Close");
            if (closeTf == null)
            {
                Debug.LogError("[PlayerUIController] Dialog close button not found at path: Header/Button_Close");
                return;
            }

            _dialogCloseButton = closeTf.GetComponent<Button>();
            if (_dialogCloseButton == null)
            {
                Debug.LogError("[PlayerUIController] Dialog close button is missing Button component.");
                return;
            }

            _dialogCloseButton.onClick.AddListener(HandleDialogCloseClicked);

            _npcDialogRuntime = _dialogGuiInstance.GetComponent<NpcDialogRuntime>();
            if (_npcDialogRuntime == null)
                _npcDialogRuntime = _dialogGuiInstance.AddComponent<NpcDialogRuntime>();

        }

        private void HandleDialogCloseClicked()
        {
            _npcDialogRuntime?.Close();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || !other.CompareTag("QuestNPC"))
                return;

            var identity = other.GetComponentInParent<NpcIdentity>();
            if (identity == null)
                return;

            if (!_npcsInRange.Contains(identity))
                _npcsInRange.Add(identity);

            // keep CurrentNpcId meaningful for debug / UI
            CurrentNpcId = identity.Id;

            Debug.Log($"[UI] Enter npcId={identity.Id} current={CurrentNpcId} count={_npcsInRange.Count}");
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other == null || !other.CompareTag("QuestNPC"))
                return;

            var identity = other.GetComponentInParent<NpcIdentity>();
            if (identity == null)
                return;

            _npcsInRange.Remove(identity);

            if (_npcsInRange.Count == 0)
            {
                CurrentNpcId = -1;
            }
            else
            {
                var nearest = GetNearestNpcInRange();
                CurrentNpcId = nearest != null ? nearest.Id : -1;
            }

            Debug.Log($"[UI] Exit npcId={identity.Id} current={CurrentNpcId} count={_npcsInRange.Count}");
        }


        private static string SanitizeResourcesPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            string p = path.Trim();
            p = p.Replace('\\', '/');

            const string resourcesPrefix = "Resources/";
            if (p.StartsWith(resourcesPrefix))
                p = p.Substring(resourcesPrefix.Length);

            if (p.EndsWith(".prefab"))
                p = p.Substring(0, p.Length - ".prefab".Length);

            return p;
        }

        private static Transform TryFindUiRoot()
        {
            var ingame = GameObject.Find("InGameUI");
            if (ingame != null)
                return ingame.transform;

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
                return canvas.transform;

            return null;
        }
    }
}
