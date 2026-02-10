using UnityEngine;
using CityRush.Core.Services;

namespace CityRush.Core.States
{
    public class LoadLevelState : IState
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly GameContext _context;

        private const string SceneToLoad = "CR_10_Gameplay";
        private readonly ISceneLoaderService _sceneLoader;

        private const string IngameBgmPath = "Audio/Ingame/BGM/MS_KC_BGM";

        public LoadLevelState(GameStateMachine gameStateMachine, GameContext context)
        {
            _gameStateMachine = gameStateMachine;
            _context = context;
            _sceneLoader = context.Get<ISceneLoaderService>();
        }

        public void Enter()
        {
            Debug.Log("[LoadLevelState] Loading scene: " + SceneToLoad);

            _sceneLoader.Load(SceneToLoad, () =>
            {
                Debug.Log("[LoadLevelState] Scene loaded (via service).");

                TryStartIngameMusic();
                _gameStateMachine.Enter<GameLoopState>();
            });
        }

        public void Update(float deltaTime)
        {
            // probably no update needed here yet
        }

        public void Exit()
        {
            Debug.Log("[LoadLevelState] Exiting...");
        }

        private void TryStartIngameMusic()
        {
            var audio = _context.Get<IAudioService>();

            var clip = Resources.Load<AudioClip>(IngameBgmPath);
            if (clip == null)
            {
                Debug.LogWarning($"[LoadLevelState] Ingame BGM not found at Resources/{IngameBgmPath}");
                return;
            }

            audio.SetMusicPlaylist(new[] { clip }, loopPlaylist: true);
            audio.PlayMusic();
        }
    }
}
