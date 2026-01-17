// File: Assets/Project/Scripts/Units/Characters/Combat/PlayerCombatDriver.cs
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

        [SerializeField] private string boolIsUziFiring = "isUziFiring";

        [Header("Uzi")]
        [SerializeField] private Vector2 uziSpawnOffset = new Vector2(0.6f, 0.15f);

        [Header("Shotgun")]
        [SerializeField] private Vector2 shotgunSpawnOffset = new Vector2(0.8f, 0.15f);

        [Header("Cooldowns (seconds)")]
        //[SerializeField] private float uziShotsPerSecond = 12f; // later: skills can scale this
        [SerializeField] private float shotgunCooldown = 0.45f;
        [SerializeField] private float punchCooldown = 0.25f;
        [SerializeField] private float throwCooldown = 0.6f;

        [Header("Lock Durations (seconds)")]
        [SerializeField] private float shotgunLockDuration = 0.18f;
        [SerializeField] private float meleeLockDuration = 0.22f;
        [SerializeField] private float throwLockDuration = 0.22f;

        [Header("Animator (set these to match your Animator Controller)")]
        [SerializeField] private string trigShotgun = "Shotgun";
        [SerializeField] private string trigPunch = "Punch";
        [SerializeField] private string trigThrow = "Throw";

        public bool IsActionLocked { get; private set; }

        private PlayerPlatformerController _controller;
        private Animator _animator;
        private Transform _graphic;
        private SpriteRenderer _graphicSprite;

        private WeaponRuntime _weaponRuntime;

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
            _weaponRuntime = GetComponent<WeaponRuntime>();

            _graphic = transform.Find("Graphic");
            if (_graphic != null)
                _animator = _graphic.GetComponent<Animator>();

            if (_graphic != null)
                _graphicSprite = _graphic.GetComponent<SpriteRenderer>();

            // Self-contained actions so we do not depend on generated PlayerControls yet.
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
                SetUziFiring(false);

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
                yield return null; // cooldown gates the actual fire
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
                    if (!_controller.IsGrounded) return;

                    SetUziFiring(false);
                    FireShotgun(lockMovement: true, lockDuration: shotgunLockDuration);
                    _nextAltTime = now + _weaponRuntime.EquippedWeapon.FireInterval;
                    return;
                }

                // LMB
                if (movingInput)
                {
                    // Try to shoot (this also auto-starts reload if mag is empty and reserve exists).
                    FireUzi();

                    bool uziAnim =
                        _weaponRuntime != null &&
                        _weaponRuntime.EquippedWeapon != null &&
                        _weaponRuntime.EquippedWeapon.Type == WeaponType.Uzi &&
                        !_weaponRuntime.IsReloading &&
                        (
                            _weaponRuntime.EquippedWeapon.MagazineSize <= 0 || // no-mag weapon (not Uzi, but safe)
                            _weaponRuntime.Magazine > 0                         // has bullets loaded
                        );

                    SetUziFiring(uziAnim);

                    float interval = 0.1f;
                    if (_weaponRuntime != null && _weaponRuntime.EquippedWeapon != null)
                        interval = Mathf.Max(0.01f, _weaponRuntime.EquippedWeapon.FireInterval);

                    _nextPrimaryTime = now + interval;
                }
                else
                {
                    if (!_controller.IsGrounded) return;

                    SetUziFiring(false);
                    FireShotgun(lockMovement: true, lockDuration: shotgunLockDuration);
                    _nextPrimaryTime = now + _weaponRuntime.EquippedWeapon.FireInterval;
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

        private Vector2 GetFacingDirection()
        {
            if (_graphicSprite != null && _graphicSprite.flipX)
                return Vector2.left;

            return Vector2.right;
        }


        private bool FireUzi()
        {
            if (_weaponRuntime == null) return false;

            Vector2 dir = GetFacingDirection();
            Vector2 origin = (Vector2)transform.position + new Vector2(uziSpawnOffset.x * dir.x, uziSpawnOffset.y);

            return _weaponRuntime.TryFireUzi(origin, dir);
        }

        private void SetUziFiring(bool isFiring)
        {
            if (_animator == null) return;
            if (string.IsNullOrEmpty(boolIsUziFiring)) return;

            _animator.SetBool(boolIsUziFiring, isFiring);
        }

        private void FireShotgun(bool lockMovement, float lockDuration)
        {
            Trigger(trigShotgun);

            if (_weaponRuntime != null)
            {
                Vector2 dir = GetFacingDirection();
                Vector2 origin = (Vector2)transform.position + new Vector2(shotgunSpawnOffset.x * dir.x, shotgunSpawnOffset.y);

                _weaponRuntime.TryFireShotgun(origin, dir);
            }

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
