using System;
using UnityEngine.SceneManagement;

namespace CityRush.Core.Services
{
    public class SceneLoaderService : ISceneLoaderService
    {
        public void Load(string sceneName, System.Action onLoaded = null)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (onLoaded != null)
                operation.completed += _ => onLoaded.Invoke();
        }
    }
}
