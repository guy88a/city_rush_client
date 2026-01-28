using UnityEngine;

namespace CityRush.Units
{
    [DisallowMultipleComponent]
    public sealed class NpcIdentity : MonoBehaviour
    {
        [SerializeField] private int id;
        public int Id => id;
    }
}
