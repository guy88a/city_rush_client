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

        private GameObject _lastAttackerRoot;

        private void Awake()
        {
            if (maxHp < 1) maxHp = 1;
            currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        }
        private void OnEnable()
        {
            // When spawned from pool, always start full.
            currentHp = maxHp;
            ClearLastAttackerRoot();
        }

        private void OnDisable()
        {
            // When returned to pool, reset so we don't carry damage across respawns.
            currentHp = maxHp;
            ClearLastAttackerRoot();
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
            {
                OnDied?.Invoke();
                ClearLastAttackerRoot();
            }
        }

        public GameObject LastAttackerRoot => _lastAttackerRoot;

        public void SetLastAttackerRoot(GameObject attackerRoot)
        {
            _lastAttackerRoot = attackerRoot;
        }

        public void ClearLastAttackerRoot()
        {
            _lastAttackerRoot = null;
        }

        public void RefillToFull(bool raiseHealedEvent = true)
        {
            int before = currentHp;
            currentHp = maxHp;
            ClearLastAttackerRoot();

            int delta = currentHp - before;
            if (raiseHealedEvent && delta > 0)
                OnHealed?.Invoke(currentHp, delta);
        }

    }
}