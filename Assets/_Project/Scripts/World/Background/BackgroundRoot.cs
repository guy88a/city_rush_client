using UnityEngine;

namespace CityRush.World.Background
{
    public class BackgroundRoot : MonoBehaviour
    {
        public Transform CameraTransform;
        public BackgroundLayer[] Layers;
        public bool UseLateUpdate = true;

        float _lastCameraX;
        bool _initialized;

        void Start()
        {
            if (CameraTransform == null)
                return;

            _lastCameraX = CameraTransform.position.x;

            for (int i = 0; i < Layers.Length; i++)
            {
                if (Layers[i] != null)
                    Layers[i].Initialize(_lastCameraX);
            }

            _initialized = true;
        }

        void Update()
        {
            if (!UseLateUpdate)
                Tick();
        }

        void LateUpdate()
        {
            if (UseLateUpdate)
                Tick();
        }

        void Tick()
        {
            if (!_initialized || CameraTransform == null)
                return;

            float cameraX = CameraTransform.position.x;

            for (int i = 0; i < Layers.Length; i++)
            {
                if (Layers[i] != null)
                    Layers[i].Tick(cameraX);
            }

            _lastCameraX = cameraX;
        }

        public void SetLayersActive(bool isActive)
        {
            if (Layers == null) return;

            for (int i = 0; i < Layers.Length; i++)
            {
                if (Layers[i] != null)
                    Layers[i].gameObject.SetActive(isActive);
            }
        }
    }
}
