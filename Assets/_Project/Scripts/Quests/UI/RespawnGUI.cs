using UnityEngine;
using UnityEngine.UI;

namespace CityRush.UI
{
    [DisallowMultipleComponent]
    public sealed class RespawnGUI : MonoBehaviour
    {
        private Button _btnRespawn;

        private void Awake()
        {
            CacheRefs();
            WireButtons();
            SetOpen(false);
        }

        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);

        private void CacheRefs()
        {
            var btnTr = transform.Find("Button");
            if (btnTr == null)
            {
                Debug.LogError("[RespawnGUI] Missing child 'Button' under RespawnGUI.", this);
                return;
            }

            _btnRespawn = btnTr.GetComponent<Button>();
            if (_btnRespawn == null)
                Debug.LogError("[RespawnGUI] 'Button' missing Button component.", this);
        }

        private void WireButtons()
        {
            if (_btnRespawn == null) return;

            _btnRespawn.onClick.RemoveAllListeners();
            _btnRespawn.onClick.AddListener(() =>
            {
                Debug.Log("[RespawnGUI] Respawn clicked.", this);
            });
        }

        private void SetOpen(bool isOpen) => gameObject.SetActive(isOpen);
    }
}
