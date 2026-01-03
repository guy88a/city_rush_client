using CityRush.Units.Characters.Controllers;
using UnityEngine;

namespace CityRush.World.Interior
{
    [DisallowMultipleComponent]
    public sealed class CorridorExitTrigger : MonoBehaviour
    {
        [Range(0.01f, 0.99f)]
        [SerializeField] private float requiredOverlap01 = 0.5f;

        private BoxCollider2D _trigger;
        private bool _fired;

        // cached target (no tags, no repeated lookups)
        private PlayerPlatformerController _player;
        private Collider2D _playerCollider;

        private void Awake()
        {
            _trigger = GetComponent<BoxCollider2D>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_player != null) return;

            PlayerPlatformerController p = other.GetComponentInParent<PlayerPlatformerController>();
            if (p == null) return;

            _player = p;
            _playerCollider = other;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (_fired) return;
            if (_trigger == null) return;

            if (_player == null) return;
            if (other != _playerCollider) return;

            Bounds a = other.bounds;
            Bounds b = _trigger.bounds;

            float overlapW = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
            if (overlapW <= 0f) return;

            float pct = overlapW / Mathf.Max(0.0001f, a.size.x);

            if (pct >= requiredOverlap01)
            {
                _fired = true;
                Debug.Log($"[CorridorExitTrigger] overlap >= {requiredOverlap01:0.00} (now {pct:0.00})", this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_player == null) return;
            if (other != _playerCollider) return;

            _player = null;
            _playerCollider = null;
            _fired = false;
        }
    }
}
