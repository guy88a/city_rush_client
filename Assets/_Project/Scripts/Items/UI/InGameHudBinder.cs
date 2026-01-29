using CityRush.Units.Characters.Combat;
using UnityEngine;

namespace CityRush.UI
{
    public enum WeaponHudMode
    {
        Platformer = 0,
        Sniper = 1
    }

    [DisallowMultipleComponent]
    public sealed class InGameHudBinder : MonoBehaviour
    {
        [Header("Runtime (set by code)")]
        [SerializeField] private Transform playerRoot;
        [SerializeField] private CharacterWeaponSet weaponSet;
        [SerializeField] private Health health;

        [Header("Weapons HUD (optional roots)")]
        [SerializeField] private Transform weaponsHudRoot;
        [SerializeField] private GameObject platformer_0;
        [SerializeField] private GameObject platformer_1;
        [SerializeField] private GameObject platformer_2;
        [SerializeField] private GameObject platformer_3;
        [SerializeField] private GameObject sniper_0;
        [SerializeField] private GameObject sniper_1;

        [Header("Health HUD (optional roots)")]
        [SerializeField] private Transform hearthRoot;
        [SerializeField] private GameObject hearthFull;
        [SerializeField] private GameObject hearthHalf;
        [SerializeField] private GameObject hearthEmpty;

        private WeaponHudMode _weaponMode = WeaponHudMode.Platformer;

        private int _lastWeaponKey = int.MinValue;
        private int _lastHealthState = int.MinValue;

        private Health _boundHealth;

        private void Awake()
        {
            EnsureHudRefs();
        }

        private void OnEnable()
        {
            EnsureHudRefs();
            BindToHealth(health);
            RefreshAll(force: true);
        }

        private void OnDisable()
        {
            BindToHealth(null);
        }

        private void Update()
        {
            RefreshWeapons(force: false);
        }

        // Called from GameLoopState (Step 2).
        public void BindPlayer(Transform newPlayerRoot)
        {
            playerRoot = newPlayerRoot;

            weaponSet = (playerRoot != null) ? playerRoot.GetComponent<CharacterWeaponSet>() : null;

            var newHealth = (playerRoot != null) ? playerRoot.GetComponent<Health>() : null;
            BindToHealth(newHealth);
            health = newHealth;

            RefreshAll(force: true);
        }

        // Called from GameLoopState (Step 2).
        public void SetWeaponMode(WeaponHudMode mode)
        {
            if (_weaponMode == mode)
                return;

            _weaponMode = mode;
            _lastWeaponKey = int.MinValue;
            RefreshWeapons(force: true);
        }

        private void BindToHealth(Health newHealth)
        {
            if (_boundHealth == newHealth)
                return;

            if (_boundHealth != null)
            {
                _boundHealth.OnDamaged -= OnHealthChanged;
                _boundHealth.OnHealed -= OnHealthChanged;
                _boundHealth.OnDied -= OnDied;
            }

            _boundHealth = newHealth;

            if (isActiveAndEnabled && _boundHealth != null)
            {
                _boundHealth.OnDamaged += OnHealthChanged;
                _boundHealth.OnHealed += OnHealthChanged;
                _boundHealth.OnDied += OnDied;
            }
        }

        private void OnHealthChanged(int newHp, int amount)
        {
            RefreshHealth(force: false);
        }

        private void OnDied()
        {
            RefreshHealth(force: true);
        }

        private void RefreshAll(bool force)
        {
            RefreshWeapons(force);
            RefreshHealth(force);
        }

        private void RefreshWeapons(bool force)
        {
            if (weaponSet == null)
                return;

            bool hasUzi = weaponSet.UziWeapon != null;
            bool hasShotgun = weaponSet.ShotgunWeapon != null;
            bool hasSniper = weaponSet.SniperWeapon != null;

            int mask = 0;

            if (_weaponMode == WeaponHudMode.Platformer)
            {
                if (hasUzi) mask |= 1;
                if (hasShotgun) mask |= 2;
            }
            else
            {
                if (hasSniper) mask |= 1;
            }

            int key = (((int)_weaponMode) << 8) | mask;

            if (!force && key == _lastWeaponKey)
                return;

            _lastWeaponKey = key;

            SetWeaponsModeRootsActive(_weaponMode);

            if (_weaponMode == WeaponHudMode.Platformer)
            {
                SetActiveSafe(platformer_0, mask == 0);
                SetActiveSafe(platformer_1, mask == 1);
                SetActiveSafe(platformer_2, mask == 2);
                SetActiveSafe(platformer_3, mask == 3);

                SetActiveSafe(sniper_0, false);
                SetActiveSafe(sniper_1, false);
            }
            else
            {
                SetActiveSafe(platformer_0, false);
                SetActiveSafe(platformer_1, false);
                SetActiveSafe(platformer_2, false);
                SetActiveSafe(platformer_3, false);

                SetActiveSafe(sniper_0, !hasSniper);
                SetActiveSafe(sniper_1, hasSniper);
            }
        }

        private void RefreshHealth(bool force)
        {
            if (_boundHealth == null)
                return;

            int hp = _boundHealth.CurrentHp;
            int max = Mathf.Max(1, _boundHealth.MaxHp);

            int state;
            if (hp <= 0) state = 0;                 // Empty
            else if (hp * 2 > max) state = 2;       // Full (>50%)
            else state = 1;                         // Half (1..50%)

            if (!force && state == _lastHealthState)
                return;

            _lastHealthState = state;

            SetActiveSafe(hearthFull, state == 2);
            SetActiveSafe(hearthHalf, state == 1);
            SetActiveSafe(hearthEmpty, state == 0);
        }

        private void SetWeaponsModeRootsActive(WeaponHudMode mode)
        {
            // Nothing to do besides toggling variants, but keeping this hook for cleanliness.
            // We keep "never both" by disabling the opposite group variants in RefreshWeapons().
        }

        private void EnsureHudRefs()
        {
            if (weaponsHudRoot == null)
                weaponsHudRoot = transform.Find("WeaponsHUD");

            if (hearthRoot == null)
                hearthRoot = transform.Find("Hearth");

            if (weaponsHudRoot != null)
            {
                if (platformer_0 == null) platformer_0 = FindGo(weaponsHudRoot, "Platformer_0");
                if (platformer_1 == null) platformer_1 = FindGo(weaponsHudRoot, "Platformer_1");
                if (platformer_2 == null) platformer_2 = FindGo(weaponsHudRoot, "Platformer_2");
                if (platformer_3 == null) platformer_3 = FindGo(weaponsHudRoot, "Platformer_3");
                if (sniper_0 == null) sniper_0 = FindGo(weaponsHudRoot, "Sniper_0");
                if (sniper_1 == null) sniper_1 = FindGo(weaponsHudRoot, "Sniper_1");
            }

            if (hearthRoot != null)
            {
                if (hearthFull == null) hearthFull = FindGo(hearthRoot, "Full");
                if (hearthHalf == null) hearthHalf = FindGo(hearthRoot, "Half");
                if (hearthEmpty == null) hearthEmpty = FindGo(hearthRoot, "Empty");
            }
        }

        private static GameObject FindGo(Transform root, string childName)
        {
            if (root == null) return null;
            var t = root.Find(childName);
            return (t != null) ? t.gameObject : null;
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go == null) return;
            if (go.activeSelf == active) return;
            go.SetActive(active);
        }
    }
}
