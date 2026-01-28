using CityRush.Quests;
using CityRush.Quests.Data;
using CityRush.Units;
using CityRush.Units.Characters.Controllers;
using UnityEngine;
using CityRush.UI;

namespace CityRush.Units.Characters.Combat
{
    [DisallowMultipleComponent]
    public sealed class CharacterDeath : MonoBehaviour
    {
        [SerializeField] private string isAliveParam = "isAlive";

        private Health _health;
        private NPCController _npcController;
        private Rigidbody2D _rb;
        private Animator _animator;

        private bool _handledDeath;

        [Header("Despawn After Death")]
        [SerializeField] private float despawnDelay = 3f;
        [SerializeField] private float fadeOutDuration = 0.35f;

        private SpriteRenderer _graphicSr;
        private Coroutine _despawnRoutine;

        private NpcIdentity _npcIdentity;
        private IQuestService _questService;

        private PlayerPlatformerController _playerController;
        private PlayerCombatDriver _playerCombat;

        private PlayerUIController _playerUi;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _npcController = GetComponent<NPCController>();
            _rb = GetComponent<Rigidbody2D>();

            Transform graphic = transform.Find("Graphic");
            if (graphic != null)
            {
                _graphicSr = graphic.GetComponent<SpriteRenderer>();
                _animator = graphic.GetComponent<Animator>();
            }

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            _npcIdentity = GetComponent<NpcIdentity>();

            var host = Object.FindFirstObjectByType<QuestServiceHost>();
            _questService = host != null ? host.Service : null;

            _playerController = GetComponent<PlayerPlatformerController>();
            _playerCombat = GetComponent<PlayerCombatDriver>();

            _playerUi = GetComponent<PlayerUIController>();
        }

        private void OnEnable()
        {
            _handledDeath = false;

            if (_despawnRoutine != null)
            {
                StopCoroutine(_despawnRoutine);
                _despawnRoutine = null;
            }

            ResetGraphicAlpha();

            if (_health != null)
                _health.OnDied += HandleDied;

            // If we respawned alive, allow control again.
            if (_npcController != null && _health != null && _health.IsAlive)
                _npcController.enabled = true;

            SyncAnimatorAlive();
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.OnDied -= HandleDied;
        }

        private void SyncAnimatorAlive()
        {
            if (_animator == null || _health == null) return;
            _animator.SetBool(isAliveParam, _health.IsAlive);
        }

        private void HandleDied()
        {
            if (_handledDeath) return;
            _handledDeath = true;

            // Quest kill credit (player-only)
            if (_questService != null && _health != null && _npcIdentity != null)
            {
                GameObject attackerRoot = _health.LastAttackerRoot;
                if (attackerRoot != null)
                {
                    var attackerUnit = attackerRoot.GetComponent<GameUnit>();
                    if (attackerUnit != null && attackerUnit.EntryType == GameUnitType.Player)
                    {
                        _questService.SubmitEvent(new QuestEvent(QuestActionType.Kill, _npcIdentity.Id, 1));
                    }
                }
            }

            if (_animator != null)
                _animator.SetBool(isAliveParam, false);

            // Freeze movement/actions (keep RB + collisions enabled)
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            if (_npcController != null)
                _npcController.enabled = false;

            if (_despawnRoutine != null)
            {
                StopCoroutine(_despawnRoutine);
                _despawnRoutine = null;
            }

            // PLAYER: freeze + disable combat, but do NOT despawn/fade.
            if (_playerController != null)
            {
                _playerController.EnterDeadLock();
                _playerCombat?.EnterDeadLock();

                // Force the killer NPC to exit combat so it won't keep firing / get stuck.
                if (_health != null)
                {
                    GameObject attackerRoot = _health.LastAttackerRoot;
                    if (attackerRoot != null)
                    {
                        var attackerNpc = attackerRoot.GetComponent<NPCController>();
                        if (attackerNpc == null)
                            attackerNpc = attackerRoot.GetComponentInChildren<NPCController>();

                        attackerNpc?.ForceExitCombat();
                    }
                }

                _playerUi?.ShowRespawnDialog();

                return;
            }

            // NPC: keep existing despawn behavior.
            _despawnRoutine = StartCoroutine(DeathDespawnRoutine());
        }

        private System.Collections.IEnumerator DeathDespawnRoutine()
        {
            yield return new WaitForSeconds(despawnDelay);

            float t = 0f;
            float dur = Mathf.Max(0.01f, fadeOutDuration);

            while (t < dur)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(1f - (t / dur));
                SetGraphicAlpha(a);
                yield return null;
            }

            SetGraphicAlpha(0f);

            // Return to pool via the spawn manager path (active list removal + respawn schedule).
            if (_npcController != null && _npcController.OnDespawn != null)
                _npcController.OnDespawn.Invoke(_npcController);
            else
                gameObject.SetActive(false);
        }

        private void ResetGraphicAlpha()
        {
            SetGraphicAlpha(1f);
        }

        private void SetGraphicAlpha(float a)
        {
            if (_graphicSr == null) return;

            Color c = _graphicSr.color;
            c.a = a;
            _graphicSr.color = c;
        }

    }
}
