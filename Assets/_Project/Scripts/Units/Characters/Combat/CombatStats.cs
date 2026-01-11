using UnityEngine;


namespace CityRush.Units.Characters.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatStats : MonoBehaviour
    {
        [Header("Offense")]
        [SerializeField] private int power = 0;


        [Header("Defense")]
        [SerializeField] private int armor = 0;


        public int Power
        {
            get => power;
            set => power = Mathf.Max(0, value);
        }


        public int Armor
        {
            get => armor;
            set => armor = Mathf.Max(0, value);
        }


        private void Awake()
        {
            power = Mathf.Max(0, power);
            armor = Mathf.Max(0, armor);
        }
    }
}