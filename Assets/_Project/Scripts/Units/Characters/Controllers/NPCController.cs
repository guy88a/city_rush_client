using System;
using UnityEngine;

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
            Transform graphic = transform.Find("Graphic");
            _spriteRenderer = graphic.GetComponent<SpriteRenderer>();
            _animator = graphic.GetComponent<Animator>();
        }

        protected override void ComputeVelocity()
        {
            if (_hasStreetBounds)
            {
                float x = transform.position.x;
                if (x < _streetMinX - despawnMargin || x > _streetMaxX + despawnMargin)
                {
                    gameObject.SetActive(false);
                    return;
                }
            }

            Vector2 move = Vector2.zero;
            move.x = MoveDir;

            bool flipSprite = (_spriteRenderer.flipX ? (move.x > 0f) : (move.x < 0f));
            if (flipSprite)
                _spriteRenderer.flipX = !_spriteRenderer.flipX;

            targetVelocity = move * maxSpeed;

            if (_animator != null)
                _animator.SetFloat("speed", Math.Abs(move.x * maxSpeed));
        }
    }
}
