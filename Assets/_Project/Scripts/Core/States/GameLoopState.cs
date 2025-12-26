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
    private GameObject _playerInstance;
    private Transform _playerTransform;

    private float _cameraHalfWidth;
    private float _streetLeftX;
    private float _streetRightX;

    public GameLoopState(Game game, GameContext context)
    {
        _game = game;
        _context = context;
    }

    public void Enter()
    {
        var prefabs = _context.GetData<CorePrefabsRegistry>();

        // Background (unchanged)
        _backgroundInstance = Object.Instantiate(prefabs.BackgroundPrefab);
        _backgroundInstance.CameraTransform = _game.CameraTransform;

        // Street
        _streetInstance = Object.Instantiate(prefabs.StreetPrefab);
        _streetInstance.Initialize(_game.GlobalCamera);

        TextAsset jsonAsset = Resources.Load<TextAsset>("Streets/street_01");

        var request = new StreetLoadRequest(
            streetId: "street_01",
            streetJson: jsonAsset.text
        );

        _streetInstance.Build(request);

        Camera cam = _game.GlobalCamera;
        _cameraHalfWidth = cam.orthographicSize * cam.aspect;
        _streetLeftX = _streetInstance.LeftBoundX;
        _streetRightX = _streetInstance.RightBoundX;

        _playerInstance = Object.Instantiate(prefabs.PlayerPrefab);

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
    }
}
