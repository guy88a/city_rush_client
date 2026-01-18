using System.Collections;
using UnityEngine;

namespace CityRush.Units.Characters.Combat
{
    [DisallowMultipleComponent]
    public sealed class CharacterWeaponSet : MonoBehaviour
    {
        [Header("Weapons")]
        [SerializeField] private WeaponDefinition uziWeapon;
        [SerializeField] private WeaponDefinition shotgunWeapon;

        private WeaponShooter _shooter;

        private WeaponSlotRuntime _uzi;
        private WeaponSlotRuntime _shotgun;

        public WeaponDefinition UziWeapon => uziWeapon;
        public WeaponDefinition ShotgunWeapon => shotgunWeapon;

        public int UziMagazine => _uzi.Magazine;
        public int UziReserve => _uzi.Reserve;
        public bool IsUziReloading => _uzi.IsReloading;

        public int ShotgunMagazine => _shotgun.Magazine;
        public int ShotgunReserve => _shotgun.Reserve;
        public bool IsShotgunReloading => _shotgun.IsReloading;

        private void Awake()
        {
            _shooter = GetComponent<WeaponShooter>();

            _uzi = new WeaponSlotRuntime(this, WeaponType.Uzi);
            _shotgun = new WeaponSlotRuntime(this, WeaponType.Shotgun);

            _uzi.ResetFrom(uziWeapon);
            _shotgun.ResetFrom(shotgunWeapon);

            if (uziWeapon == null) Debug.LogWarning("[CharacterWeaponSet] Missing UziWeapon", this);
            if (shotgunWeapon == null) Debug.LogWarning("[CharacterWeaponSet] Missing ShotgunWeapon", this);
        }

        private WeaponDefinition GetWeapon(WeaponType type)
        {
            return type == WeaponType.Uzi ? uziWeapon : shotgunWeapon;
        }

        public bool TryFireUzi(Vector2 origin, Vector2 direction)
        {
            WeaponDefinition w = uziWeapon;
            if (w == null || w.Type != WeaponType.Uzi) return false;
            if (_shooter == null) return false;

            return _uzi.TryFire(
                w,
                () => _shooter.FireUzi(origin, direction, w)
            );
        }

        public bool TryFireShotgun(Vector2 origin, Vector2 direction)
        {
            WeaponDefinition w = shotgunWeapon;
            if (w == null || w.Type != WeaponType.Shotgun) return false;
            if (_shooter == null) return false;

            return _shotgun.TryFire(
                w,
                () => _shooter.FireShotgun(origin, direction, w)
            );
        }

        public bool IsUziAnimActive()
        {
            WeaponDefinition w = uziWeapon;
            if (w == null) return false;

            // Same rule you wanted: no anim while reloading or when mag is empty.
            if (_uzi.IsReloading) return false;
            if (w.MagazineSize > 0 && _uzi.Magazine <= 0) return false;

            return true;
        }

        private sealed class WeaponSlotRuntime
        {
            private readonly MonoBehaviour _host;
            private readonly WeaponType _type;

            private float _nextFireTime;

            public int Magazine { get; private set; }
            public int Reserve { get; private set; }
            public bool IsReloading { get; private set; }

            public WeaponSlotRuntime(MonoBehaviour host, WeaponType type)
            {
                _host = host;
                _type = type;
            }

            public void ResetFrom(WeaponDefinition w)
            {
                if (w == null || w.Type != _type)
                {
                    Magazine = 0;
                    Reserve = 0;
                    IsReloading = false;
                    _nextFireTime = 0f;
                    return;
                }

                Magazine = Mathf.Max(0, w.MagazineSize);
                Reserve = Mathf.Max(0, w.AmmoReserveMax);
                IsReloading = false;
                _nextFireTime = 0f;
            }

            public bool TryFire(WeaponDefinition w, System.Action doFire)
            {
                if (w == null || w.Type != _type) return false;
                if (IsReloading) return false;

                float now = Time.time;
                if (now < _nextFireTime) return false;

                if (w.MagazineSize > 0)
                {
                    if (Magazine <= 0)
                    {
                        if (w.ReloadTime > 0f && Reserve > 0)
                            _host.StartCoroutine(ReloadRoutine(w));
                        return false;
                    }

                    Magazine--;
                }

                _nextFireTime = now + Mathf.Max(0.01f, w.FireInterval);

                doFire?.Invoke();
                return true;
            }

            private IEnumerator ReloadRoutine(WeaponDefinition w)
            {
                if (IsReloading) yield break;
                if (w == null) yield break;
                if (Reserve <= 0) yield break;

                IsReloading = true;

                float t = Mathf.Max(0f, w.ReloadTime);
                if (t > 0f)
                    yield return new WaitForSeconds(t);

                int need = Mathf.Max(0, w.MagazineSize - Magazine);
                int load = Mathf.Min(need, Reserve);

                Magazine += load;
                Reserve -= load;

                Magazine = Mathf.Clamp(Magazine, 0, w.MagazineSize);
                Reserve = Mathf.Clamp(Reserve, 0, w.AmmoReserveMax);

                IsReloading = false;
            }
        }

        public bool TryFireUzi(Vector2 origin, Vector2 direction, CharacterUnit onlyTarget)
        {
            WeaponDefinition w = uziWeapon;
            if (w == null || w.Type != WeaponType.Uzi) return false;
            if (_shooter == null) return false;

            return _uzi.TryFire(w, () => _shooter.FireUzi(origin, direction, w, onlyTarget));
        }

        public bool TryFireShotgun(Vector2 origin, Vector2 direction, CharacterUnit onlyTarget)
        {
            WeaponDefinition w = shotgunWeapon;
            if (w == null || w.Type != WeaponType.Shotgun) return false;
            if (_shooter == null) return false;

            return _shotgun.TryFire(w, () => _shooter.FireShotgun(origin, direction, w, onlyTarget));
        }
    }
}
