using CityRush.Core.Services;
using System;
using System.Collections.Generic;

namespace CityRush.Core
{
    public class GameContext
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Register<T>(T service) where T : class, IGameService
        {
            _services[typeof(T)] = service;
        }

        public T Get<T>() where T : class, IGameService
        {
            return _services[typeof(T)] as T;
        }
    }
}
