using CityRush.Core.Services;
using CityRush.World.Map;
using CityRush.World.Map.Runtime;
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

            // Load raw map data
            TextAsset json = Resources.Load<TextAsset>("Maps/LibertyState");
            var mapData = JsonUtility.FromJson<MapData>(json.text);

            // Create map runtime manager
            var mapManager = new MapManager(mapData);

            // Register into context
            _context.Set(mapManager);

            // (Optional) Do NOT expose MapData anymore
            // _context.Set(mapData); // intentionally removed

            _gameStateMachine.Enter<LoadLevelState>();
        }

        public void Exit()
        {
            Debug.Log("[BootstrapState] Exited.");
        }

        public void Update(float deltaTime) { }
    }
}
