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

    private bool _isExitingInterior;

    private Vector3 _returnStreetPlayerPos;
    private Vector3 _returnStreetCameraPos;

    private CorridorExitTrigger _corridorExitTrigger;

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

        if (_corridorExitTrigger != null)
            _corridorExitTrigger.ExitRequested -= HandleCorridorExitRequested;

        _corridorExitTrigger = null;

        _world?.Exit();

        _navigation = null;
        _world = null;

        _prefabs = null;
        _mapManager = null;
    }

    public void Update(float deltaTime)
    {
        if (_isEnteringInterior || _isExitingInterior || _isInInterior)
            return;

        _navigation?.Tick(deltaTime);
    }

    private void HandleBuildingDoorInteract(BuildingDoor door)
    {
        if (_isEnteringInterior || _isInInterior)
            return;

        _returnStreetPlayerPos = _world.PlayerTransform.position;
        _returnStreetCameraPos = _game.CameraTransform.position;

        _isEnteringInterior = true;

        _world.PlayerController.Freeze();

        _world.ScreenFade.FadeOut(() =>
        {
            _world.UnloadStreet();
            _world.LoadCorridor(_prefabs.CorridorPrefab);
            _world.CenterCorridorOnCamera();
            _world.RepositionPlayerForCorridorSpawn();

            _world.ScreenFade.FadeIn(() =>
            {
                _isEnteringInterior = false;
                _isInInterior = true;

                BindCorridorExitTrigger();

                _world.PlayerController.Unfreeze();
            });
        });
    }

    private void BindCorridorExitTrigger()
    {
        if (_corridorExitTrigger != null)
            _corridorExitTrigger.ExitRequested -= HandleCorridorExitRequested;

        _corridorExitTrigger = Object.FindFirstObjectByType<CorridorExitTrigger>();

        if (_corridorExitTrigger != null)
            _corridorExitTrigger.ExitRequested += HandleCorridorExitRequested;
    }

    private void HandleCorridorExitRequested()
    {
        if (_isEnteringInterior || _isExitingInterior || !_isInInterior)
            return;

        _isExitingInterior = true;
        _world.PlayerController.Freeze();

        _world.ScreenFade.FadeOut(() =>
        {
            if (_corridorExitTrigger != null)
                _corridorExitTrigger.ExitRequested -= HandleCorridorExitRequested;

            _corridorExitTrigger = null;

            // Load the same street (no CommitMove)
            StreetRef streetRef = _mapManager.GetCurrentStreet();
            _world.LoadStreet(_prefabs, streetRef);

            // Restore player + camera where they were before entering corridor
            _world.PlayerTransform.position = _returnStreetPlayerPos;
            _game.CameraTransform.position = _returnStreetCameraPos;

            // Reset navigation state cleanly
            _navigation.Enter();

            _world.ScreenFade.FadeIn(() =>
            {
                _isInInterior = false;
                _isExitingInterior = false;

                _world.PlayerController.Unfreeze();
            });
        });
    }

}
