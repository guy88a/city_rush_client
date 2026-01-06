namespace CityRush.World.Map.Runtime
{
    public readonly struct MapNeighbor
    {
        public readonly bool Exists;
        public readonly bool IsEmpty;
        public readonly StreetRef Street;
        public readonly MapPosition? TargetPosition;

        public MapNeighbor(
            bool exists,
            bool isEmpty,
            StreetRef street,
            MapPosition? targetPosition)
        {
            Exists = exists;
            IsEmpty = isEmpty;
            Street = street;
            TargetPosition = targetPosition;
        }
    }
}
