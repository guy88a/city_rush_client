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

        private Button _controlsButton;
        private Button _returnButton;
        private GameObject _controlsRoot;

        private Button _exitButton;

        private void OnExitClicked()
        {
        #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
        }

        private void BindExitButton()
        {
            var exitGO = GameObject.Find("Canvas/MainMenu/Menu/Buttons/Exit");
            if (exitGO == null)
            {
                Debug.LogError("[MainMenuState] Exit button not found at Canvas/MainMenu/Menu/Buttons/Exit");
                return;
            }

            _exitButton = exitGO.GetComponent<Button>();
            if (_exitButton == null)
            {
                Debug.LogError("[MainMenuState] Exit Button component missing.");
                return;
            }

            _exitButton.onClick.RemoveListener(OnExitClicked);
            _exitButton.onClick.AddListener(OnExitClicked);
        }

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
                BindControlsUI();
                BindExitButton();
            });
        }

        public void Exit()
        {
            if (_playButton != null)
                _playButton.onClick.RemoveListener(OnPlayClicked);

            if (_controlsButton != null)
                _controlsButton.onClick.RemoveListener(OnControlsClicked);

            if (_returnButton != null)
                _returnButton.onClick.RemoveListener(OnReturnClicked);

            if (_exitButton != null)
                _exitButton.onClick.RemoveListener(OnExitClicked);

            _playButton = null;
            _controlsButton = null;
            _returnButton = null;
            _controlsRoot = null;

            Debug.Log("[MainMenuState] Exiting...");
        }

        public void Update(float deltaTime) { }

        private void OnPlayClicked()
        {
            _gameStateMachine.Enter<LoadLevelState>();
        }

        private void OnControlsClicked()
        {
            if (_controlsRoot != null)
                _controlsRoot.SetActive(true);
        }

        private void OnReturnClicked()
        {
            if (_controlsRoot != null)
                _controlsRoot.SetActive(false);
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
            var playGO = GameObject.Find("Canvas/MainMenu/Menu/Buttons/Play");

            if (playGO == null)
            {
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

        private void BindControlsUI()
        {
            _controlsRoot = GameObject.Find("Canvas/MainMenu/Controls");
            if (_controlsRoot == null)
            {
                Debug.LogError("[MainMenuState] Controls root not found at Canvas/MainMenu/Controls");
                return;
            }

            var controlsBtnGO = GameObject.Find("Canvas/MainMenu/Menu/Buttons/Controls");
            if (controlsBtnGO == null)
            {
                Debug.LogError("[MainMenuState] Controls button not found at Canvas/MainMenu/Menu/Buttons/Controls");
                return;
            }

            var returnBtnGO = GameObject.Find("Canvas/MainMenu/Controls/Return");
            if (returnBtnGO == null)
            {
                Debug.LogError("[MainMenuState] Return button not found at Canvas/MainMenu/Controls/Return");
                return;
            }

            _controlsButton = controlsBtnGO.GetComponent<Button>();
            _returnButton = returnBtnGO.GetComponent<Button>();

            if (_controlsButton == null || _returnButton == null)
            {
                Debug.LogError("[MainMenuState] Controls/Return Button component missing.");
                return;
            }

            _controlsRoot.SetActive(false);

            _controlsButton.onClick.RemoveListener(OnControlsClicked);
            _controlsButton.onClick.AddListener(OnControlsClicked);

            _returnButton.onClick.RemoveListener(OnReturnClicked);
            _returnButton.onClick.AddListener(OnReturnClicked);

            Debug.Log("[MainMenuState] Controls UI bound.");
        }
    }
}
