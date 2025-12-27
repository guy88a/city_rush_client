using CityRush.Core;
using CityRush.Core.Prefabs;
using CityRush.Core.States;
using CityRush.Units.Characters.Controllers;
using CityRush.World.Background;
using CityRush.World.Map;
using CityRush.World.Map.Runtime;
using CityRush.World.Street;
using UnityEngine;

public class GameLoopState : IState
{
    private readonly Game _game;
    private readonly GameContext _context;

    private MapManager _mapManager;
    private BackgroundRoot _backgroundInstance;
    private StreetComponent _streetInstance;

    private GameObject _playerInstance;
    private Transform _playerTransform;
    private BoxCollider2D _playerCollider;
    private PlayerPlatformerController _playerController;

    private float _cameraHalfWidth;
    private float _streetLeftX;
    private float _streetRightX;

    private bool _canNavigate = false;

    // when traveling between street spawns player at new street (left/right based od direction)
    // with _cameraHalfWidth gap this modifier set gap by % (0 to 1).
    private float _navSpawnGapModifier = 0.2f;

    private enum StreetNavigationIntent
    {
        None,
        Left,
        Right
    }

    private StreetNavigationIntent _navigationIntent = StreetNavigationIntent.None;

    public GameLoopState(Game game, GameContext context)
    {
        _game = game;
        _context = context;
    }

    public void Enter()
    {
        var prefabs = _context.GetData<CorePrefabsRegistry>();
        _mapManager = _context.GetData<MapManager>();

        // Background
        _backgroundInstance = Object.Instantiate(prefabs.BackgroundPrefab);
        _backgroundInstance.CameraTransform = _game.CameraTransform;

        // Street
        _streetInstance = Object.Instantiate(prefabs.StreetPrefab);
        _streetInstance.Initialize(_game.GlobalCamera);

        StreetRef streetRef = _mapManager.GetCurrentStreet();
        TextAsset jsonAsset = Resources.Load<TextAsset>($"Maps/{streetRef.JsonPath}");

        _streetInstance.Build(new StreetLoadRequest(
            streetRef.StreetId,
            jsonAsset.text
        ));

        Camera cam = _game.GlobalCamera;
        _cameraHalfWidth = cam.orthographicSize * cam.aspect;
        _streetLeftX = _streetInstance.LeftBoundX;
        _streetRightX = _streetInstance.RightBoundX;

        // Player
        _playerInstance = Object.Instantiate(prefabs.PlayerPrefab);
        _playerTransform = _playerInstance.transform;
        _playerCollider = _playerInstance.GetComponent<BoxCollider2D>();
        _playerController = _playerInstance.GetComponent<PlayerPlatformerController>();

        float spawnX = _streetInstance.SpawnX;
        _playerTransform.position = new Vector3(spawnX, 0f, 0f);

        // Allow navigation AFTER first frame
        _canNavigate = true;
    }

    public void Exit()
    {
        if (_backgroundInstance != null)
            Object.Destroy(_backgroundInstance.gameObject);

        if (_streetInstance != null)
            Object.Destroy(_streetInstance.gameObject);

        if (_playerInstance != null)
            Object.Destroy(_playerInstance);
    }

    public void Update(float deltaTime)
    {
        // Handle transition FIRST (allowed while frozen)
        if (_navigationIntent != StreetNavigationIntent.None)
        {
            LoadNextStreet();
            return;
        }

        // Block gameplay logic while frozen
        if (_playerController.IsFrozen)
            return;

        if (_playerTransform == null || _playerCollider == null)
            return;

        // Camera follow
        float minX = _streetLeftX + _cameraHalfWidth;
        float maxX = _streetRightX - _cameraHalfWidth;

        float targetX = _playerTransform.position.x;
        float clampedX = Mathf.Clamp(targetX, minX, maxX);

        Vector3 camPos = _game.CameraTransform.position;
        camPos.x = clampedX;
        _game.CameraTransform.position = camPos;

        // Navigation only when allowed
        if (!_canNavigate)
            return;

        float cameraLeftX = camPos.x - _cameraHalfWidth;
        float cameraRightX = camPos.x + _cameraHalfWidth;
        float playerCenterX = _playerCollider.bounds.center.x;

        if (playerCenterX > cameraRightX)
        {
            _navigationIntent = StreetNavigationIntent.Right;
            _playerController.Freeze();
            _canNavigate = false;
        }
        else if (playerCenterX < cameraLeftX)
        {
            _navigationIntent = StreetNavigationIntent.Left;
            _playerController.Freeze();
            _canNavigate = false;
        }
    }

    private void LoadNextStreet()
    {
        var intent = _navigationIntent;
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
                _playerController.Unfreeze();
                _canNavigate = true;
                return;
        }

        if (!_mapManager.CanMove(direction))
        {
            _playerController.Unfreeze();
            _canNavigate = true;
            return;
        }

        // Destroy old street
        Object.Destroy(_streetInstance.gameObject);

        // Commit move
        _mapManager.CommitMove(direction);

        // Load new street
        StreetRef nextStreet = _mapManager.GetCurrentStreet();
        TextAsset jsonAsset = Resources.Load<TextAsset>($"Maps/{nextStreet.JsonPath}");

        _streetInstance = Object.Instantiate(
            _context.GetData<CorePrefabsRegistry>().StreetPrefab
        );
        _streetInstance.Initialize(_game.GlobalCamera);

        _streetInstance.Build(new StreetLoadRequest(
            nextStreet.StreetId,
            jsonAsset.text
        ));

        // Reset bounds
        _streetLeftX = _streetInstance.LeftBoundX;
        _streetRightX = _streetInstance.RightBoundX;

        // Reposition player
        float spawnX;

        if (direction == MapDirection.Right)
        {
            // Came from left > spawn near left
            spawnX = _streetInstance.LeftBoundX + (_cameraHalfWidth * 0.2f);
        }
        else // MapDirection.Left
        {
            // Came from right > spawn near right
            spawnX = _streetInstance.RightBoundX - (_cameraHalfWidth * 0.2f);
        }
        _playerTransform.position =
            new Vector3(spawnX, _playerTransform.position.y, 0f);

        // Reset camera
        Vector3 camPos = _game.CameraTransform.position;
        camPos.x = spawnX;
        _game.CameraTransform.position = camPos;

        // Resume play
        _playerController.Unfreeze();
        _canNavigate = true;
    }

}
