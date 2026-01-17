using UnityEngine;

namespace CityRush.Units.Characters.Combat
{
    [DisallowMultipleComponent]
    public sealed class ProjectileLinear : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private Collider2D _col;
        private SpriteRenderer _sr;

        private Vector2 _velocity;
        private float _life;
        private float _lifeMax;

        private int _baseDamage;
        private DamageResolver _ownerDamage;
        private Collider2D[] _ownerColliders;
        [SerializeField] private LayerMask hitMask = ~0;

        private System.Action<ProjectileLinear> _returnToPool;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _sr = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            _life = 0f;
        }

        public void Launch(
            Vector2 direction,
            float speed,
            float lifetime,
            int baseDamage,
            DamageResolver ownerDamage,
            Collider2D[] ownerColliders,
            System.Action<ProjectileLinear> returnToPool
        )
        {
            _velocity = direction.normalized * Mathf.Max(0f, speed);
            if (_sr != null)
                _sr.flipX = direction.x < 0f;
            _lifeMax = Mathf.Max(0.01f, lifetime);

            _baseDamage = Mathf.Max(0, baseDamage);
            _ownerDamage = ownerDamage;
            _ownerColliders = ownerColliders;
            _returnToPool = returnToPool;

            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }
        }

        private void FixedUpdate()
        {
            _life += Time.fixedDeltaTime;
            if (_life >= _lifeMax)
            {
                Despawn();
                return;
            }

            if (_rb != null)
            {
                Vector2 p = _rb.position;
                _rb.MovePosition(p + _velocity * Time.fixedDeltaTime);
            }
            else
            {
                transform.position += (Vector3)(_velocity * Time.fixedDeltaTime);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & hitMask) == 0)
                return;

            if (other == null) return;
            if (_col != null && other == _col) return;

            if (_ownerColliders != null)
            {
                for (int i = 0; i < _ownerColliders.Length; i++)
                {
                    if (other == _ownerColliders[i])
                        return;
                }
            }

            Destroyable destroyable = other.GetComponentInParent<Destroyable>();
            if (destroyable != null)
            {
                if (destroyable.TryHit(_baseDamage))
                {
                    Despawn();
                    return;
                }

                return;
            }

            // Ignore dead targets (keep their RB/colliders enabled; projectile should not despawn)
            Health health = other.GetComponentInParent<Health>();
            if (health != null && !health.IsAlive)
                return;

            // Damage is applied by the attacker via DamageResolver.
            if (_ownerDamage != null)
            {
                Health targetHealth = other.GetComponentInParent<Health>();
                if (targetHealth != null)
                    _ownerDamage.TryApplyDamage(targetHealth.gameObject, _baseDamage);
                else
                    _ownerDamage.TryApplyDamage(other, _baseDamage);
                Despawn();
            }

        }

        private void Despawn()
        {
            if (_returnToPool != null)
                _returnToPool(this);
            else
                gameObject.SetActive(false);
        }
    }
}