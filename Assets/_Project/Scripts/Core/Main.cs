using CityRush.Core;
using CityRush.World.Street;

public static class Main
{
    private static Game _game;

    public static void Start(StreetComponent streetPrefab)
    {
        _game = new Game(streetPrefab);
        _game.Start();
    }

    public static void Update(float deltaTime)
    {
        _game?.Update(deltaTime);
    }
}
