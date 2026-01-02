using CityRush.Core;
using CityRush.Core.Prefabs;
using CityRush.Core.States;
using CityRush.World.Map;
using CityRush.World.Map.Runtime;
using CityRush.World.Interior;
using UnityEngine;

public class GameLoopState : IState
{
    private readonly Game _game;
    private readonly GameContext _context;

    private CorePrefabsRegistry _prefabs;
    private MapManager _mapManager;

    private GameLoopWorld _world;
    private GameLoopNavigation _navigation;

    // Keep same effective behavior as before (0.2f was hardcoded in LoadNextStreet).
    private const float NavSpawnGapModifier = 0.2f;

    private bool _isEnteringInterior;
    private bool _isInInterior;

    public GameLoopState(Game game, GameContext context)
    {
        _game = game;
        _context = context;
    }

    public void Enter()
    {
        _prefabs = _context.GetData<CorePrefabsRegistry>();
        _mapManager = _context.GetData<MapManager>();

        _world = new GameLoopWorld(_game, NavSpawnGapModifier);
        _world.Enter(_prefabs, _mapManager);

        _navigation = new GameLoopNavigation(_game, _world, _prefabs, _mapManager);
        _navigation.Enter();

        if (_world?.PlayerController != null)
            _world.PlayerController.OnBuildingDoorInteract += HandleBuildingDoorInteract;
    }

    public void Exit()
    {
        if (_world?.PlayerController != null)
            _world.PlayerController.OnBuildingDoorInteract -= HandleBuildingDoorInteract;

        _world?.Exit();

        _navigation = null;
        _world = null;

        _prefabs = null;
        _mapManager = null;
    }

    public void Update(float deltaTime)
    {
        if (_isEnteringInterior || _isInInterior)
            return;

        _navigation?.Tick(deltaTime);
    }

    private void HandleBuildingDoorInteract(BuildingDoor door)
    {
        if (_isEnteringInterior || _isInInterior)
            return;

        _isEnteringInterior = true;

        _world.PlayerController.Freeze();

        _world.ScreenFade.FadeOut(() =>
        {
            _world.UnloadStreet();
            _world.LoadCorridor(_prefabs.CorridorPrefab);

            _world.ScreenFade.FadeIn(() =>
            {
                _isEnteringInterior = false;
                _isInInterior = true;

                // keep frozen for now — next step will reposition + unfreeze
            });
        });
    }

}
