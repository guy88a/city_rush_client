using CityRush.World.Buildings.Registry.Interior;
using UnityEngine;

namespace CityRush.World.Interior
{
    public sealed class CorridorComponent : MonoBehaviour
    {
        [Header("Registries")]
        [SerializeField] private InteriorWallRegistry wallRegistry;
        [SerializeField] private InteriorFloorRegistry floorRegistry;
        [SerializeField] private InteriorSkirtingRegistry skirtingRegistry;
        [SerializeField] private InteriorDoorRegistry doorRegistry;
        [SerializeField] private InteriorDoorFrameRegistry doorFrameRegistry;

        [Header("Data")]
        [TextArea(5, 20)]
        [SerializeField] private string corridorData;

        public InteriorWallRegistry WallRegistry => wallRegistry;
        public InteriorFloorRegistry FloorRegistry => floorRegistry;
        public InteriorSkirtingRegistry SkirtingRegistry => skirtingRegistry;
        public InteriorDoorRegistry DoorRegistry => doorRegistry;
        public InteriorDoorFrameRegistry DoorFrameRegistry => doorFrameRegistry;

        public string CorridorData
        {
            get => corridorData;
            private set => corridorData = value;
        }

        public void SetJson(string json)
        {
            CorridorData = json;
        }
    }
}
