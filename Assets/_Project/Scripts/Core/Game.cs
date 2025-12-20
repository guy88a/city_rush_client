using CityRush.Core;
using CityRush.Core.Services;
using CityRush.Core.States;
using CityRush.World.Background;
using CityRush.World.Street;
using UnityEngine;

public class Game
{
    private readonly GameStateMachine _stateMachine;
    private readonly GameContext _context;

    public BackgroundRoot BackgroundPrefab { get; }
    public StreetComponent StreetPrefab { get; }

    public Camera GlobalCamera { get; }
    public Transform CameraTransform => GlobalCamera.transform;

    public float StreetLeftBoundX { get; private set; }
    public float StreetRightBoundX { get; private set; }

    public Game(
        Camera globalCameraPrefab,
        BackgroundRoot backgroundPrefab,
        StreetComponent streetPrefab)
    {
        BackgroundPrefab = backgroundPrefab;
        StreetPrefab = streetPrefab;

        GlobalCamera = Object.Instantiate(globalCameraPrefab);
        Object.DontDestroyOnLoad(GlobalCamera.gameObject);

        _context = new GameContext();
        RegisterServices();

        _stateMachine = new GameStateMachine(this, _context);
    }

    public void Start()
    {
        _stateMachine.Enter<BootstrapState>();
    }

    public void Update(float deltaTime)
    {
        _stateMachine.Update(deltaTime);
    }

    private void RegisterServices()
    {
        _context.Register<ILoggerService>(new LoggerService());
        _context.Register<ISceneLoaderService>(new SceneLoaderService());
    }

    public void SetStreetBounds(float left, float right)
    {
        StreetLeftBoundX = left;
        StreetRightBoundX = right;
    }
}
