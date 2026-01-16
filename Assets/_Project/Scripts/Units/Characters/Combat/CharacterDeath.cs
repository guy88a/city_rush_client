using UnityEngine;
using CityRush.Units.Characters.Controllers;

namespace CityRush.Units.Characters.Combat
{
    [DisallowMultipleComponent]
    public sealed class CharacterDeath : MonoBehaviour
    {
        [SerializeField] private string isAliveParam = "isAlive";

        private Health _health;
        private NPCController _npcController;
        private Rigidbody2D _rb;
        private Animator _animator;

        private bool _handledDeath;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _npcController = GetComponent<NPCController>();
            _rb = GetComponent<Rigidbody2D>();

            Transform graphic = transform.Find("Graphic");
            if (graphic != null)
                _animator = graphic.GetComponent<Animator>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
        }

        private void OnEnable()
        {
            if (_health != null)
                _health.OnDied += HandleDied;

            SyncAnimatorAlive();
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.OnDied -= HandleDied;
        }

        private void SyncAnimatorAlive()
        {
            if (_animator == null || _health == null) return;
            _animator.SetBool(isAliveParam, _health.IsAlive);
        }

        private void HandleDied()
        {
            if (_handledDeath) return;
            _handledDeath = true;

            if (_animator != null)
                _animator.SetBool(isAliveParam, false);

            // Freeze movement/actions (keep RB + collisions enabled)
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            if (_npcController != null)
                _npcController.enabled = false;
        }
    }
}
