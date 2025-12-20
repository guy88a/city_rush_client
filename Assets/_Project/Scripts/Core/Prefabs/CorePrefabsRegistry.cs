using UnityEngine;
using CityRush.World.Background;
using CityRush.World.Street;

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

        public Camera GlobalCameraPrefab => globalCameraPrefab;
        public BackgroundRoot BackgroundPrefab => backgroundPrefab;
        public StreetComponent StreetPrefab => streetPrefab;
    }
}
