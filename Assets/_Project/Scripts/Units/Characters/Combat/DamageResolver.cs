using CityRush.Units.Characters;
using CityRush.Units;
using UnityEngine;


namespace CityRush.Units.Characters.Combat
{
    [DisallowMultipleComponent]
    public sealed class DamageResolver : MonoBehaviour
    {
        [SerializeField] private bool clampToMinimumOne = true;


        private CombatStats _selfStats;


        private void Awake()
        {
            _selfStats = GetComponent<CombatStats>();
        }


        public int ResolveFinalDamage(int baseDamage, CombatStats targetStats)
        {
            int power = _selfStats != null ? _selfStats.Power : 0;
            int armor = targetStats != null ? targetStats.Armor : 0;


            int dmg = baseDamage + power - armor;
            if (clampToMinimumOne) dmg = Mathf.Max(1, dmg);
            return dmg;
        }


        public bool TryApplyDamage(GameObject target, int baseDamage)
        {
            if (target == null) return false;


            Health health = target.GetComponentInParent<Health>();
            Destroyable destroyable = null;

            if (health == null)
            {
                destroyable = target.GetComponentInParent<Destroyable>();
                if (destroyable == null) return false;
            }

            CombatStats targetStats = target.GetComponent<CombatStats>();
            int finalDamage = ResolveFinalDamage(baseDamage, targetStats);

            var attackerCombat = GetComponent<CharacterCombatState>();
            if (attackerCombat != null)
                attackerCombat.EnterCombat();

            var victimCombat = target.GetComponentInParent<CharacterCombatState>();
            if (victimCombat != null)
                victimCombat.EnterCombat();

            var attackerUnit = GetComponent<CharacterUnit>();
            if (victimCombat != null && attackerUnit != null)
                victimCombat.SetTarget(attackerUnit);

            if (health != null)
            {
                // Track who last damaged the victim (used for player-only kill quest credit).
                health.SetLastAttackerRoot(attackerUnit != null ? attackerUnit.gameObject : gameObject);

                health.TakeDamage(finalDamage);
                return true;
            }

            return destroyable != null && destroyable.TryHit(finalDamage);
        }


        public bool TryApplyDamage(Collider2D targetCollider, int baseDamage)
        {
            if (targetCollider == null) return false;
            return TryApplyDamage(targetCollider.gameObject, baseDamage);
        }
    }
}