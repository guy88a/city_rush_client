using CityRush.Core.Services;
using UnityEngine;

namespace CityRush.Core.States
{
    public class PauseMenuState : IState
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly GameContext _context;

        private float _prevTimeScale;

        public PauseMenuState(GameStateMachine gameStateMachine, GameContext context)
        {
            _gameStateMachine = gameStateMachine;
            _context = context;
        }

        public void Enter()
        {
            Debug.Log("[PauseMenuState] Entered.");

            _context.Get<IAudioService>().PauseAllAmbient(true);

            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            // Stub:
            // - Later: spawn Pause Menu UI root here.
            // - UI "Resume" will call: _gameStateMachine.Enter<GameLoopState>();
        }

        public void Exit()
        {
            Time.timeScale = _prevTimeScale;

            _context.Get<IAudioService>().PauseAllAmbient(false);

            // Later: destroy Pause Menu UI root here.
            Debug.Log("[PauseMenuState] Exited.");
        }

        public void Update(float deltaTime) { }
    }
}
