using CityRush.Core;
using CityRush.Core.Prefabs;
using CityRush.World.Map;
using CityRush.World.Map.Runtime;
using UnityEngine;

internal sealed class GameLoopNavigation
{
    private readonly Game _game;
    private readonly GameLoopWorld _world;
    private readonly CorePrefabsRegistry _prefabs;
    private readonly MapManager _mapManager;

    private bool _isTransitioning;
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
        _isTransitioning = false;
        _navigationIntent = StreetNavigationIntent.None;

        _suppressNavigationThisFrame = false;

        // Keep same behavior as current GameLoopState.Enter (no "one frame later" gate)
        _canNavigate = true;
    }

    public void Tick(float deltaTime)
    {
        // Handle transition FIRST (allowed while frozen)
        if (_navigationIntent != StreetNavigationIntent.None && !_isTransitioning)
        {
            _isTransitioning = true;

            _world.ScreenFade.FadeOut(() =>
            {
                LoadNextStreet();

                _world.ScreenFade.FadeIn(() =>
                {
                    _isTransitioning = false;

                    // MUST behave exactly the same as current code
                    _suppressNavigationThisFrame = false;
                });
            });

            return;
        }

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

        // Navigation only when allowed
        if (!_canNavigate || _suppressNavigationThisFrame)
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

    private void LoadNextStreet()
    {
        StreetNavigationIntent intent = _navigationIntent;
        _navigationIntent = StreetNavigationIntent.None;

        MapDirection direction;

        switch (intent)
        {
            case StreetNavigationIntent.Right:
                direction = MapDirection.Right;
                break;

            case StreetNavigationIntent.Left:
                direction = MapDirection.Left;
                break;

            default:
                _world.PlayerController.Unfreeze();
                _canNavigate = true;
                return;
        }

        if (!_mapManager.CanMove(direction))
        {
            _world.PlayerController.Unfreeze();
            _canNavigate = true;
            return;
        }

        // Destroy old street BEFORE commit move (same ordering as current)
        if (_world.Street != null)
            Object.Destroy(_world.Street.gameObject);

        // Commit move
        _mapManager.CommitMove(direction);

        // Load new street
        StreetRef nextStreet = _mapManager.GetCurrentStreet();
        _world.LoadStreet(_prefabs, nextStreet);

        // Reposition player + reset camera
        _world.RepositionPlayerForStreetEntry(direction);

        // Resume play
        _world.PlayerController.Unfreeze();
        _canNavigate = true;

        // MUST behave exactly the same as current code
        _suppressNavigationThisFrame = true;
    }
}
