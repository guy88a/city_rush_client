using CityRush.Core;
using CityRush.Core.Services;
using CityRush.Core.States;

public class Game
{
    private readonly GameStateMachine _stateMachine;
    private readonly GameContext _context;

    public Game()
    {
        _context = new GameContext();
        RegisterServices();
        _stateMachine = new GameStateMachine(_context);
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
}
