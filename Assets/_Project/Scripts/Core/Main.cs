using CityRush.Core.Prefabs;
using CityRush.Quests;

public static class Main
{
    private static Game _game;

    public static void Start(CorePrefabsRegistry corePrefabs, QuestDB questDB)
    {
        _game = new Game(corePrefabs, questDB);
        _game.Start();
    }

    public static void Update(float deltaTime)
    {
        _game?.Update(deltaTime);
    }
}
