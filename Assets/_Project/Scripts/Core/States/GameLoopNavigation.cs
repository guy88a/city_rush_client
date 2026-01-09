using CityRush.Core;
using CityRush.Core.Prefabs;
using CityRush.World.Map;
using CityRush.World.Map.Runtime;
using UnityEngine;

internal sealed class GameLoopNavigation
{
    private readonly Game _game;
    private readonly GameLoopWorld _world;

    // kept for constructor compatibility (not used anymore in Step 3)
    private readonly CorePrefabsRegistry _prefabs;
    private readonly MapManager _mapManager;

    private bool _canNavigate;
    private bool _suppressNavigationThisFrame;

    private enum StreetNavigationIntent
    {
        None,
        Left,
        Right
    }

    private StreetNavigationIntent _navigationIntent = StreetNavigationIntent.None;

    public GameLoopNavigation(
        Game game,
        GameLoopWorld world,
        CorePrefabsRegistry prefabs,
        MapManager mapManager
    )
    {
        _game = game;
        _world = world;
        _prefabs = prefabs;
        _mapManager = mapManager;
    }

    public void Enter()
    {
        _navigationIntent = StreetNavigationIntent.None;
        _suppressNavigationThisFrame = false;
        _canNavigate = true;
    }

    public void Tick(float deltaTime)
    {
        // Block gameplay logic while frozen
        if (_world.PlayerController.IsFrozen)
            return;

        if (_world.PlayerTransform == null || _world.PlayerCollider == null)
            return;

        // Camera follow
        float minX = _world.StreetLeftX + _world.CameraHalfWidth;
        float maxX = _world.StreetRightX - _world.CameraHalfWidth;

        float targetX = _world.PlayerTransform.position.x;
        float clampedX = Mathf.Clamp(targetX, minX, maxX);

        Vector3 camPos = _game.CameraTransform.position;
        camPos.x = clampedX;
        _game.CameraTransform.position = camPos;

        // Suppress navigation checks for exactly one Tick (camera follow still runs)
        bool allowNav = _canNavigate && !_suppressNavigationThisFrame;

        if (_suppressNavigationThisFrame)
            _suppressNavigationThisFrame = false;

        if (!allowNav)
            return;

        float cameraLeftX = camPos.x - _world.CameraHalfWidth;
        float cameraRightX = camPos.x + _world.CameraHalfWidth;
        float playerCenterX = _world.PlayerCollider.bounds.center.x;

        if (playerCenterX > cameraRightX)
        {
            _navigationIntent = StreetNavigationIntent.Right;
            _world.PlayerController.Freeze();
            _canNavigate = false;
        }
        else if (playerCenterX < cameraLeftX)
        {
            _navigationIntent = StreetNavigationIntent.Left;
            _world.PlayerController.Freeze();
            _canNavigate = false;
        }
    }

    public bool TryConsumeStreetTransition(out MapDirection direction)
    {
        direction = default;

        if (_navigationIntent == StreetNavigationIntent.None)
            return false;

        StreetNavigationIntent intent = _navigationIntent;
        _navigationIntent = StreetNavigationIntent.None;

        switch (intent)
        {
            case StreetNavigationIntent.Right:
                direction = MapDirection.Right;
                return true;

            case StreetNavigationIntent.Left:
                direction = MapDirection.Left;
                return true;

            default:
                return false;
        }
    }

    public void EndStreetTransition()
    {
        // Always resume play after the fade completes (even if move was blocked)
        
        _canNavigate = true;

        // MUST suppress for one Tick after transition completes
        _suppressNavigationThisFrame = true;
    }
}
