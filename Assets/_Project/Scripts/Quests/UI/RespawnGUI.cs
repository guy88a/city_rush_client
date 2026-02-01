using System;
using UnityEngine;
using UnityEngine.UI;

namespace CityRush.UI
{
    [DisallowMultipleComponent]
    public sealed class RespawnGUI : MonoBehaviour
    {
        public event Action RespawnClicked;

        private Button _btn;

        private void Awake()
        {
            var btnTf = null as Transform;

            // Your prefab path: RespawnGUI > Button
            btnTf = transform.Find("Button");

            if (btnTf == null)
            {
                Debug.LogError("[RespawnGUI] Button not found at path: Button", this);
                return;
            }

            _btn = btnTf.GetComponent<Button>();
            if (_btn == null)
            {
                Debug.LogError("[RespawnGUI] Button is missing Button component.", this);
                return;
            }

            _btn.onClick.RemoveListener(HandleClicked);
            _btn.onClick.AddListener(HandleClicked);

            Close();
        }

        private void HandleClicked()
        {
            RespawnClicked?.Invoke();
        }

        public void Open() => gameObject.SetActive(true);
        public void Close() => gameObject.SetActive(false);
    }
}
