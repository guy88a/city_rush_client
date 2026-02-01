using UnityEngine;

namespace CityRush.Units.Characters.Controllers
{
    /// <summary>
    /// Shared animation + animation-related locks for Player & NPC.
    ///
    /// This is the single source of truth for Animator parameter names.
    /// Controllers should call this instead of Animator.Set* directly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterBehavior : MonoBehaviour
    {
        private const string GraphicChildName = "Graphic";

        // -----------------
        // Animator params (must match Character_Base.controller)
        // -----------------
        private static readonly int SpeedHash = Animator.StringToHash("speed");
        private static readonly int IsJumpingHash = Animator.StringToHash("isJumping");
        private static readonly int TakeOffHash = Animator.StringToHash("takeOff");
        private static readonly int UziHash = Animator.StringToHash("Uzi");
        private static readonly int ShotgunHash = Animator.StringToHash("Shotgun");
        private static readonly int PunchHash = Animator.StringToHash("Punch");
        private static readonly int ThrowHash = Animator.StringToHash("Throw");
        private static readonly int IsUziFiringHash = Animator.StringToHash("isUziFiring");
        private static readonly int IsAliveHash = Animator.StringToHash("isAlive");
        private static readonly int WhackedHash = Animator.StringToHash("Whacked");
        private static readonly int IsWhackedHash = Animator.StringToHash("isWhacked");

        // -----------------
        // Animator states (Base Layer) used for automatic locks.
        // These are state *names* visible in the Animator graph.
        // -----------------
        private static readonly int FireShotgunStateHash = Animator.StringToHash("fire_shotgun");
        private static readonly int AttackPunchStateHash = Animator.StringToHash("attack_punch");
        private static readonly int AttackThrowStateHash = Animator.StringToHash("attack_throw");
        private static readonly int WhackedStateHash = Animator.StringToHash("whacked");
        private static readonly int DieStateHash = Animator.StringToHash("die");

        [Header("References")]
        [SerializeField] private Transform graphic;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;

        [Header("Locks")]
        [Tooltip("If enabled, CanMove/CanAct will be blocked while Animator is in action states (shotgun/punch/throw/whacked/die).")]
        [SerializeField] private bool lockByAnimatorState = true;

        [Tooltip("Block movement while in locked states.")]
        [SerializeField] private bool lockMovementByAnimatorState = true;

        [Tooltip("Block actions while in locked states.")]
        [SerializeField] private bool lockActionsByAnimatorState = true;

        // Optional explicit lock windows (used by external systems if needed).
        private float _moveLockedUntil;
        private float _actionLockedUntil;

        private bool _alive = true;

        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public Animator Animator => animator;

        public bool IsAlive => _alive;

        public bool CanMove => !IsMovementLocked;
        public bool CanAct => !IsActionLocked;

        public bool IsMovementLocked
        {
            get
            {
                if (!_alive) return true;
                if (Time.time < _moveLockedUntil) return true;
                if (!lockByAnimatorState || !lockMovementByAnimatorState) return false;
                return IsInLockedAnimatorState();
            }
        }

        public bool IsActionLocked
        {
            get
            {
                if (!_alive) return true;
                if (Time.time < _actionLockedUntil) return true;
                if (!lockByAnimatorState || !lockActionsByAnimatorState) return false;
                return IsInLockedAnimatorState();
            }
        }

        private void Awake()
        {
            ResolveRefs();
        }

        private void OnEnable()
        {
            // Pool-safe reset.
            ResolveRefs();

            _alive = true;
            _moveLockedUntil = 0f;
            _actionLockedUntil = 0f;

            if (animator == null) return;

            // Reset common bools to sane defaults for freshly enabled objects.
            animator.SetBool(IsAliveHash, true);
            animator.SetBool(IsUziFiringHash, false);
            animator.SetBool(IsWhackedHash, false);
            animator.SetBool(IsJumpingHash, false);
            animator.SetFloat(SpeedHash, 0f);

            // Clear any pooled leftover triggers.
            animator.ResetTrigger(TakeOffHash);
            animator.ResetTrigger(UziHash);
            animator.ResetTrigger(ShotgunHash);
            animator.ResetTrigger(PunchHash);
            animator.ResetTrigger(ThrowHash);
            animator.ResetTrigger(WhackedHash);
        }

        private void ResolveRefs()
        {
            if (graphic == null)
                graphic = transform.Find(GraphicChildName);

            if (graphic != null)
            {
                if (spriteRenderer == null)
                    spriteRenderer = graphic.GetComponent<SpriteRenderer>();

                if (animator == null)
                    animator = graphic.GetComponent<Animator>();
            }

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        // -----------------
        // Explicit locks
        // -----------------

        public void ClearLocks()
        {
            _moveLockedUntil = 0f;
            _actionLockedUntil = 0f;
        }

        public void LockMovementFor(float seconds)
        {
            if (seconds <= 0f) return;
            _moveLockedUntil = Mathf.Max(_moveLockedUntil, Time.time + seconds);
        }

        public void LockActionsFor(float seconds)
        {
            if (seconds <= 0f) return;
            _actionLockedUntil = Mathf.Max(_actionLockedUntil, Time.time + seconds);
        }

        public void LockAllFor(float seconds)
        {
            LockMovementFor(seconds);
            LockActionsFor(seconds);
        }

        // -----------------
        // Animator param API
        // -----------------

        public void SetSpeed(float absSpeed)
        {
            if (animator == null) return;
            animator.SetFloat(SpeedHash, absSpeed);
        }

        public void SetJumping(bool isJumping)
        {
            if (animator == null) return;
            animator.SetBool(IsJumpingHash, isJumping);
        }

        public void TriggerTakeOff()
        {
            if (animator == null) return;
            animator.SetTrigger(TakeOffHash);
        }

        public void TriggerUzi()
        {
            if (animator == null) return;
            animator.SetTrigger(UziHash);
        }

        public void TriggerShotgun()
        {
            if (animator == null) return;
            animator.SetTrigger(ShotgunHash);
        }

        public void TriggerPunch()
        {
            if (animator == null) return;
            animator.SetTrigger(PunchHash);
        }

        public void TriggerThrow()
        {
            if (animator == null) return;
            animator.SetTrigger(ThrowHash);
        }

        public void SetUziFiring(bool isFiring)
        {
            if (animator == null) return;
            animator.SetBool(IsUziFiringHash, isFiring);
        }

        public void SetAlive(bool isAlive)
        {
            _alive = isAlive;

            if (animator == null) return;
            animator.SetBool(IsAliveHash, isAlive);

            if (!isAlive)
            {
                // Hard-block everything (until re-enabled / reset).
                _moveLockedUntil = float.PositiveInfinity;
                _actionLockedUntil = float.PositiveInfinity;
            }
        }

        public void TriggerWhacked()
        {
            if (animator == null) return;
            animator.SetTrigger(WhackedHash);
        }

        public void SetWhacked(bool isWhacked)
        {
            if (animator == null) return;
            animator.SetBool(IsWhackedHash, isWhacked);
        }

        // -----------------
        // Convenience (animation + locks)
        // -----------------

        public void PlayWhacked(float moveLockSeconds = 0f, float actionLockSeconds = 0f)
        {
            TriggerWhacked();
            SetWhacked(true);

            if (moveLockSeconds > 0f) LockMovementFor(moveLockSeconds);
            if (actionLockSeconds > 0f) LockActionsFor(actionLockSeconds);
        }

        // -----------------
        // Internals
        // -----------------

        private bool IsInLockedAnimatorState(int layer = 0)
        {
            if (animator == null) return false;

            // Use Next state while transitioning so locks apply immediately after triggering.
            int stateHash = animator.IsInTransition(layer)
                ? animator.GetNextAnimatorStateInfo(layer).shortNameHash
                : animator.GetCurrentAnimatorStateInfo(layer).shortNameHash;

            if (stateHash == 0)
                return false;

            return stateHash == FireShotgunStateHash
                || stateHash == AttackPunchStateHash
                || stateHash == AttackThrowStateHash
                || stateHash == WhackedStateHash
                || stateHash == DieStateHash;
        }
    }
}
