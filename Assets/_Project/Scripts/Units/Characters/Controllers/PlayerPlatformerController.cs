using UnityEngine;
using UnityEngine.InputSystem;

namespace CityRush.Units.Characters.Controllers
{
    public class PlayerPlatformerController : PhysicsObject
    {
        public float maxSpeed = 7;
        public float jumpTakeoffSpeed = 7;

        private bool jumpPressed = false;
        private bool jumpReleased = false;

        private PlayerControls controls;

        void Start()
        {
            controls = new PlayerControls();
            controls.Player.Enable();

            controls.Player.Jump.started += ctx => jumpPressed = true;
            controls.Player.Jump.canceled += ctx => jumpReleased = true;
        }

        protected override void ComputeVelocity()
        {
            Vector2 move = Vector2.zero;

            Vector2 input = controls.Player.Move.ReadValue<Vector2>();
            move.x = input.x;

            if (jumpPressed && grounded)
            {
                velocity.y = jumpTakeoffSpeed;
                jumpPressed = false;
            }
            else if (jumpReleased)
            {
                if (velocity.y > 0)
                    velocity.y *= 0.5f;

                jumpReleased = false;
            }

            targetVelocity = move * maxSpeed;
        }

    }
}
