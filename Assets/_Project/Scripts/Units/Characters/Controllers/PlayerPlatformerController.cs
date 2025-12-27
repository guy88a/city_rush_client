using System;
using UnityEngine;
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

        public bool IsFrozen { get; private set; }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
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
            IsFrozen = false;
        }
    }
}
