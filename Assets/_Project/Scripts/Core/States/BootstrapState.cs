using UnityEngine;

namespace CityRush.Core.States
{
    public class BootstrapState : IState
    {
        private readonly GameStateMachine _gameStateMachine;

        public BootstrapState(GameStateMachine gameStateMachine)
        {
            _gameStateMachine = gameStateMachine;
        }

        public void Enter()
        {
            Debug.Log("[BootstrapState] Entered.");
        }

        public void Exit()
        {
            Debug.Log("[BootstrapState] Exited.");
        }
    }
}
