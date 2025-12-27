namespace CityRush.World.Map.Runtime
{
    public readonly struct MapNavigationSnapshot
    {
        public readonly MapPosition Current;
        public readonly StreetRef CurrentStreet;
        public readonly MapNeighbor Left;
        public readonly MapNeighbor Right;
        public readonly MapNeighbor Up;
        public readonly MapNeighbor Down;

        public MapNavigationSnapshot(
            MapPosition current,
            StreetRef currentStreet,
            MapNeighbor left,
            MapNeighbor right,
            MapNeighbor up,
            MapNeighbor down)
        {
            Current = current;
            CurrentStreet = currentStreet;
            Left = left;
            Right = right;
            Up = up;
            Down = down;
        }
    }
}
