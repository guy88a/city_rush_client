using System.Collections.Generic;
using UnityEngine;

namespace CityRush.Core.Services
{
    public sealed class AudioProfilesRegistry
    {
        private readonly Dictionary<string, AudioAreaProfile> _profiles = new();

        public void Add(AudioAreaProfile profile)
        {
            if (profile == null || string.IsNullOrEmpty(profile.Id))
                return;

            _profiles[profile.Id] = profile;
        }

        public bool TryGet(string id, out AudioAreaProfile profile)
            => _profiles.TryGetValue(id, out profile);
    }
}
