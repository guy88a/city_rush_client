using CityRush.Core.Services;
using CityRush.World.Map;
using UnityEngine;

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
            _context.Get<ILoggerService>()?.Info("[BootstrapState] Entered.");

            TextAsset json = Resources.Load<TextAsset>("Maps/LibertyState");
            var mapData = JsonUtility.FromJson<MapData>(json.text);
            _context.Set(mapData);

            _gameStateMachine.Enter<LoadLevelState>();
        }

        public void Exit()
        {
            Debug.Log("[BootstrapState] Exited.");
        }

        public void Update(float deltaTime) { }
    }

}
