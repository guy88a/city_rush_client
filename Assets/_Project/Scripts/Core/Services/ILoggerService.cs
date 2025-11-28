namespace CityRush.Core.Services
{
    public interface ILoggerService : IGameService
    {
        void Success(string message);
        void Error(string message);
        void Warning(string message);
        void Info(string message);
        void Event(string message);
    }
}
