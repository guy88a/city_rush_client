using System;
using UnityEngine;
using CityRush.Units.Characters;
using CityRush.Units.Characters.Combat;
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
        private CharacterWeaponSet _weapons;

        [SerializeField] private Vector2 uziSpawnOffset = new Vector2(0.6f, 0.15f);
        [SerializeField] private Vector2 shotgunSpawnOffset = new Vector2(0.8f, 0.15f);

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
            {
                _combatState.OnCombatEntered += HandleCombatEntered;
                _combatState.OnCombatExited += HandleCombatExited;
            }

            _weapons = GetComponent<CharacterWeaponSet>();
        }

        private void OnDestroy()
        {
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
                float absDx = Mathf.Abs(dx);

                WeaponDefinition uziW = _weapons != null ? _weapons.UziWeapon : null;
                WeaponDefinition shotW = _weapons != null ? _weapons.ShotgunWeapon : null;

                float uziRange = GetWeaponRange(uziW);
                float shotRange = GetWeaponRange(shotW);

                // 1) If in shotgun range => STOP + shotgun
                if (shotW != null && shotRange > 0f && absDx <= shotRange)
                {
                    targetVelocity = Vector2.zero;

                    // Face target while firing.
                    if (_spriteRenderer != null)
                    {
                        bool wantFlip = dx < 0f;
                        if (_spriteRenderer.flipX != wantFlip)
                            _spriteRenderer.flipX = wantFlip;
                    }

                    if (_animator != null)
                    {
                        _animator.SetFloat("speed", 0f);
                        _animator.SetBool("isUziFiring", false);
                    }

                    Vector2 fireDir = GetFacingDirectionFromDx(dx);
                    Vector2 origin = (Vector2)transform.position + new Vector2(shotgunSpawnOffset.x * fireDir.x, shotgunSpawnOffset.y);

                    bool fired = _weapons != null && _combatState.Target != null
                        && _weapons.TryFireShotgun(origin, fireDir, _combatState.Target);

                    if (fired && _animator != null)
                        _animator.SetTrigger("Shotgun");

                    return;
                }

                // 2) Otherwise => RUN toward target (always)
                int dir = dx >= 0f ? 1 : -1;
                Vector2 moveCombat = new Vector2(dir, 0f);

                if (_spriteRenderer != null)
                {
                    bool wantFlip = dir < 0;
                    if (_spriteRenderer.flipX != wantFlip)
                        _spriteRenderer.flipX = wantFlip;
                }

                targetVelocity = moveCombat * maxSpeed;

                if (_animator != null)
                    _animator.SetFloat("speed", Mathf.Abs(moveCombat.x * maxSpeed));

                // 3) If in uzi range while running => fire uzi (no stopping)
                if (uziW != null && uziRange > 0f && absDx <= uziRange)
                {
                    Vector2 fireDir = GetFacingDirectionFromDx(dx);
                    Vector2 origin = (Vector2)transform.position + new Vector2(uziSpawnOffset.x * fireDir.x, uziSpawnOffset.y);

                    bool fired = _weapons != null && _combatState.Target != null
                        && _weapons.TryFireUzi(origin, fireDir, _combatState.Target);

                    if (_animator != null)
                    {
                        if (fired)
                            _animator.SetTrigger("Uzi");

                        // optional: if your graph also uses this bool
                        _animator.SetBool("isUziFiring", _weapons.IsUziAnimActive());
                    }
                }
                else
                {
                    if (_animator != null)
                        _animator.SetBool("isUziFiring", false);
                }

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

        private float GetWeaponRange(WeaponDefinition w)
        {
            if (w == null) return 0f;

            if (w.Type == WeaponType.Uzi)
                return Mathf.Max(0f, w.ProjectileSpeed * w.ProjectileLifetime);

            if (w.Type == WeaponType.Shotgun)
                return Mathf.Max(0f, Mathf.Abs(w.ShotgunBoxOffset.x) + (w.ShotgunBoxSize.x * 0.5f));

            return 0f;
        }

        private Vector2 GetFacingDirectionFromDx(float dx)
        {
            return dx >= 0f ? Vector2.right : Vector2.left;
        }
    }
}
