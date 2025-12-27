using CityRush.Core;
using CityRush.Core.States;
using CityRush.Core.Prefabs;
using CityRush.World.Background;
using CityRush.World.Street;
using UnityEngine;
using CityRush.World.Map;

public class GameLoopState : IState
{
    private readonly Game _game;
    private readonly GameContext _context;

    private MapData _mapData;
    private BackgroundRoot _backgroundInstance;
    private StreetComponent _streetInstance;

    private GameObject _playerInstance;
    private Transform _playerTransform;
    private BoxCollider2D _playerCollider;

    private float _cameraHalfWidth;
    private float _streetLeftX;
    private float _streetRightX;

    private bool _loggedLeft;
    private bool _loggedRight;

    public GameLoopState(Game game, GameContext context)
    {
        _game = game;
        _context = context;
    }

    public void Enter()
    {
        var prefabs = _context.GetData<CorePrefabsRegistry>();
        _mapData = _context.GetData<MapData>();

        // Background (unchanged)
        _backgroundInstance = Object.Instantiate(prefabs.BackgroundPrefab);
        _backgroundInstance.CameraTransform = _game.CameraTransform;

        // Street
        _streetInstance = Object.Instantiate(prefabs.StreetPrefab);
        _streetInstance.Initialize(_game.GlobalCamera);

        StreetRef streetRef = _mapData
        .Zones[0]                 // IronCity
        .Structure[1]             // Row 1 = Downtown
        .Streets[0];              // Column 0 = DT_Street_0

        TextAsset jsonAsset = Resources.Load<TextAsset>($"Maps/{streetRef.JsonPath}");

        var request = new StreetLoadRequest(
            streetId: streetRef.StreetId,
            streetJson: jsonAsset.text
        );

        _streetInstance.Build(request);

        Camera cam = _game.GlobalCamera;
        _cameraHalfWidth = cam.orthographicSize * cam.aspect;
        _streetLeftX = _streetInstance.LeftBoundX;
        _streetRightX = _streetInstance.RightBoundX;

        _playerInstance = Object.Instantiate(prefabs.PlayerPrefab);
        _playerCollider = _playerInstance.GetComponent<BoxCollider2D>();

        float spawnX = _streetInstance.SpawnX;
        float groundY = 0f;

        _playerInstance.transform.position = new Vector3(spawnX, groundY, 0f);

        _playerTransform = _playerInstance.transform;
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
        if (_playerTransform == null)
            return;

        float minX = _streetLeftX + _cameraHalfWidth;
        float maxX = _streetRightX - _cameraHalfWidth;

        float targetX = _playerTransform.position.x;
        float clampedX = Mathf.Clamp(targetX, minX, maxX);

        Vector3 camPos = _game.CameraTransform.position;
        camPos.x = clampedX;
        _game.CameraTransform.position = camPos;

        // Street Navigation
        if (_playerCollider == null)
            return;

        // Camera visible bounds
        float cameraCenterX = _game.CameraTransform.position.x;
        float cameraLeftX = cameraCenterX - _cameraHalfWidth;
        float cameraRightX = cameraCenterX + _cameraHalfWidth;

        // Player collider bounds
        Bounds b = _playerCollider.bounds;
        float playerCenterX = _playerCollider.bounds.center.x;

        // Left
        if (!_loggedLeft && playerCenterX < cameraLeftX)
        {
            Debug.Log("Left");
            _loggedLeft = true;
        }

        // Right
        if (!_loggedRight && playerCenterX > cameraRightX)
        {
            Debug.Log("Right");
            _loggedRight = true;
        }

        // Reset
        if (playerCenterX >= cameraLeftX)
            _loggedLeft = false;

        if (playerCenterX <= cameraRightX)
            _loggedRight = false;
    }
}
