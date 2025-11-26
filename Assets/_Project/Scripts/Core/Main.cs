namespace CityRush.Core
{
    public static class Main
    {
        private static Game _game;

        public static void Start()
        {
            _game = new Game();
            _game.Start();
        }
    }
}
