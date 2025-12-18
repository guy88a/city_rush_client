using UnityEngine;

namespace CityRush.World.Background
{
    public class BackgroundLayer : MonoBehaviour
    {
        public BackgroundLayerConfig Config;

        Transform _tile0;
        Transform _tile1;
        float _halfLoop;

        float _lastCameraX;

        public void Initialize(float cameraX)
        {
            _lastCameraX = cameraX;

            if (Config.LoopHorizontally && transform.childCount >= 2)
            {
                _tile0 = transform.GetChild(0);
                _tile1 = transform.GetChild(1);
                _halfLoop = Config.LoopWidth * 0.5f;
            }
        }

        public void Tick(float cameraX)
        {
            if (!Config.FollowCameraX)
                return;

            float deltaX = cameraX - _lastCameraX;
            _lastCameraX = cameraX;

            float moveX = deltaX * Config.ParallaxMultiplier;
            transform.position += Vector3.right * moveX;

            if (!Config.LoopHorizontally || _tile0 == null)
                return;

            float camX = cameraX;
            if (_tile0.position.x + _halfLoop < camX)
                _tile0.position += Vector3.right * Config.LoopWidth;

            if (_tile1.position.x + _halfLoop < camX)
                _tile1.position += Vector3.right * Config.LoopWidth;

        }
    }
}
