using UnityEngine;

namespace CityRush.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(this);
            Main.Start();
        }
    }
}
