using CityRush.Units.Characters.Combat;
using System;
using UnityEngine;

namespace CityRush.Units.Characters
{
    [DisallowMultipleComponent]
    public sealed class CharacterCombatState : MonoBehaviour
    {
        [SerializeField] private bool isInCombat;

        public bool IsInCombat => isInCombat;

        public event Action OnCombatEntered;
        public event Action OnCombatExited;

        private Health _health;

        public CharacterUnit Target { get; private set; }
        public bool HasTarget => Target != null;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            isInCombat = false; // hard reset on spawn
            if (_health != null)
                _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            ExitCombat(); // reset on despawn / pool return

            if (_health != null)
                _health.OnDied -= HandleDied;
        }

        private void HandleDied()
        {
            ExitCombat();
        }

        public void EnterCombat()
        {
            if (isInCombat) return;
            Debug.Log("Entering Combat");
            isInCombat = true;
            OnCombatEntered?.Invoke();
        }

        public void ExitCombat()
        {
            if (!isInCombat) return;
            Debug.Log("Exiting Combat");
            isInCombat = false;
            ClearTarget();
            OnCombatExited?.Invoke();
        }

        public void SetInCombat(bool value)
        {
            Debug.Log("Set In Combat");
            if (value) EnterCombat();
            else ExitCombat();
        }

        public void SetTarget(CharacterUnit target)
        {
            Target = target;
        }

        public void ClearTarget()
        {
            Target = null;
        }
    }
}
