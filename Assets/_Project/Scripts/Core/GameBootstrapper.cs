using UnityEngine;
using CityRush.World.Street;
using CityRush.World.Background;

namespace CityRush.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private BackgroundRoot backgroundPrefab;
        [SerializeField] private StreetComponent streetPrefab;
        [SerializeField] private Camera globalCameraPrefab; // NEW

        private void Awake()
        {
            DontDestroyOnLoad(this);
            Main.Start(globalCameraPrefab, backgroundPrefab, streetPrefab);
        }

        private void Update()
        {
            Main.Update(Time.deltaTime);
        }
    }
}
