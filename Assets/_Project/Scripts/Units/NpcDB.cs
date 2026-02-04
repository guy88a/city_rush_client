using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityRush.Units
{
    // Keep this enum intentionally minimal in v1.
    // Expand only when you actually need additional categories.
    public enum NpcCategory : byte
    {
        None = 0,
        Generic = 1,
        Resident = 2,
        Police = 3,
    }

    [Serializable]
    public struct NpcStats
    {
        [Min(1)] public int maxHealth;
        [Min(0)] public int armor;
        [Min(0)] public int power;
    }

    // Weapons are defined by CharacterWeaponSet slots (Uzi / Shotgun / Sniper).
    // Values are ItemIds (ItemsDb) where the item is a weapon.
    [Serializable]
    public struct NpcWeaponSetData
    {
        [Tooltip("ItemsDb ItemId. 0 = empty slot.")]
        public int uziItemId;
        [Tooltip("ItemsDb ItemId. 0 = empty slot.")]
        public int shotgunItemId;
        [Tooltip("ItemsDb ItemId. 0 = empty slot.")]
        public int sniperItemId;
    }

    [Serializable]
    public struct NpcQuestLinks
    {
        public int[] startQuestIds;
        public int[] endQuestIds;
    }

    [Serializable]
    public sealed class NpcDefinition
    {
        [Header("Identity")]
        [SerializeField] private int npcId;
        [SerializeField] private string displayName;
        [SerializeField] private NpcCategory category = NpcCategory.Generic;

        [Header("Stats")]
        [SerializeField] private NpcStats stats;

        [Header("Weapons")]
        [SerializeField] private NpcWeaponSetData weapons;

        [Header("Quests")]
        [SerializeField] private NpcQuestLinks quests;

        [Header("Loot")]
        [SerializeField] private int lootTableId;

        public int NpcId => npcId;
        public string DisplayName => displayName;
        public NpcCategory Category => category;
        public NpcStats Stats => stats;
        public NpcWeaponSetData Weapons => weapons;
        public NpcQuestLinks Quests => quests;
        public int LootTableId => lootTableId;
    }

    [CreateAssetMenu(menuName = "CityRush/Units/NPC DB", fileName = "NpcDB")]
    public sealed class NpcDB : ScriptableObject
    {
        [SerializeField] private List<NpcDefinition> npcs = new();

        [NonSerialized] private Dictionary<int, NpcDefinition> _byId;
        [NonSerialized] private bool _cacheBuilt;

        public List<NpcDefinition> Npcs => npcs;

        public void BuildCacheIfNeeded()
        {
            if (_cacheBuilt)
                return;

            _cacheBuilt = true;

            if (_byId == null)
                _byId = new Dictionary<int, NpcDefinition>(npcs != null ? npcs.Count : 0);
            else
                _byId.Clear();

            if (npcs == null)
                return;

            for (int i = 0; i < npcs.Count; i++)
            {
                var def = npcs[i];
                if (def == null)
                    continue;

                int id = def.NpcId;
                if (id <= 0)
                    continue;

                if (_byId.ContainsKey(id))
                {
                    Debug.LogWarning($"[NpcDB] Duplicate NpcId {id} in '{name}' (index {i}). Keeping first.", this);
                    continue;
                }

                _byId.Add(id, def);
            }
        }

        public bool TryGet(int npcId, out NpcDefinition def)
        {
            def = null;

            if (npcId <= 0)
                return false;

            BuildCacheIfNeeded();
            return _byId != null && _byId.TryGetValue(npcId, out def);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure duplicates/missing ids are caught early.
            _cacheBuilt = false;
            BuildCacheIfNeeded();
        }
#endif
    }
}
