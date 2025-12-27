namespace CityRush.World.Map.Runtime
{
    public readonly struct MapNavigationSnapshot
    {
        public readonly MapPosition Current;
        public readonly MapNeighbor Left;
        public readonly MapNeighbor Right;
        public readonly MapNeighbor Up;
        public readonly MapNeighbor Down;

        public MapNavigationSnapshot(
            MapPosition current,
            MapNeighbor left,
            MapNeighbor right,
            MapNeighbor up,
            MapNeighbor down)
        {
            Current = current;
            Left = left;
            Right = right;
            Up = up;
            Down = down;
        }
    }
}
