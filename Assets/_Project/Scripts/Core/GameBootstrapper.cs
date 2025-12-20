using UnityEngine;
using CityRush.World.Street;
using CityRush.World.Background;

namespace CityRush.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private BackgroundRoot backgroundPrefab;
        [SerializeField] private StreetComponent streetPrefab;

        private void Awake()
        {
            DontDestroyOnLoad(this);
            Main.Start(backgroundPrefab, streetPrefab);
        }

        private void Update()
        {
            Main.Update(Time.deltaTime); // Delegate to Main
        }
    }
}
