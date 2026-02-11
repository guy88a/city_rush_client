using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CityRush.Core.Services;

namespace CityRush.UI
{
    public sealed class UIButtonClickSound : MonoBehaviour
    {
        private IAudioService _audio;

        private AudioClip _downClip;
        private AudioClip _upClip;

        private const string AudioRootName = "__AudioRoot";

        private void Awake()
        {
            _downClip = Resources.Load<AudioClip>("Audio/UI/click_down");
            _upClip = Resources.Load<AudioClip>("Audio/UI/click_up");

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            HookAllButtons();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HookAllButtons();
        }

        private void HookAllButtons()
        {
            _audio ??= FindAudioService();
            if (_audio == null)
                return;

            var buttons = FindObjectsOfType<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];

                // Up sound on click (release)
                btn.onClick.RemoveListener(OnAnyButtonClick);
                btn.onClick.AddListener(OnAnyButtonClick);

                // Down sound on press
                var relay = btn.GetComponent<UIButtonPointerDownUpRelay>();
                if (relay == null)
                    relay = btn.gameObject.AddComponent<UIButtonPointerDownUpRelay>();

                relay.OnDown -= OnAnyButtonDown;
                relay.OnDown += OnAnyButtonDown;

                // Optional: if you prefer "up" exactly on pointer up instead of onClick, use this:
                // relay.OnUp -= OnAnyButtonUp;
                // relay.OnUp += OnAnyButtonUp;
            }

        }

        private void OnAnyButtonDown()
        {
            _audio ??= FindAudioService();
            if (_audio == null)
                return;

            PlayUi(_downClip);
        }

        private void OnAnyButtonClick()
        {
            _audio ??= FindAudioService();
            if (_audio == null)
                return;

            // "Up" sound on click (release)
            PlayUi(_upClip);
        }

        private void PlayUi(AudioClip clip)
        {
            if (clip == null)
                return;

            _audio.PlayOneShot(
                SoundCategory.UI,
                clip,
                volume01: 0.8f,
                pitchMin: 0.95f,
                pitchMax: 1.05f
            );
        }

        private IAudioService FindAudioService()
        {
            var root = GameObject.Find(AudioRootName);
            if (root == null)
                return null;

            var host = root.GetComponent<AudioServiceHost>();
            if (host == null)
                return null;

            return host.GetAudio();
        }
    }
}
