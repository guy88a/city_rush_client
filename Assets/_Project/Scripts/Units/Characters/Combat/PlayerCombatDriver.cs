using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using CityRush.Units.Characters.Controllers;

namespace CityRush.Units.Characters.Combat
{
    public sealed class PlayerCombatDriver : MonoBehaviour
    {
        public enum WeaponMode
        {
            Gun,
            Melee
        }

        [Header("Mode")]
        [SerializeField] private WeaponMode selectedMode = WeaponMode.Gun;

        [Header("Cooldowns (seconds)")]
        [SerializeField] private float uziCooldown = 0.08f;
        [SerializeField] private float shotgunCooldown = 0.45f;
        [SerializeField] private float punchCooldown = 0.25f;
        [SerializeField] private float throwCooldown = 0.6f;

        [Header("Lock Durations (seconds)")]
        [SerializeField] private float shotgunLockDuration = 0.18f;
        [SerializeField] private float meleeLockDuration = 0.22f;
        [SerializeField] private float throwLockDuration = 0.22f;

        [Header("Animator (set these to match your Animator Controller)")]
        [SerializeField] private string trigUzi = "Uzi";
        [SerializeField] private string trigShotgun = "Shotgun";
        [SerializeField] private string trigPunch = "Punch";
        [SerializeField] private string trigThrow = "Throw";

        public bool IsActionLocked { get; private set; }

        private PlayerPlatformerController _controller;
        private Animator _animator;

        private InputAction _primaryAction;
        private InputAction _altAction;

        private bool _isPrimaryHeld;
        private bool _isAltHeld;

        private Coroutine _primaryLoop;
        private Coroutine _altLoop;

        private float _nextPrimaryTime;
        private float _nextAltTime;

        private void Awake()
        {
            _controller = GetComponent<PlayerPlatformerController>();

            Transform graphic = transform.Find("Graphic");
            if (graphic != null)
                _animator = graphic.GetComponent<Animator>();

            // Self-contained actions so we do not depend on your generated PlayerControls yet.
            _primaryAction = new InputAction("FirePrimary", InputActionType.Button, "<Mouse>/leftButton");
            _altAction = new InputAction("FireAlt", InputActionType.Button, "<Mouse>/rightButton");
        }

        private void OnEnable()
        {
            _primaryAction.Enable();
            _altAction.Enable();

            _primaryAction.started += OnPrimaryStarted;
            _primaryAction.canceled += OnPrimaryCanceled;

            _altAction.started += OnAltStarted;
            _altAction.canceled += OnAltCanceled;
        }

        private void OnDisable()
        {
            _primaryAction.started -= OnPrimaryStarted;
            _primaryAction.canceled -= OnPrimaryCanceled;

            _altAction.started -= OnAltStarted;
            _altAction.canceled -= OnAltCanceled;

            _primaryAction.Disable();
            _altAction.Disable();

            StopAllCombatLoops();
            ForceUnlockMovement();
        }

        public void SetWeaponMode(WeaponMode mode) => selectedMode = mode;

        private void OnPrimaryStarted(InputAction.CallbackContext ctx) => SetPrimaryHeld(true);
        private void OnPrimaryCanceled(InputAction.CallbackContext ctx) => SetPrimaryHeld(false);

        private void OnAltStarted(InputAction.CallbackContext ctx) => SetAltHeld(true);
        private void OnAltCanceled(InputAction.CallbackContext ctx) => SetAltHeld(false);

        public void SetPrimaryHeld(bool held)
        {
            _isPrimaryHeld = held;

            if (held)
            {
                if (_primaryLoop == null)
                    _primaryLoop = StartCoroutine(FireLoop(isAlt: false));
            }
            else
            {
                if (_primaryLoop != null)
                {
                    StopCoroutine(_primaryLoop);
                    _primaryLoop = null;
                }
            }
        }

        public void SetAltHeld(bool held)
        {
            _isAltHeld = held;

            if (held)
            {
                if (_altLoop == null)
                    _altLoop = StartCoroutine(FireLoop(isAlt: true));
            }
            else
            {
                if (_altLoop != null)
                {
                    StopCoroutine(_altLoop);
                    _altLoop = null;
                }
            }
        }

        private IEnumerator FireLoop(bool isAlt)
        {
            while ((isAlt && _isAltHeld) || (!isAlt && _isPrimaryHeld))
            {
                TryFireOnce(isAlt);
                yield return null; // allows immediate responsiveness; cooldown gates the actual fire
            }
        }

        private void TryFireOnce(bool isAlt)
        {
            if (_controller == null) return;
            if (_controller.IsFrozen) return;

            float now = Time.time;
            if (isAlt)
            {
                if (now < _nextAltTime) return;
            }
            else
            {
                if (now < _nextPrimaryTime) return;
            }

            bool movingInput = _controller.IsMovingInput;

            if (selectedMode == WeaponMode.Gun)
            {
                if (isAlt)
                {
                    // RMB: always Shotgun. If moving, stop movement, shoot, then resume automatically.
                    FireShotgun(lockMovement: true, lockDuration: shotgunLockDuration, resumeOnUnlock: true, isAltChannel: true);
                    _nextAltTime = now + shotgunCooldown;
                    return;
                }

                // LMB
                if (movingInput)
                {
                    // Moving: Uzi, no lock.
                    FireUzi(isAltChannel: false);
                    _nextPrimaryTime = now + uziCooldown;
                }
                else
                {
                    // Idle: Shotgun, lock.
                    FireShotgun(lockMovement: true, lockDuration: shotgunLockDuration, resumeOnUnlock: false, isAltChannel: false);
                    _nextPrimaryTime = now + shotgunCooldown;
                }

                return;
            }

            // Melee selected
            if (isAlt)
            {
                FireThrow(lockDuration: throwLockDuration);
                _nextAltTime = now + throwCooldown;
            }
            else
            {
                FirePunch(lockDuration: meleeLockDuration);
                _nextPrimaryTime = now + punchCooldown;
            }
        }

        private void FireUzi(bool isAltChannel)
        {
            Trigger(trigUzi);
        }

        private void FireShotgun(bool lockMovement, float lockDuration, bool resumeOnUnlock, bool isAltChannel)
        {
            Trigger(trigShotgun);

            if (lockMovement)
                StartActionLock(lockDuration);
        }

        private void FirePunch(float lockDuration)
        {
            Trigger(trigPunch);
            StartActionLock(lockDuration);
        }

        private void FireThrow(float lockDuration)
        {
            Trigger(trigThrow);
            StartActionLock(lockDuration);
        }

        private void Trigger(string triggerName)
        {
            if (_animator == null) return;
            if (string.IsNullOrEmpty(triggerName)) return;

            _animator.SetTrigger(triggerName);
        }

        private void StartActionLock(float duration)
        {
            if (_controller == null) return;

            // Do not stack locks.
            if (IsActionLocked) return;

            IsActionLocked = true;
            _controller.SetMovementEnabled(false);

            StartCoroutine(ActionLockRoutine(duration));
        }

        private IEnumerator ActionLockRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);

            IsActionLocked = false;

            if (_controller != null)
                _controller.SetMovementEnabled(true);
        }

        private void StopAllCombatLoops()
        {
            if (_primaryLoop != null) { StopCoroutine(_primaryLoop); _primaryLoop = null; }
            if (_altLoop != null) { StopCoroutine(_altLoop); _altLoop = null; }
            _isPrimaryHeld = false;
            _isAltHeld = false;
        }

        private void ForceUnlockMovement()
        {
            if (_controller == null) return;
            _controller.SetMovementEnabled(true);
            IsActionLocked = false;
        }
    }
}
