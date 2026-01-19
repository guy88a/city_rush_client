using UnityEngine;

namespace CityRush.Units.Characters.Combat
{
    public enum WeaponType
    {
        Uzi,
        Shotgun,
        Melee,
        Sniper,
        Thrown
    }

    [CreateAssetMenu(menuName = "CityRush/Combat/Weapon Definition", fileName = "Weapon_")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Type")]
        [SerializeField] private WeaponType type = WeaponType.Uzi;

        [Header("Core")]
        [SerializeField] private int baseDamage = 1;

        // For Uzi: interval between bullets (fire rate).
        // For Shotgun/Sniper/Melee/Thrown: this is "time between shots/hits" (your reload speed as fire rate).
        [SerializeField] private float fireInterval = 0.1f;

        [Header("Ammo")]
        // 0 = no magazine (melee). For thrown, you can still use magazine=0 and rely on ammo reserve only.
        [SerializeField] private int magazineSize = 0;

        // 0 = infinite (melee). For thrown: this is your count.
        [SerializeField] private int ammoReserveMax = 0;

        // Used when magazineSize > 0.
        [SerializeField] private float reloadTime = 1.0f;

        [Header("Uzi Projectile")]
        [SerializeField] private ProjectileLinear projectilePrefab;
        [SerializeField] private int projectilePoolSize = 40;
        [SerializeField] private float projectileSpeed = 18f;
        [SerializeField] private float projectileLifetime = 1.2f;

        [Header("Shotgun Hit (OverlapBox)")]
        [SerializeField] private Vector2 shotgunBoxSize = new Vector2(1.6f, 0.8f);
        [SerializeField] private Vector2 shotgunBoxOffset = new Vector2(0.9f, 0.15f);
        [SerializeField] private float shotgunKnockbackImpulse = 6f;
        [SerializeField] private LayerMask shotgunHitMask;

        public WeaponType Type => type;

        public int BaseDamage => baseDamage;
        public float FireInterval => fireInterval;

        public int MagazineSize => magazineSize;
        public int AmmoReserveMax => ammoReserveMax;
        public float ReloadTime => reloadTime;

        public ProjectileLinear ProjectilePrefab => projectilePrefab;
        public int ProjectilePoolSize => projectilePoolSize;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;

        public Vector2 ShotgunBoxSize => shotgunBoxSize;
        public Vector2 ShotgunBoxOffset => shotgunBoxOffset;
        public float ShotgunKnockbackImpulse => shotgunKnockbackImpulse;
        public LayerMask ShotgunHitMask => shotgunHitMask;

        private void OnValidate()
        {
            baseDamage = Mathf.Max(0, baseDamage);
            fireInterval = Mathf.Max(0.01f, fireInterval);

            magazineSize = Mathf.Max(0, magazineSize);
            ammoReserveMax = Mathf.Max(0, ammoReserveMax);
            reloadTime = Mathf.Max(0f, reloadTime);

            projectilePoolSize = Mathf.Max(1, projectilePoolSize);
            projectileSpeed = Mathf.Max(0f, projectileSpeed);
            projectileLifetime = Mathf.Max(0.01f, projectileLifetime);

            shotgunBoxSize.x = Mathf.Max(0.01f, shotgunBoxSize.x);
            shotgunBoxSize.y = Mathf.Max(0.01f, shotgunBoxSize.y);
            shotgunKnockbackImpulse = Mathf.Max(0f, shotgunKnockbackImpulse);
        }
    }
}
