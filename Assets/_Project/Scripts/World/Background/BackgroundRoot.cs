using UnityEngine;

namespace CityRush.World.Background
{
    public class BackgroundRoot : MonoBehaviour
    {
        [Header("References")]
        public Transform CameraTransform;

        [Header("Layers")]
        public BackgroundLayer[] Layers;

        [Header("Settings")]
        public bool UseLateUpdate = true;
    }
}
