using UnityEngine;

namespace CityRush.World.Interior
{
    public abstract class Door : MonoBehaviour
    {
        [SerializeField] private string doorId;

        public string DoorId => doorId;

        public abstract void Enter(GameObject player);
    }
}
