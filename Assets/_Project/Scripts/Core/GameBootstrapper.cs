using CityRush.Core.Prefabs;
using CityRush.Quests;
using UnityEngine;

namespace CityRush.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private CorePrefabsRegistry corePrefabs;
        [SerializeField] private QuestDB questDB;

        private void Awake()
        {
            DontDestroyOnLoad(this);
            Main.Start(corePrefabs, questDB);
        }

        private void Update()
        {
            Main.Update(Time.deltaTime);
        }
    }
}
