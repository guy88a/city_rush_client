using UnityEngine;

namespace CityRush.World.Street
{
    // Attach this to: Street/Backstreet
    public sealed class BackstreetParallax : MonoBehaviour
    {
        [SerializeField] private float parallaxMultiplier = 0.25f;

        private Transform _cam;
        private float _lastCamX;

        private void Start()
        {
            // Dynamic camera lookup (no Inspector wiring).
            // Prefer Camera.main if tagged, fallback to any camera in scene.
            Camera cam = Camera.main;
            if (cam == null)
                cam = FindAnyObjectByType<Camera>();

            if (cam == null)
            {
                Debug.LogError("[BackstreetParallax] No Camera found in scene.");
                enabled = false;
                return;
            }

            _cam = cam.transform;
            _lastCamX = _cam.position.x;
        }

        private void LateUpdate()
        {
            float camX = _cam.position.x;
            float dx = camX - _lastCamX;

            if (dx != 0f)
            {
                Vector3 p = transform.position;
                p.x += dx * parallaxMultiplier;
                transform.position = p;

                _lastCamX = camX;
            }
        }
    }
}
