using CityRush.Core.Services;
using System;
using System.Collections.Generic;

namespace CityRush.Core
{
    public class GameContext
    {
        private readonly Dictionary<Type, object> _services = new();
        private readonly Dictionary<Type, object> _data = new();

        // SERVICES (unchanged)
        public void Register<T>(T service) where T : class, IGameService
        {
            _services[typeof(T)] = service;
        }

        public T Get<T>() where T : class, IGameService
        {
            return _services[typeof(T)] as T;
        }

        // DATA (new)
        public void Set<T>(T value)
        {
            _data[typeof(T)] = value;
        }

        public T GetData<T>()
        {
            return (T)_data[typeof(T)];
        }
    }
}
