using System;
using UnityEngine;

namespace CityRush.Units
{
    public abstract class GameUnit : MonoBehaviour
    {
        [Header("Identity (Entry)")]
        [SerializeField] private GameUnitType entryType;
        [SerializeField] private int entryId;

        [Header("Identity (Instance)")]
        [SerializeField] private string instanceGuid;

        [Header("UI")]
        [SerializeField] private string displayName;

        public GameUnitType EntryType => entryType;
        public int EntryId => entryId;

        // Read-only at runtime; use SetInstanceGuid for controlled override (spawn/restore).
        public string InstanceGuid => instanceGuid;

        public string DisplayName => displayName;

        // WoW-style key: npc/215, item/5516, etc.
        public string EntryKey => $"{entryType.ToString().ToLowerInvariant()}/{entryId}";

        protected virtual void Awake()
        {
            EnsureInstanceGuid();
        }

        public void EnsureInstanceGuid()
        {
            if (!string.IsNullOrWhiteSpace(instanceGuid)) return;
            instanceGuid = Guid.NewGuid().ToString("N");
        }

        // Intended for spawn/restore only.
        public void SetInstanceGuid(string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
                throw new ArgumentException("InstanceGuid cannot be null/empty.", nameof(guid));

            instanceGuid = guid;
        }

        // Optional: allow setting entry fields via code when spawning (kept controlled).
        public void SetEntry(GameUnitType type, int id)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "EntryId must be > 0.");
            entryType = type;
            entryId = id;
        }

        public void SetDisplayName(string name)
        {
            displayName = name ?? string.Empty;
        }
    }
}
