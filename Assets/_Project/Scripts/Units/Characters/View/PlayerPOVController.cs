using UnityEngine;
using CityRush.Units.Characters.Controllers;

namespace CityRush.Units.Characters.View
{
    [DisallowMultipleComponent]
    public sealed class PlayerPOVController : MonoBehaviour
    {
        private const string GRAPHIC_CHILD_NAME = "Graphic";

        private PlayerPlatformerController _platformer;
        private Rigidbody2D _rb;
        private Transform _graphic;

        private bool _isInPOV;

        private bool _platformerWasEnabled;
        private bool _rbSimulatedWasEnabled;

        public bool IsInPOV => _isInPOV;

        private void Awake()
        {
            _platformer = GetComponent<PlayerPlatformerController>();
            _rb = GetComponent<Rigidbody2D>();
            _graphic = transform.Find(GRAPHIC_CHILD_NAME);

            if (_graphic == null)
                Debug.LogError($"[PlayerPOVController] Missing child '{GRAPHIC_CHILD_NAME}'.", this);
        }

        public void EnterPOV()
        {
            if (_isInPOV) return;
            _isInPOV = true;

            // Clear interaction state BEFORE disabling (otherwise OnTriggerExit won't run)
            if (_platformer != null)
                _platformer.ClearInteractionState();

            // Hide visuals (keep root alive for refs)
            if (_graphic != null)
                _graphic.gameObject.SetActive(false);

            // Disable gameplay controller (stops Update + FixedUpdate in PhysicsObject chain)
            if (_platformer != null)
            {
                _platformerWasEnabled = _platformer.enabled;
                _platformer.enabled = false;
            }

            // Disable physics simulation entirely
            if (_rb != null)
            {
                _rbSimulatedWasEnabled = _rb.simulated;

                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;

                _rb.simulated = false;
            }
        }

        public void ExitPOV()
        {
            if (!_isInPOV) return;
            _isInPOV = false;

            // Restore physics
            if (_rb != null)
            {
                _rb.simulated = _rbSimulatedWasEnabled;

                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            // Restore controller
            if (_platformer != null)
            {
                _platformer.enabled = _platformerWasEnabled;

                // Clean resume state
                _platformer.Unfreeze();
            }

            // Restore visuals
            if (_graphic != null)
                _graphic.gameObject.SetActive(true);
        }
    }
}
