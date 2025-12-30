using CityRush.Core;
using CityRush.Core.Prefabs;
using CityRush.Core.States;
using CityRush.World.Map;
using CityRush.World.Map.Runtime;

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
    }

    public void Exit()
    {
        _world?.Exit();

        _navigation = null;
        _world = null;

        _prefabs = null;
        _mapManager = null;
    }

    public void Update(float deltaTime)
    {
        _navigation?.Tick(deltaTime);
    }
}
