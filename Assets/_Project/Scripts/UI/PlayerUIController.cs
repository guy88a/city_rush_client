using UnityEngine;
using UnityEngine.InputSystem;
using CityRush.Items;
using CityRush.Items.UI;

namespace CityRush.UI
{
    [DisallowMultipleComponent]
    public sealed class PlayerUIController : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private string inventoryGuiResourcePath = "UI/Prefabs/GUIs/InventoryGUI";

        [Header("Optional")]
        [Tooltip("Parent UI under an existing Canvas root (recommended: InGameUI). If null, controller will try to auto-find a Canvas.")]
        [SerializeField] private Transform uiRoot;

        private PlayerItemsRuntime _playerItems;

        private GameObject _inventoryGuiInstance;
        private InventoryGuiBinder _inventoryBinder;

        public bool IsInventoryOpen => _inventoryGuiInstance != null && _inventoryGuiInstance.activeSelf;

        private void Awake()
        {
            _playerItems = GetComponent<PlayerItemsRuntime>();

            EnsureInventoryGuiSpawned();
            SetInventoryOpen(false);
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.iKey.wasPressedThisFrame)
                ToggleInventory();
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
