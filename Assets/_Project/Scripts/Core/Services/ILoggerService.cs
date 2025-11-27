namespace CityRush.Core.Services
{
    public interface ILoggerService : IGameService
    {
        void Log(string message);
    }
}
