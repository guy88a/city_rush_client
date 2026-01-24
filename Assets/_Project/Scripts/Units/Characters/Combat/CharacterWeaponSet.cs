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
        [SerializeField] private WeaponDefinition sniperWeapon;

        private WeaponShooter _shooter;

        private WeaponSlotRuntime _uzi;
        private WeaponSlotRuntime _shotgun;
        private WeaponSlotRuntime _sniper;

        private bool _initialized;

        public WeaponDefinition UziWeapon => uziWeapon;
        public WeaponDefinition ShotgunWeapon => shotgunWeapon;
        public WeaponDefinition SniperWeapon => sniperWeapon;

        public int UziMagazine => _uzi.Magazine;
        public int UziReserve => _uzi.Reserve;
        public bool IsUziReloading => _uzi.IsReloading;

        public int ShotgunMagazine => _shotgun.Magazine;
        public int ShotgunReserve => _shotgun.Reserve;
        public bool IsShotgunReloading => _shotgun.IsReloading;

        public int SniperMagazine => _sniper.Magazine;
        public int SniperReserve => _sniper.Reserve;
        public bool IsSniperReloading => _sniper.IsReloading;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            _initialized = true;

            _shooter = GetComponent<WeaponShooter>();

            _uzi = new WeaponSlotRuntime(this, WeaponType.Uzi);
            _shotgun = new WeaponSlotRuntime(this, WeaponType.Shotgun);
            _sniper = new WeaponSlotRuntime(this, WeaponType.Sniper);

            // Initial spawn defaults (magazine full, reserve max)
            _uzi.InitializeFromDefaults(uziWeapon);
            _shotgun.InitializeFromDefaults(shotgunWeapon);
            _sniper.InitializeFromDefaults(sniperWeapon);

            if (uziWeapon == null) Debug.LogWarning("[CharacterWeaponSet] Missing UziWeapon", this);
            if (shotgunWeapon == null) Debug.LogWarning("[CharacterWeaponSet] Missing ShotgunWeapon", this);
            if (sniperWeapon == null) Debug.LogWarning("[CharacterWeaponSet] Missing SniperWeapon", this);
        }

        // Equip rule (locked): reserve persists; equip cancels reload and tops off magazine from reserve.
        // Extra rule (for now): equipping into an empty slot initializes ammo to defaults.
        public bool TryEquipWeapon(WeaponDefinition weapon)
        {
            EnsureInitialized();

            if (weapon == null)
                return false;

            switch (weapon.Type)
            {
                case WeaponType.Uzi:
                    {
                        bool wasEmpty = (uziWeapon == null);
                        uziWeapon = weapon;
                        if (wasEmpty) _uzi.InitializeFromDefaults(weapon);
                        else _uzi.EquipFrom(weapon);
                        return true;
                    }

                case WeaponType.Shotgun:
                    {
                        bool wasEmpty = (shotgunWeapon == null);
                        shotgunWeapon = weapon;
                        if (wasEmpty) _shotgun.InitializeFromDefaults(weapon);
                        else _shotgun.EquipFrom(weapon);
                        return true;
                    }

                case WeaponType.Sniper:
                    {
                        bool wasEmpty = (sniperWeapon == null);
                        sniperWeapon = weapon;
                        if (wasEmpty) _sniper.InitializeFromDefaults(weapon);
                        else _sniper.EquipFrom(weapon);
                        return true;
                    }

                default:
                    return false;
            }
        }

        // Convenience for ItemsDB weaponDefinitionId (Resources path).
        public bool TryEquipWeaponByDefinitionId(string weaponDefinitionId)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(weaponDefinitionId))
                return false;

            WeaponDefinition weapon = Resources.Load<WeaponDefinition>(weaponDefinitionId);
            if (weapon == null)
            {
                Debug.LogWarning($"[CharacterWeaponSet] Missing WeaponDefinition at Resources path '{weaponDefinitionId}'", this);
                return false;
            }

            return TryEquipWeapon(weapon);
        }

        public bool TryFireUzi(Vector2 origin, Vector2 direction)
        {
            EnsureInitialized();

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
            EnsureInitialized();

            WeaponDefinition w = shotgunWeapon;
            if (w == null || w.Type != WeaponType.Shotgun) return false;
            if (_shooter == null) return false;

            return _shotgun.TryFire(
                w,
                () => _shooter.FireShotgun(origin, direction, w)
            );
        }

        public bool TryFireSniperADS(Camera cam)
        {
            EnsureInitialized();

            WeaponDefinition w = sniperWeapon;
            if (w == null || w.Type != WeaponType.Sniper) return false;
            if (_shooter == null) return false;

            return _sniper.TryFire(
                w,
                () => _shooter.FireSniperADS(cam, w)
            );
        }

        public bool IsUziAnimActive()
        {
            EnsureInitialized();

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
            private int _reloadToken;

            public int Magazine { get; private set; }
            public int Reserve { get; private set; }
            public bool IsReloading { get; private set; }

            public WeaponSlotRuntime(MonoBehaviour host, WeaponType type)
            {
                _host = host;
                _type = type;
            }

            public void InitializeFromDefaults(WeaponDefinition w)
            {
                _reloadToken++;
                IsReloading = false;
                _nextFireTime = 0f;

                if (w == null || w.Type != _type)
                {
                    Magazine = 0;
                    Reserve = 0;
                    return;
                }

                Magazine = Mathf.Max(0, w.MagazineSize);
                Reserve = Mathf.Max(0, w.AmmoReserveMax);
            }

            // Reserve persists across equip.
            // Equip cancels reload and tops off magazine from reserve.
            public void EquipFrom(WeaponDefinition w)
            {
                _reloadToken++;
                IsReloading = false;
                _nextFireTime = 0f;

                if (w == null || w.Type != _type)
                {
                    Magazine = 0;
                    return;
                }

                // Clamp existing ammo to new weapon bounds.
                Reserve = Mathf.Clamp(Reserve, 0, w.AmmoReserveMax);
                Magazine = Mathf.Clamp(Magazine, 0, w.MagazineSize);

                // Top-off magazine from reserve.
                if (w.MagazineSize > 0)
                {
                    int need = Mathf.Max(0, w.MagazineSize - Magazine);
                    int load = Mathf.Min(need, Reserve);

                    Magazine += load;
                    Reserve -= load;

                    Magazine = Mathf.Clamp(Magazine, 0, w.MagazineSize);
                    Reserve = Mathf.Clamp(Reserve, 0, w.AmmoReserveMax);
                }
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
                int token = _reloadToken;

                float t = Mathf.Max(0f, w.ReloadTime);
                if (t > 0f)
                    yield return new WaitForSeconds(t);

                // Cancelled by EquipFrom / InitializeFromDefaults
                if (token != _reloadToken)
                {
                    IsReloading = false;
                    yield break;
                }

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
            EnsureInitialized();

            WeaponDefinition w = uziWeapon;
            if (w == null || w.Type != WeaponType.Uzi) return false;
            if (_shooter == null) return false;

            return _uzi.TryFire(w, () => _shooter.FireUzi(origin, direction, w, onlyTarget));
        }

        public bool TryFireShotgun(Vector2 origin, Vector2 direction, CharacterUnit onlyTarget)
        {
            EnsureInitialized();

            WeaponDefinition w = shotgunWeapon;
            if (w == null || w.Type != WeaponType.Shotgun) return false;
            if (_shooter == null) return false;

            return _shotgun.TryFire(w, () => _shooter.FireShotgun(origin, direction, w, onlyTarget));
        }
    }
}
