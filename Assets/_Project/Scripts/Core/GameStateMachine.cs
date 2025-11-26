using System;
using System.Collections.Generic;
using CityRush.Core.States;

namespace CityRush.Core
{
    public class GameStateMachine
    {
        private Dictionary<Type, IState> _states;
        private IState _activeState;

        public GameStateMachine()
        {
            _states = new Dictionary<Type, IState> {
                { typeof(BootstrapState), new BootstrapState(this) }
            };
        }

        public void Enter<TState>() where TState : class, IState
        {
            _activeState?.Exit();
            _activeState = _states[typeof(TState)];
            _activeState.Enter();
        }
    }
}
