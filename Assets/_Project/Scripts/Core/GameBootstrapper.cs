using UnityEngine;
using CityRush.World.Street;

namespace CityRush.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private StreetComponent streetPrefab;

        private void Awake()
        {
            DontDestroyOnLoad(this);
            Main.Start(streetPrefab);
        }

        private void Update()
        {
            Main.Update(Time.deltaTime); // Delegate to Main
        }
    }
}
