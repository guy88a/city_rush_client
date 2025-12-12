namespace CityRush.World.Buildings
{
    // Centralized sorting orders for building-related sprites.
    // Keep this boring and explicit.
    public static class BuildingSorting
    {
        public const int Walls = 0;
        public const int SideDecorations = 1;
        public const int RooftopFloor = 1;
        public const int Separators = 2;
        public const int Windows = 3;
        public const int Doors = 4;

        public const int RooftopWall = 5;
        public const int Player = 6;             // reference value only
        public const int RoofSep = 7;

        public const int PropsMin = 8;
        public const int PropsMax = 10;
    }
}
