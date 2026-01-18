using UnityEngine;

namespace CityRush.Units.Characters.Combat
{
    [DisallowMultipleComponent]
    public sealed class WeaponShooter : MonoBehaviour
    {
        [SerializeField] private bool debugDrawShotgunBox = true;
        [SerializeField] private Color debugShotgunColor = new Color(1f, 0.6f, 0.1f, 0.9f);
        private bool _shotgunDebugHasBox;
        private Vector2 _shotgunDebugCenter;
        private Vector2 _shotgunDebugSize;

        private DamageResolver _damage;
        private Collider2D[] _ownerColliders;

        private ProjectilePool _uziPool;
        private ProjectileLinear _uziPrefabCached;

        private readonly Collider2D[] _shotgunHits = new Collider2D[32];

        private void Awake()
        {
            _damage = GetComponent<DamageResolver>();
            _ownerColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        }

        public void FireUzi(Vector2 origin, Vector2 direction, WeaponDefinition weapon, CharacterUnit onlyTarget = null)
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
                _uziPool.Despawn,
                onlyTarget
            );
        }

        public void FireShotgun(Vector2 origin, Vector2 direction, WeaponDefinition weapon, CharacterUnit onlyTarget = null)
        {
            if (weapon == null) return;
            if (weapon.Type != WeaponType.Shotgun) return;

            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.right;
            direction.Normalize();

            // Compute overlap box center in front of the shooter.
            Vector2 offset = weapon.ShotgunBoxOffset;
            Vector2 center = origin + new Vector2(offset.x * direction.x, offset.y);

            _shotgunDebugHasBox = true;
            _shotgunDebugCenter = center;
            _shotgunDebugSize = weapon.ShotgunBoxSize;

            var filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = weapon.ShotgunHitMask,
                useTriggers = true
            };

            int hitCount = Physics2D.OverlapBox(
                center,
                weapon.ShotgunBoxSize,
                0f,
                filter,
                _shotgunHits
            );

            Debug.Log($"[Shotgun] hits={hitCount} center={center} size={weapon.ShotgunBoxSize}");
            for (int i = 0; i < hitCount; i++)
            {
                var h = _shotgunHits[i];
                if (h != null)
                    Debug.Log($"[Shotgun] hit: {h.name} layer={LayerMask.LayerToName(h.gameObject.layer)} isTrigger={h.isTrigger}");
            }

            if (hitCount <= 0) return;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _shotgunHits[i];
                if (hit == null) continue;

                if (onlyTarget != null)
                {
                    var hitUnit = hit.GetComponentInParent<CharacterUnit>();
                    if (hitUnit != onlyTarget)
                        continue;
                }

                // Clear slot for next call (avoids stale refs).
                _shotgunHits[i] = null;

                // IMPORTANT: ignore the shooter (even if hit mask includes Player).
                if (IsOwnerCollider(hit)) continue;

                // Apply damage (supports multi-hit).
                _damage.TryApplyDamage(hit, weapon.BaseDamage);

                // Knockback (optional, only if target has dynamic RB).
                float impulse = weapon.ShotgunKnockbackImpulse;
                if (impulse > 0f)
                {
                    var phys = hit.GetComponentInParent<PhysicsObject>();
                    if (phys != null)
                        phys.AddImpulse(direction * impulse);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugDrawShotgunBox) return;
            if (!_shotgunDebugHasBox) return;

            Gizmos.color = debugShotgunColor;

            Vector3 c = new Vector3(_shotgunDebugCenter.x, _shotgunDebugCenter.y, 0f);
            Vector3 s = new Vector3(_shotgunDebugSize.x, _shotgunDebugSize.y, 0f);

            Gizmos.DrawWireCube(c, s);
        }


        private bool IsOwnerCollider(Collider2D c)
        {
            if (c == null) return true;

            if (_ownerColliders == null) return false;

            for (int i = 0; i < _ownerColliders.Length; i++)
            {
                if (_ownerColliders[i] == c)
                    return true;
            }

            return false;
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
