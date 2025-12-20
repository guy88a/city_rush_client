using CityRush.Core;
using CityRush.Core.States;
using CityRush.Core.Prefabs;
using CityRush.World.Background;
using CityRush.World.Street;
using UnityEngine;

public class GameLoopState : IState
{
    private readonly Game _game;
    private readonly GameContext _context;

    private BackgroundRoot _backgroundInstance;
    private StreetComponent _streetInstance;

    public GameLoopState(Game game, GameContext context)
    {
        _game = game;
        _context = context;
    }

    public void Enter()
    {
        var prefabs = _context.GetData<CorePrefabsRegistry>();

        _backgroundInstance = Object.Instantiate(prefabs.BackgroundPrefab);
        _backgroundInstance.CameraTransform = _game.CameraTransform;

        _streetInstance = Object.Instantiate(prefabs.StreetPrefab);
        _streetInstance.Initialize(_game.GlobalCamera);
    }

    public void Exit()
    {
        if (_backgroundInstance != null)
            Object.Destroy(_backgroundInstance.gameObject);

        if (_streetInstance != null)
            Object.Destroy(_streetInstance.gameObject);
    }

    public void Update(float deltaTime) { }
}
