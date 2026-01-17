using System;
using UnityEngine;
using CityRush.Units.Characters;
using CityRush.Units.Characters.Movement;

namespace CityRush.Units.Characters.Controllers
{
    public sealed class NPCController : PhysicsObject
    {
        [Header("Movement")]
        [SerializeField] private float maxSpeed = 6f;

        [Tooltip("Extra margin outside bounds before despawn.")]
        [SerializeField] private float despawnMargin = 2f;

        private float _streetMinX;
        private float _streetMaxX;
        private bool _hasStreetBounds;

        private int _moveDir = 1; // +1 right, -1 left

        private SpriteRenderer _spriteRenderer;
        private Animator _animator;

        public Action<NPCController> OnDespawn;

        [Header("Combat Behavior")]
        private int aggression = 5;
        [SerializeField] private float chaseStopDistance = 1f;

        private bool _fightMode; // true = chase target, false = flee (later)

        private CharacterCombatState _combatState;

        public int Aggression
        {
            get => aggression;
            set => aggression = Mathf.Clamp(value, 0, 10);
        }

        public float MaxSpeed
        {
            get => maxSpeed;
            set => maxSpeed = value;
        }

        public int MoveDir
        {
            get => _moveDir;
            set => _moveDir = value >= 0 ? 1 : -1;
        }

        public void SetStreetBounds(float minX, float maxX)
        {
            _streetMinX = minX;
            _streetMaxX = maxX;
            _hasStreetBounds = true;
        }

        public void SetupPatrol(float minX, float maxX, int moveDir, float speed)
        {
            SetStreetBounds(minX, maxX);
            MoveDir = moveDir;
            MaxSpeed = speed;
        }

        private void Awake()
        {
            Debug.Log($"[NPC Awake] name={gameObject.name} id={GetInstanceID()} aggression={aggression}");
            aggression = Mathf.Clamp(aggression, 0, 10);
            Debug.Log($"[NPC Awake] name={gameObject.name} id={GetInstanceID()} aggression={aggression}");

            Transform graphic = transform.Find("Graphic");
            if (graphic != null)
            {
                _spriteRenderer = graphic.GetComponent<SpriteRenderer>();
                _animator = graphic.GetComponent<Animator>();
            }

            _combatState = GetComponent<CharacterCombatState>();
            if (_combatState != null)
                _combatState.OnCombatEntered += HandleCombatEntered;

            if (_combatState != null)
            {
                _combatState.OnCombatEntered += HandleCombatEntered;
                _combatState.OnCombatExited += HandleCombatExited;
            }
        }

        private void OnDestroy()
        {
            if (_combatState != null)
                _combatState.OnCombatEntered -= HandleCombatEntered;

            if (_combatState != null)
            {
                _combatState.OnCombatEntered -= HandleCombatEntered;
                _combatState.OnCombatExited -= HandleCombatExited;
            }
        }

        private void HandleCombatEntered()
        {
            int chance = Mathf.Clamp(aggression, 0, 10) * 10;
            int roll = UnityEngine.Random.Range(0, 100);
            _fightMode = roll < chance;
            Debug.Log($"[NPC] Aggression={aggression} Chance={chance}% Roll={roll} => {(_fightMode ? "fight" : "flee")}");

            MaxSpeed = UnityEngine.Random.Range(CharacterSpeedSettings.MinRunSpeed, CharacterSpeedSettings.MaxRunSpeed);
        }

        private void HandleCombatExited()
        {
            _fightMode = false;
        }

        private bool DecideFight()
        {
            int chance = Mathf.Clamp(aggression, 0, 10) * 10; // 0..100
            return UnityEngine.Random.Range(0, 100) < chance;
        }

        protected override void ComputeVelocity()
        {
            if (_hasStreetBounds)
            {
                float x = transform.position.x;
                if (x < _streetMinX - despawnMargin || x > _streetMaxX + despawnMargin)
                {
                    OnDespawn?.Invoke(this);
                    gameObject.SetActive(false);
                    return;
                }
            }

            // Combat chase (fight mode)
            if (_combatState != null && _combatState.IsInCombat && _fightMode && _combatState.HasTarget)
            {
                Vector2 myPos = rb != null ? rb.position : (Vector2)transform.position;
                Vector2 targetPos = _combatState.Target.transform.position;

                float dx = targetPos.x - myPos.x;

                if (Mathf.Abs(dx) <= chaseStopDistance)
                {
                    targetVelocity = Vector2.zero;

                    if (_animator != null)
                        _animator.SetFloat("speed", 0f);

                    return;
                }

                int dir = dx >= 0f ? 1 : -1;
                Vector2 moveCombat = new Vector2(dir, 0f);

                if (_spriteRenderer != null)
                {
                    bool flipSprite = (_spriteRenderer.flipX ? (moveCombat.x > 0f) : (moveCombat.x < 0f));
                    if (flipSprite)
                        _spriteRenderer.flipX = !_spriteRenderer.flipX;
                }

                targetVelocity = moveCombat * maxSpeed;

                if (_animator != null)
                    _animator.SetFloat("speed", Mathf.Abs(moveCombat.x * maxSpeed));

                return;
            }


            Vector2 move = Vector2.zero;
            move.x = MoveDir;

            if (_spriteRenderer != null)
            {
                bool flipSprite = (_spriteRenderer.flipX ? (move.x > 0f) : (move.x < 0f));
                if (flipSprite)
                    _spriteRenderer.flipX = !_spriteRenderer.flipX;
            }

            targetVelocity = move * maxSpeed;

            if (_animator != null)
                _animator.SetFloat("speed", Math.Abs(move.x * maxSpeed));
        }
    }
}
