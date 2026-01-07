using UnityEngine;

namespace CityRush.World.Interior
{
    public sealed class ApartmentWindowNavTarget : MonoBehaviour
    {
        [SerializeField] private Transform cameraAnchor;

        public Vector3 GetCameraFocusPosition()
        {
            return cameraAnchor != null ? cameraAnchor.position : transform.position;
        }
    }
}
