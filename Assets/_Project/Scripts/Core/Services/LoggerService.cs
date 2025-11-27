namespace CityRush.Core.Services
{
    public class LoggerService : ILoggerService
    {
        public void Log(string message)
        {
            UnityEngine.Debug.Log(message);
        }
    }
}
