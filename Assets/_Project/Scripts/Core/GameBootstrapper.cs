using UnityEngine;
using CityRush.Core.Prefabs;

namespace CityRush.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private CorePrefabsRegistry corePrefabs;

        private void Awake()
        {
            DontDestroyOnLoad(this);
            Main.Start(corePrefabs);
        }

        private void Update()
        {
            Main.Update(Time.deltaTime);
        }
    }
}
