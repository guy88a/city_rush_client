namespace CityRush.Core.Services
{
    public interface ISceneLoaderService : IGameService
    {
        void Load(string sceneName, System.Action onLoaded = null);
    }
}
