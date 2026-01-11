using System;
using UnityEngine;


namespace CityRush.Units.Characters.Combat
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private int maxHp = 100;
        [SerializeField] private int currentHp = 100;


        public int MaxHp => maxHp;
        public int CurrentHp => currentHp;
        public bool IsAlive => currentHp > 0;


        public event Action<int, int> OnDamaged; // (newHp, amount)
        public event Action<int, int> OnHealed; // (newHp, amount)
        public event Action OnDied;


        private void Awake()
        {
            if (maxHp < 1) maxHp = 1;
            currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        }


        public void SetMaxHp(int value, bool refill)
        {
            maxHp = Mathf.Max(1, value);


            if (refill)
                currentHp = maxHp;
            else
                currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        }


        public void Heal(int amount)
        {
            if (!IsAlive) return;
            if (amount <= 0) return;


            int before = currentHp;
            currentHp = Mathf.Min(maxHp, currentHp + amount);
            int delta = currentHp - before;
            if (delta > 0)
                OnHealed?.Invoke(currentHp, delta);
        }


        public void TakeDamage(int amount)
        {
            if (!IsAlive) return;
            if (amount <= 0) return;


            int before = currentHp;
            currentHp = Mathf.Max(0, currentHp - amount);
            int delta = before - currentHp;


            if (delta > 0)
                OnDamaged?.Invoke(currentHp, delta);


            if (currentHp == 0)
                OnDied?.Invoke();
        }
    }
}