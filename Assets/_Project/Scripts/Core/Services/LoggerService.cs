using UnityEngine;

namespace CityRush.Core.Services
{
    public class LoggerService : ILoggerService
    {
        public void Success(string message)
        {
            Debug.Log(Format("Success", "green", message));
        }

        public void Error(string message)
        {
            Debug.LogError(Format("Error", "red", message));
        }

        public void Warning(string message)
        {
            Debug.LogWarning(Format("Warning", "orange", message));
        }

        public void Info(string message)
        {
            Debug.Log(Format("Info", "#00cfff", message)); // sky-blue
        }

        public void Event(string message)
        {
            Debug.Log(Format("Event", "#30f0c0", message)); // turquoise
        }

        private string Format(string prefix, string color, string message)
        {
            return $"<b><color={color}>{prefix}:</color></b> {message}";
        }
    }
}
