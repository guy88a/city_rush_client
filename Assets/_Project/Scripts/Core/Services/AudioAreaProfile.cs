using System;
using UnityEngine;

namespace CityRush.Core.Services
{
    [Serializable]
    public sealed class AudioAreaProfile
    {
        public string Id; // e.g. "Street_Default", "Corridor", "Apartment", etc.

        [Serializable]
        public struct AmbientLayer
        {
            public AmbientType Type;
            public AudioClip Clip;
            [Range(0f, 1f)] public float Volume01;
        }

        public AmbientLayer[] AmbientLayers;
    }
}
