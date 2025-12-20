using CityRush.Core.Prefabs;

public static class Main
{
    private static Game _game;

    public static void Start(CorePrefabsRegistry corePrefabs)
    {
        _game = new Game(corePrefabs);
        _game.Start();
    }

    public static void Update(float deltaTime)
    {
        _game?.Update(deltaTime);
    }
}
