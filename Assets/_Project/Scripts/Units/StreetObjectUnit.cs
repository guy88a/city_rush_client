using UnityEngine;

namespace CityRush.Units
{
    // World object that is spawned/managed on streets (and later window/sniper views).
    // Behavior is added via separate components (Destroyable, Lockpickable, Readable, etc.).
    [DisallowMultipleComponent]
    public sealed class StreetObjectUnit : WorldObjectUnit
    {
        // Intentionally empty.
    }
}
