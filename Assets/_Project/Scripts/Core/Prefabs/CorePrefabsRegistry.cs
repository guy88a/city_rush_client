using UnityEngine;
using CityRush.World.Background;
using CityRush.World.Street;
using CityRush.World.Interior;

namespace CityRush.Core.Prefabs
{
    [CreateAssetMenu(
        fileName = "CorePrefabsRegistry",
        menuName = "CityRush/Core/Core Prefabs Registry"
    )]
    public class CorePrefabsRegistry : ScriptableObject
    {
        [Header("Core World Prefabs")]
        [SerializeField] private Camera globalCameraPrefab;
        [SerializeField] private BackgroundRoot backgroundPrefab;
        [SerializeField] private StreetComponent streetPrefab;
        [SerializeField] private CorridorComponent corridorPrefab;
        [SerializeField] private ApartmentComponent apartmentPrefab;

        [Header("Core UI / Transitions")]
        [SerializeField] private GameObject screenFadeCanvasPrefab;

        [Header("Player")]
        [SerializeField] private GameObject playerPrefab;

        public Camera GlobalCameraPrefab => globalCameraPrefab;
        public BackgroundRoot BackgroundPrefab => backgroundPrefab;
        public StreetComponent StreetPrefab => streetPrefab;
        public CorridorComponent CorridorPrefab => corridorPrefab;
        public ApartmentComponent ApartmentPrefab => apartmentPrefab;

        public GameObject ScreenFadeCanvasPrefab => screenFadeCanvasPrefab;
        public GameObject PlayerPrefab => playerPrefab;
    }
}
