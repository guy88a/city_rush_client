namespace CityRush.Units.Characters.Movement
{
    public static class CharacterSpeedSettings
    {
        // Street NPCs (units/sec)
        public const float MinWalkSpeed = 2f;
        public const float MaxWalkSpeed = 4f;

        public const float MinRunSpeed = 5.1f;
        public const float MaxRunSpeed = 7f;

        // Animation controller rule: running when Speed > this value.
        public const float RunAnimThreshold = 5f;
    }
}
