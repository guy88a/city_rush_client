using CityRush.World.Background;
using CityRush.World.Street;
using UnityEngine;

namespace CityRush.Core.States
{
    public class GameLoopState : IState
    {
        private BackgroundRoot _backgroundPrefab;
        private StreetComponent _streetInstance;
        private readonly Game _game;

        public GameLoopState(Game game)
        {
            _game = game;
        }

        public void Enter()
        {
            _backgroundPrefab = UnityEngine.Object.Instantiate(_game.BackgroundPrefab);
            _streetInstance = UnityEngine.Object.Instantiate(_game.StreetPrefab);
        }

        public void Update(float deltaTime) { }

        public void Exit()
        {
            
        }
    }
}
