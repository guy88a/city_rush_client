using UnityEngine;

namespace CityRush.Units.Characters.Combat
{
    [DisallowMultipleComponent]
    public sealed class WeaponShooter : MonoBehaviour
    {
        private DamageResolver _damage;
        private Collider2D[] _ownerColliders;

        private ProjectilePool _uziPool;
        private ProjectileLinear _uziPrefabCached;

        private void Awake()
        {
            _damage = GetComponent<DamageResolver>();
            _ownerColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        }

        public void FireUzi(Vector2 origin, Vector2 direction, WeaponDefinition weapon)
        {
            if (weapon == null) return;
            if (weapon.Type != WeaponType.Uzi) return;

            ProjectileLinear prefab = weapon.ProjectilePrefab;
            if (prefab == null) return;

            EnsureUziPool(prefab, weapon.ProjectilePoolSize);

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
                weapon.ProjectileSpeed,
                weapon.ProjectileLifetime,
                weapon.BaseDamage,
                _damage,
                _ownerColliders,
                _uziPool.Despawn
            );
        }

        private void EnsureUziPool(ProjectileLinear prefab, int poolSize)
        {
            // Rebuild pool if weapon prefab changed (future-proof for weapon swaps).
            if (_uziPool != null && _uziPrefabCached == prefab)
                return;

            _uziPrefabCached = prefab;
            _uziPool = new ProjectilePool(prefab, poolSize, transform);
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
