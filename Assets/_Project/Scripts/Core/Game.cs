using CityRush.Core;
using CityRush.Core.Prefabs;
using CityRush.Core.Services;
using CityRush.Core.States;
using CityRush.Quests;
using UnityEngine;

public class Game
{
    private readonly GameStateMachine _stateMachine;
    private readonly GameContext _context;

    private readonly CorePrefabsRegistry _corePrefabs;
    private readonly QuestDB _questDB;

    public Camera GlobalCamera { get; private set; }
    public Transform CameraTransform => GlobalCamera.transform;

    public float StreetLeftBoundX { get; private set; }
    public float StreetRightBoundX { get; private set; }

    public Game(CorePrefabsRegistry corePrefabs, QuestDB questDB)
    {
        _corePrefabs = corePrefabs;
        _questDB = questDB;

        _context = new GameContext();
        RegisterServices();

        // Camera stays a core runtime system
        GlobalCamera = Object.Instantiate(_corePrefabs.GlobalCameraPrefab);
        Object.DontDestroyOnLoad(GlobalCamera.gameObject);

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

        // Register prefab registry as data
        _context.Set(_corePrefabs);
        _context.Set(_questDB);
    }

}
