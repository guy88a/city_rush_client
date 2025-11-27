using System;
using CityRush.Core;
using CityRush.Core.States;
using System.Collections.Generic;

public class GameStateMachine
{
    private readonly Dictionary<Type, IState> _states;
    private IState _activeState;

    public GameStateMachine(GameContext context)
    {
        _states = new Dictionary<Type, IState> {
            { typeof(BootstrapState), new BootstrapState(this, context) },
            { typeof(LoadLevelState), new LoadLevelState(this, context) },
            { typeof(GameLoopState), new GameLoopState() }
        };
    }

    public void Enter<TState>() where TState : class, IState
    {
        _activeState?.Exit();
        _activeState = _states[typeof(TState)];
        _activeState.Enter();
    }

    public void Update(float deltaTime)
    {
        _activeState?.Update(deltaTime);
    }
}
