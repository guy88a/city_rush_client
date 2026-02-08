using System.Collections;
using UnityEngine;
using CityRush.Items;
using CityRush.Units.Characters.Combat;

namespace CityRush.Units
{
    [DisallowMultipleComponent]
    public sealed class NpcRuntimeData : MonoBehaviour
    {
        private NpcIdentity _id;
        private NpcDefinition _def;

        private bool _visualsApplied;
        private bool _graphicCached;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;

        private bool _weaponsApplied;
        private Coroutine _weaponsRetryCo;

        public int NpcId => _id != null ? _id.Id : 0;
        public NpcDefinition Definition => _def;

        private void Awake()
        {
            _id = GetComponent<NpcIdentity>();
            if (_id == null)
                Debug.LogError("[NpcRuntimeData] Missing NpcIdentity on NPC root.", this);
        }

        private void Start()
        {
            ApplyFromDb();
        }

        public void ApplyFromDb()
        {
            if (_id == null || _id.Id <= 0)
                return;

            var host = NpcDbHost.Instance != null ? NpcDbHost.Instance : FindFirstObjectByType<NpcDbHost>();
            if (host == null || host.NpcDb == null)
            {
                Debug.LogWarning($"[NpcRuntimeData] No NpcDbHost/NpcDB found for npcId={_id.Id}.", this);
                return;
            }

            if (!host.NpcDb.TryGet(_id.Id, out _def) || _def == null)
            {
                Debug.LogWarning($"[NpcRuntimeData] npcId={_id.Id} not found in NpcDB.", this);
                return;
            }

            ApplyVisuals(_def, host);
            ApplyStats(_def);

            // Weapons may depend on PlayerItemsRuntime being spawned later -> retry.
            TryApplyWeapons(host);
        }

        private void ApplyStats(NpcDefinition def)
        {
            var health = GetComponent<Health>();
            if (health != null)
            {
                int maxHp = Mathf.Max(1, def.Stats.maxHealth);
                health.SetMaxHp(maxHp, refill: true);
            }

            var stats = GetComponent<CombatStats>();
            if (stats != null)
            {
                stats.Power = Mathf.Max(0, def.Stats.power);
                stats.Armor = Mathf.Max(0, def.Stats.armor);
            }
        }

        private void ApplyVisuals(NpcDefinition def, NpcDbHost host)
        {
            if (_visualsApplied)
                return;

            // Empty key => keep prefab defaults.
            string visualKey = def != null ? def.VisualKey : null;
            if (string.IsNullOrWhiteSpace(visualKey))
                return;

            var visualsDb = host != null ? host.NpcVisualsDb : null;
            if (visualsDb == null)
            {
                Debug.LogWarning($"[NpcRuntimeData] NpcVisualsDB missing; cannot apply visuals for npcId={NpcId} visualKey='{visualKey}'.", this);
                _visualsApplied = true;
                return;
            }

            if (!visualsDb.TryGet(visualKey, out var visual) || visual == null)
            {
                Debug.LogWarning($"[NpcRuntimeData] visualKey='{visualKey}' not found in NpcVisualsDB (npcId={NpcId}).", this);
                _visualsApplied = true;
                return;
            }

            CacheGraphicRefsIfNeeded();

            if (_spriteRenderer != null && visual.Sprite != null)
                _spriteRenderer.sprite = visual.Sprite;

            if (_animator != null && visual.OverrideController != null)
                _animator.runtimeAnimatorController = visual.OverrideController;

            _visualsApplied = true;
        }

        private void CacheGraphicRefsIfNeeded()
        {
            if (_graphicCached)
                return;

            _graphicCached = true;

            Transform graphic = transform.Find("Graphic");
            if (graphic != null)
            {
                _spriteRenderer = graphic.GetComponent<SpriteRenderer>();
                _animator = graphic.GetComponent<Animator>();
            }

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
        }

        private void TryApplyWeapons(NpcDbHost host)
        {
            if (_weaponsApplied)
                return;

            var itemsDb = host != null ? host.ItemsDb : null;
            if (itemsDb == null)
            {
                if (_weaponsRetryCo == null)
                    _weaponsRetryCo = StartCoroutine(RetryWeaponsUntilItemsDb());
                return;
            }

            ApplyWeapons(_def, itemsDb);
            _weaponsApplied = true;

            if (_weaponsRetryCo != null)
            {
                StopCoroutine(_weaponsRetryCo);
                _weaponsRetryCo = null;
            }
        }

        private IEnumerator RetryWeaponsUntilItemsDb()
        {
            // ~1 second @ 60fps.
            for (int i = 0; i < 60; i++)
            {
                yield return null;

                var host = NpcDbHost.Instance != null ? NpcDbHost.Instance : FindFirstObjectByType<NpcDbHost>();
                if (host == null)
                    continue;

                var itemsDb = host.ItemsDb;
                if (itemsDb == null)
                    continue;

                ApplyWeapons(_def, itemsDb);
                _weaponsApplied = true;
                _weaponsRetryCo = null;
                yield break;
            }

            Debug.LogWarning($"[NpcRuntimeData] ItemsDb still missing after retry; cannot equip weapons for npcId={NpcId}.", this);
            _weaponsRetryCo = null;
        }

        private void ApplyWeapons(NpcDefinition def, ItemsDb itemsDb)
        {
            var weaponSet = GetComponent<CharacterWeaponSet>();
            if (weaponSet == null)
                return;

            EquipWeaponItemId(itemsDb, weaponSet, def.Weapons.uziItemId);
            EquipWeaponItemId(itemsDb, weaponSet, def.Weapons.shotgunItemId);
            EquipWeaponItemId(itemsDb, weaponSet, def.Weapons.sniperItemId);
        }

        private void EquipWeaponItemId(ItemsDb db, CharacterWeaponSet weaponSet, int itemId)
        {
            if (itemId <= 0)
                return;

            if (!db.TryGet(itemId, out var itemDef) || itemDef == null)
            {
                Debug.LogWarning($"[NpcRuntimeData] Weapon itemId={itemId} not found in ItemsDb (npcId={NpcId}).", this);
                return;
            }

            if (!itemDef.IsWeapon || itemDef.Weapon == null)
            {
                Debug.LogWarning($"[NpcRuntimeData] itemId={itemId} is not a Weapon (npcId={NpcId}).", this);
                return;
            }

            string weaponDefId = itemDef.Weapon.WeaponDefinitionId;
            if (string.IsNullOrWhiteSpace(weaponDefId))
            {
                Debug.LogWarning($"[NpcRuntimeData] itemId={itemId} has empty WeaponDefinitionId (npcId={NpcId}).", this);
                return;
            }

            bool ok = weaponSet.TryEquipWeaponByDefinitionId(weaponDefId);
            if (!ok)
                Debug.LogWarning($"[NpcRuntimeData] Failed equipping weaponDefId='{weaponDefId}' for itemId={itemId} (npcId={NpcId}).", this);
        }
    }
}
