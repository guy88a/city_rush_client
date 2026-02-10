using UnityEngine;
using UnityEngine.UI;
using CityRush.Core.Services;

namespace CityRush.Core.States
{
    public class MainMenuState : IState
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly GameContext _context;

        private const string SceneToLoad = "CR_05_MainMenu";
        private readonly ISceneLoaderService _sceneLoader;

        private const string MenuBgmPath = "Audio/Menu/BGM/GTA3_BGM";

        private Button _playButton;

        public MainMenuState(GameStateMachine gameStateMachine, GameContext context)
        {
            _gameStateMachine = gameStateMachine;
            _context = context;
            _sceneLoader = context.Get<ISceneLoaderService>();
        }

        public void Enter()
        {
            TryStartMenuMusic();

            Debug.Log("[MainMenuState] Loading scene: " + SceneToLoad);

            _sceneLoader.Load(SceneToLoad, () =>
            {
                Debug.Log("[MainMenuState] Scene loaded (via service).");
                BindPlayButton();
            });
        }

        public void Exit()
        {
            if (_playButton != null)
                _playButton.onClick.RemoveListener(OnPlayClicked);

            _playButton = null;

            Debug.Log("[MainMenuState] Exiting...");
        }

        public void Update(float deltaTime) { }

        private void OnPlayClicked()
        {
            _gameStateMachine.Enter<LoadLevelState>();
        }

        private void TryStartMenuMusic()
        {
            var audio = _context.Get<IAudioService>();

            var clip = Resources.Load<AudioClip>(MenuBgmPath);
            if (clip == null)
            {
                Debug.LogWarning($"[MainMenuState] Menu BGM not found at Resources/{MenuBgmPath}");
                return;
            }

            audio.SetMusicPlaylist(new[] { clip }, loopPlaylist: true);
            audio.PlayMusic();
        }

        private void BindPlayButton()
        {
            // Matches your hierarchy: Canvas/MainMenu/Menu/Buttons/Play
            var playGO = GameObject.Find("Canvas/MainMenu/Menu/Buttons/Play");

            if (playGO == null)
            {
                // Fallback: search by name
                var buttons = Object.FindObjectsOfType<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i].name == "Play")
                    {
                        _playButton = buttons[i];
                        break;
                    }
                }
            }
            else
            {
                _playButton = playGO.GetComponent<Button>();
            }

            if (_playButton == null)
            {
                Debug.LogError("[MainMenuState] Play button not found/bind failed.");
                return;
            }

            _playButton.onClick.RemoveListener(OnPlayClicked);
            _playButton.onClick.AddListener(OnPlayClicked);

            Debug.Log("[MainMenuState] Play button bound.");
        }
    }
}
