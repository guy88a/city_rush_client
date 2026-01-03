using System;
using UnityEngine;
using CityRush.World.Interior;
using UnityEngine.InputSystem;

namespace CityRush.Units.Characters.Controllers
{
    public class PlayerPlatformerController : PhysicsObject
    {
        public float maxSpeed = 10;
        public float jumpTakeoffSpeed = 10;

        private bool jumpPressed = false;
        private bool jumpReleased = false;

        private PlayerControls controls;
        private SpriteRenderer spriteRenderer;
        private Animator animator;

        private BuildingDoor _currentBuildingDoor;
        public BuildingDoor CurrentBuildingDoor => _currentBuildingDoor;

        public event Action<BuildingDoor> OnBuildingDoorInteract;

        public bool IsFrozen { get; private set; }

        private void Awake()
        {
            Transform graphic = transform.Find("Graphic");
            spriteRenderer = graphic.GetComponent<SpriteRenderer>();
            animator = graphic.GetComponent<Animator>();
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

            Vector2 move = Vector2.zero;

            Vector2 input = controls.Player.Move.ReadValue<Vector2>();
            move.x = input.x;

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

        private void OnTriggerEnter2D(Collider2D other)
        {
            BuildingDoor door = other.GetComponentInParent<BuildingDoor>();
            if (door != null) _currentBuildingDoor = door;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            BuildingDoor door = other.GetComponentInParent<BuildingDoor>();
            if (door != null && door == _currentBuildingDoor) _currentBuildingDoor = null;
        }
    }
}
