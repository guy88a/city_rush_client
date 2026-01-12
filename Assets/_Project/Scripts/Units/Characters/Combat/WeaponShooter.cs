using UnityEngine;

namespace CityRush.Units.Characters.Combat
{
    [DisallowMultipleComponent]
    public sealed class WeaponShooter : MonoBehaviour
    {
        [Header("Uzi Projectile")]
        [SerializeField] private ProjectileLinear uziBulletPrefab;
        [SerializeField] private int uziPoolSize = 40;
        [SerializeField] private float uziBulletSpeed = 18f;
        [SerializeField] private float uziBulletLifetime = 1.2f;
        [SerializeField] private int uziBaseDamage = 4;

        private DamageResolver _damage;
        private Collider2D[] _ownerColliders;

        private ProjectilePool _uziPool;

        private void Awake()
        {
            _damage = GetComponent<DamageResolver>();
            _ownerColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);

            if (uziBulletPrefab != null)
                _uziPool = new ProjectilePool(uziBulletPrefab, uziPoolSize, transform);
        }

        public void FireUzi(Vector2 origin, Vector2 direction)
        {
            if (_uziPool == null) return;

            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.right;
            direction.Normalize();

            ProjectileLinear p = _uziPool.Spawn();
            if (p == null) return;

            p.transform.position = origin;
            p.transform.rotation = Quaternion.identity;

            p.Launch(
                direction,
                uziBulletSpeed,
                uziBulletLifetime,
                uziBaseDamage,
                _damage,
                _ownerColliders,
                _uziPool.Despawn
            );
        }

        private sealed class ProjectilePool
        {
            private readonly ProjectileLinear _prefab;
            private readonly Transform _parent;
            private readonly ProjectileLinear[] _items;
            private int _next;

            public ProjectilePool(ProjectileLinear prefab, int size, Transform parent)
            {
                _prefab = prefab;
                _parent = parent;

                int s = Mathf.Max(1, size);
                _items = new ProjectileLinear[s];

                for (int i = 0; i < s; i++)
                {
                    ProjectileLinear p = Object.Instantiate(_prefab, _parent);
                    p.gameObject.SetActive(false);
                    _items[i] = p;
                }

                _next = 0;
            }

            public ProjectileLinear Spawn()
            {
                // Simple ring buffer.
                for (int i = 0; i < _items.Length; i++)
                {
                    int idx = (_next + i) % _items.Length;
                    if (!_items[idx].gameObject.activeSelf)
                    {
                        _next = (idx + 1) % _items.Length;
                        _items[idx].gameObject.SetActive(true);
                        return _items[idx];
                    }
                }

                return null;
            }

            public void Despawn(ProjectileLinear p)
            {
                if (p == null) return;
                p.gameObject.SetActive(false);
            }
        }
    }
}