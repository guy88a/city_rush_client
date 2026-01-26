using System;
using UnityEngine;
using CityRush.World.Interior;
using UnityEngine.InputSystem;
using CityRush.Items;
using CityRush.Items.World;

namespace CityRush.Units.Characters.Controllers
{
    public class PlayerPlatformerController : PhysicsObject
    {
        public float maxSpeed = 10;
        public float jumpTakeoffSpeed = 10;

        private bool jumpPressed = false;
        private bool jumpReleased = false;

        public bool IsGrounded => grounded;

        private PlayerControls controls;
        private SpriteRenderer spriteRenderer;
        private Animator animator;

        private BuildingDoor _currentBuildingDoor;
        public BuildingDoor CurrentBuildingDoor => _currentBuildingDoor;

        public event Action<BuildingDoor> OnBuildingDoorInteract;

        private ApartmentDoor _currentApartmentDoor;
        public ApartmentDoor CurrentApartmentDoor => _currentApartmentDoor;

        public event Action<ApartmentDoor> OnApartmentDoorInteract;

        private WorldObjectUnit _currentWorldObject;
        public WorldObjectUnit CurrentWorldObject => _currentWorldObject;

        public event Action<WorldObjectUnit> OnWorldObjectInteract;

        private PlayerItemsRuntime _itemsRuntime;
        private ItemPickup _currentItemPickup;

        public bool IsMovementEnabled { get; private set; } = true;

        // Raw horizontal input from the Move action (A/D). Updated every ComputeVelocity().
        public float MoveInputX { get; private set; }

        // True when the player is pressing A/D (even if movement is currently locked).
        public bool IsMovingInput => Mathf.Abs(MoveInputX) > 0.01f;

        public bool IsFrozen { get; private set; }

        private void Awake()
        {
            Transform graphic = transform.Find("Graphic");
            spriteRenderer = graphic.GetComponent<SpriteRenderer>();
            animator = graphic.GetComponent<Animator>();
            _itemsRuntime = GetComponent<PlayerItemsRuntime>();
        }

        void Start()
        {
            controls = new PlayerControls();
            controls.Player.Enable();

            controls.Player.Jump.started += ctx => jumpPressed = grounded;
            controls.Player.Jump.canceled += ctx => jumpReleased = true;
        }

        protected override void ComputeVelocity()
        {
            if (IsFrozen) { return; }

            if (_currentBuildingDoor != null && Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame)
            {
                OnBuildingDoorInteract?.Invoke(_currentBuildingDoor);
            }

            if (_currentApartmentDoor != null && Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame)
            {
                OnApartmentDoorInteract?.Invoke(_currentApartmentDoor);
            }

            if (_currentWorldObject != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                OnWorldObjectInteract?.Invoke(_currentWorldObject);
            }

            if (_itemsRuntime != null && Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
            {
                const int HealingPotionItemId = 3002; // your potion id
                bool ok = _itemsRuntime.TryUseConsumable(HealingPotionItemId);

                Debug.Log($"[Consumable] H pressed -> use ok={ok}", this);
                _itemsRuntime.DebugPrintInventory();
            }

            if (_currentItemPickup != null && Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame)
            {
                if (_itemsRuntime != null)
                {
                    bool ok = _currentItemPickup.TryLoot(_itemsRuntime);
                    //Debug.Log($"[Loot] Z pressed -> TryLoot ok={ok} pickupItemId={_currentItemPickup.ItemId} amount={_currentItemPickup.Amount}", this);

                    if (ok)
                        _currentItemPickup = null; // pickup may be destroyed
                }
            }

            Vector2 move = Vector2.zero;

            Vector2 input = controls.Player.Move.ReadValue<Vector2>();
            MoveInputX = input.x;

            // Always read input for logic, but optionally prevent applying it to movement.
            move.x = IsMovementEnabled ? MoveInputX : 0f;

            if (jumpPressed && grounded)
            {
                velocity.y = jumpTakeoffSpeed;
                jumpPressed = false;
                animator.SetTrigger("takeOff");
            }
            else if (jumpReleased)
            {
                if (velocity.y > 0)
                    velocity.y *= 0.5f;

                jumpReleased = false;
            }

            if(!grounded || velocity.y > 0)
            {
                animator.SetBool("isJumping", true);
            } else
            {
                animator.SetBool("isJumping", false);
            }

                bool flipSprite = (spriteRenderer.flipX ? (move.x > 0) : (move.x < 0));
            if (flipSprite)
            {
                spriteRenderer.flipX = !spriteRenderer.flipX;
            }

            targetVelocity = move * maxSpeed;

            animator.SetFloat("speed", Math.Abs(move.x * maxSpeed));
        }

        public void Freeze()
        {
            IsFrozen = true;
            targetVelocity = Vector2.zero;
            velocity = Vector2.zero;
        }

        public void Unfreeze()
        {
            Debug.Log("PLAYER UNFREEZE!!!");
            IsFrozen = false;
            targetVelocity = Vector2.zero; // reset stale frame
        }

        public void SetMovementEnabled(bool enabled)
        {
            IsMovementEnabled = enabled;

            if (!enabled)
            {
                // Stop horizontal motion immediately, keep vertical physics.
                targetVelocity = Vector2.zero;
                velocity = new Vector2(0f, velocity.y);
            }
            else
            {
                // Reset stale frame; next ComputeVelocity will apply input normally.
                targetVelocity = Vector2.zero;
            }
        }

        public void ClearInteractionState()
        {
            _currentBuildingDoor = null;
            _currentApartmentDoor = null;
            _currentWorldObject = null;
            jumpPressed = false;
            jumpReleased = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ItemPickup pickup = other.GetComponentInParent<ItemPickup>();
            if (pickup != null)
            {
                _currentItemPickup = pickup;

                // Auto-loot ONLY tokens
                if (_itemsRuntime != null &&
                    _itemsRuntime.ItemsDb != null &&
                    _itemsRuntime.ItemsDb.TryGet(pickup.ItemId, out var def) &&
                    def.Category.Trim().Equals("Token", System.StringComparison.OrdinalIgnoreCase))
                {
                    pickup.TryLoot(_itemsRuntime);
                    _currentItemPickup = null;
                }

                return;
            }

            BuildingDoor buildingDoor = other.GetComponentInParent<BuildingDoor>();
            if (buildingDoor != null) { _currentBuildingDoor = buildingDoor; return; }

            ApartmentDoor apartmentDoor = other.GetComponentInParent<ApartmentDoor>();
            if (apartmentDoor != null) _currentApartmentDoor = apartmentDoor;

            WorldObjectUnit worldObject = other.GetComponentInParent<WorldObjectUnit>();
            if (worldObject != null) _currentWorldObject = worldObject;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            ItemPickup pickup = other.GetComponentInParent<ItemPickup>();
            if (pickup != null && pickup == _currentItemPickup)
                _currentItemPickup = null;

            BuildingDoor buildingDoor = other.GetComponentInParent<BuildingDoor>();
            if (buildingDoor != null && buildingDoor == _currentBuildingDoor) _currentBuildingDoor = null;

            ApartmentDoor apartmentDoor = other.GetComponentInParent<ApartmentDoor>();
            if (apartmentDoor != null && apartmentDoor == _currentApartmentDoor) _currentApartmentDoor = null;

            WorldObjectUnit worldObject = other.GetComponentInParent<WorldObjectUnit>();
            if (worldObject != null && worldObject == _currentWorldObject) _currentWorldObject = null;
        }
    }
}
