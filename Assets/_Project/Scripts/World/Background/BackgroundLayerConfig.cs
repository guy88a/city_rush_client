using UnityEngine;

namespace CityRush.World.Background
{
    [System.Serializable]
    public class BackgroundLayerConfig
    {
        [Header("Movement")]
        public float ParallaxMultiplier = 0f;
        public bool FollowCameraX = true;

        [Header("Looping")]
        public bool LoopHorizontally = false;
        public float LoopWidth = 0f;

        [Header("Positioning")]
        public float BaseYOffset = 0f;

        [Header("Debug")]
        public bool DrawGizmos = false;
    }
}
