using UnityEngine;

namespace CityRush.Units
{
    // World object that exists as a static prop inside building/apartment prefabs.
    // Behavior is added via separate components (Destroyable, Lockpickable, Readable, etc.).
    [DisallowMultipleComponent]
    public sealed class BuildingPropUnit : WorldObjectUnit
    {
        // Intentionally empty.
    }
}
