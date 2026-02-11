using UnityEngine;

namespace CityRush.Core.Services
{
    public sealed class AudioServiceHost : MonoBehaviour
    {
        private AudioService _owner;

        public void Bind(AudioService owner)
        {
            _owner = owner;
        }

        private void Update()
        {
            _owner?.Tick();
        }

        public IAudioService GetAudio()
        {
            return _owner;
        }
    }
}
