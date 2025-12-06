using UnityEngine;
using CityRush.Core.Services;

namespace CityRush.Core.States
{
    public class BootstrapState : IState
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly GameContext _context;

        public BootstrapState(GameStateMachine gameStateMachine, GameContext context)
        {
            _gameStateMachine = gameStateMachine;
            _context = context;
        }

        public void Enter()
        {
            Debug.Log("[BootstrapState] Entered.");
            _gameStateMachine.Enter<LoadLevelState>();
            _context.Get<ILoggerService>()?.Success("Everything is good");
        }

        public void Exit()
        {
            Debug.Log("[BootstrapState] Exited.");
        }

        public void Update(float deltaTime) { }
    }

}
