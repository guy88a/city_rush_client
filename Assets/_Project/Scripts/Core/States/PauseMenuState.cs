using UnityEngine;

namespace CityRush.Core.States
{
    public class PauseMenuState : IState
    {
        private readonly GameStateMachine _gameStateMachine;
        private float _prevTimeScale;

        public PauseMenuState(GameStateMachine gameStateMachine)
        {
            _gameStateMachine = gameStateMachine;
        }

        public void Enter()
        {
            Debug.Log("[PauseMenuState] Entered.");

            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            // Stub:
            // - Later: spawn Pause Menu UI root here.
            // - UI "Resume" will call: _gameStateMachine.Enter<GameLoopState>();
        }

        public void Exit()
        {
            Time.timeScale = _prevTimeScale;

            // Later: destroy Pause Menu UI root here.

            Debug.Log("[PauseMenuState] Exited.");
        }

        public void Update(float deltaTime) { }
    }
}
