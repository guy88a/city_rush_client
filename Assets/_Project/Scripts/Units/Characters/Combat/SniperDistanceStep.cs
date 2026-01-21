using UnityEngine;

namespace CityRush.Units.Characters.Combat
{
    [DisallowMultipleComponent]
    public sealed class SniperDistanceStep : MonoBehaviour
    {
        [SerializeField, Min(0)] private int step = 0;

        public int Step => step;

        public void SetStep(int newStep)
        {
            step = Mathf.Max(0, newStep);
        }

        private void OnValidate()
        {
            step = Mathf.Max(0, step);
        }
    }
}
