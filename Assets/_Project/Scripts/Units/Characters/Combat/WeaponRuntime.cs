using System.Collections;
using UnityEngine;

namespace CityRush.Units.Characters.Combat
{
    [DisallowMultipleComponent]
    public sealed class WeaponRuntime : MonoBehaviour
    {
        [Header("Equipped")]
        [SerializeField] private WeaponDefinition equippedWeapon;

        [Header("Runtime (read-only)")]
        [SerializeField] private int magazine;
        [SerializeField] private int ammoReserve;
        [SerializeField] private bool isReloading;

        private float _nextFireTime;

        private WeaponShooter _shooter;

        public WeaponDefinition EquippedWeapon => equippedWeapon;
        public int Magazine => magazine;
        public int AmmoReserve => ammoReserve;
        public bool IsReloading => isReloading;

        public event System.Action OnReloadStarted;
        public event System.Action OnReloadEnded;

        public bool CanFireNow => !isReloading && Time.time >= _nextFireTime && (equippedWeapon == null || equippedWeapon.MagazineSize <= 0 || magazine > 0);

        private void Awake()
        {
            _shooter = GetComponent<WeaponShooter>();
            ResetAmmoFromEquipped();
        }

        public void Equip(WeaponDefinition weapon, bool refillAmmo = true)
        {
            equippedWeapon = weapon;

            if (refillAmmo)
                ResetAmmoFromEquipped();
            else
                ClampAmmoToEquipped();
        }

        public void ResetAmmoFromEquipped()
        {
            if (equippedWeapon == null)
            {
                magazine = 0;
                ammoReserve = 0;
                isReloading = false;
                _nextFireTime = 0f;
                return;
            }

            magazine = Mathf.Max(0, equippedWeapon.MagazineSize);
            ammoReserve = Mathf.Max(0, equippedWeapon.AmmoReserveMax);
            isReloading = false;
            _nextFireTime = 0f;
        }

        private void ClampAmmoToEquipped()
        {
            if (equippedWeapon == null)
            {
                magazine = 0;
                ammoReserve = 0;
                isReloading = false;
                return;
            }

            magazine = Mathf.Clamp(magazine, 0, equippedWeapon.MagazineSize);
            ammoReserve = Mathf.Clamp(ammoReserve, 0, equippedWeapon.AmmoReserveMax);
        }

        public bool TryFireUzi(Vector2 origin, Vector2 direction)
        {
            if (equippedWeapon == null) return false;
            if (equippedWeapon.Type != WeaponType.Uzi) return false;

            if (_shooter == null) return false;

            if (isReloading) return false;

            float now = Time.time;
            if (now < _nextFireTime) return false;

            if (equippedWeapon.MagazineSize > 0)
            {
                if (magazine <= 0)
                {
                    // Auto-reload gate.
                    if (equippedWeapon.ReloadTime > 0f)
                        StartCoroutine(ReloadRoutine());
                    return false;
                }

                magazine--;
            }

            _nextFireTime = now + equippedWeapon.FireInterval;

            _shooter.FireUzi(origin, direction, equippedWeapon);
            return true;
        }

        public bool TryStartReload()
        {
            if (equippedWeapon == null) return false;
            if (isReloading) return false;

            if (equippedWeapon.MagazineSize <= 0) return false;
            if (magazine >= equippedWeapon.MagazineSize) return false;
            if (ammoReserve <= 0) return false;
            if (equippedWeapon.ReloadTime <= 0f) return false;

            StartCoroutine(ReloadRoutine());
            return true;
        }

        private IEnumerator ReloadRoutine()
        {
            if (isReloading) yield break;

            // If no reserve, cannot reload.
            if (ammoReserve <= 0) yield break;

            isReloading = true;
            OnReloadStarted?.Invoke();

            float t = Mathf.Max(0f, equippedWeapon != null ? equippedWeapon.ReloadTime : 0f);
            if (t > 0f)
                yield return new WaitForSeconds(t);

            if (equippedWeapon != null)
            {
                int need = Mathf.Max(0, equippedWeapon.MagazineSize - magazine);
                int load = Mathf.Min(need, ammoReserve);

                magazine += load;
                ammoReserve -= load;

                magazine = Mathf.Clamp(magazine, 0, equippedWeapon.MagazineSize);
                ammoReserve = Mathf.Clamp(ammoReserve, 0, equippedWeapon.AmmoReserveMax);
            }

            OnReloadEnded?.Invoke();
            isReloading = false;
        }
    }
}
