using UnityEngine;
using CityRush.World.Street;

namespace CityRush.Core.States
{
    public class GameLoopState : IState
    {
        private StreetComponent _streetInstance;
        private readonly Game _game;

        public GameLoopState(Game game)
        {
            _game = game;
        }

        public void Enter()
        {
            _streetInstance = UnityEngine.Object.Instantiate(_game.StreetPrefab);
        }

        public void Update(float deltaTime) { }

        public void Exit()
        {
            
        }
    }
}
