using CityRush.Core;
using CityRush.Core.Services;
using CityRush.Core.States;
using CityRush.World.Street;

public class Game
{
    private readonly GameStateMachine _stateMachine;
    private readonly GameContext _context;

    public StreetComponent StreetPrefab { get; }
    public float StreetLeftBoundX { get; private set; }
    public float StreetRightBoundX { get; private set; }

    public Game(StreetComponent streetPrefab)
    {
        StreetPrefab = streetPrefab;

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
