using UnityEngine;
using UnityEngine.SceneManagement;
using CityRush.Core.Services;

namespace CityRush.Core.States
{
    public class LoadLevelState : IState
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly GameContext _context;
        private const string SceneToLoad = "CR_10_Gameplay";
        private readonly ISceneLoaderService _sceneLoader;

        public LoadLevelState(GameStateMachine gameStateMachine, GameContext context)
        {
            _gameStateMachine = gameStateMachine;
            _context = context;
            _sceneLoader = context.Get<ISceneLoaderService>();
        }

        public void Enter()
        {

            Debug.Log("[LoadLevelState] Loading scene: " + SceneToLoad);

            _sceneLoader.Load(SceneToLoad, () => {
                Debug.Log("[LoadLevelState] Scene loaded (via service).");
                _gameStateMachine.Enter<GameLoopState>();
            });
        }

        public void Update(float deltaTime)
        {
            // probably no update needed here yet
        }


        private void OnSceneLoaded(AsyncOperation _)
        {
            Debug.Log("[LoadLevelState] Scene loaded.");
            _gameStateMachine.Enter<GameLoopState>();
        }

        public void Exit()
        {
            Debug.Log("[LoadLevelState] Exiting...");
        }
    }
}
