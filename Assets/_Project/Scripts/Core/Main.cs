using CityRush.Core;
using CityRush.World.Background;
using CityRush.World.Street;
using UnityEngine;

public static class Main
{
    private static Game _game;

    public static void Start(
        Camera globalCameraPrefab,
        BackgroundRoot backgroundPrefab,
        StreetComponent streetPrefab)
    {
        _game = new Game(globalCameraPrefab, backgroundPrefab, streetPrefab);
        _game.Start();
    }

    public static void Update(float deltaTime)
    {
        _game?.Update(deltaTime);
    }
}
