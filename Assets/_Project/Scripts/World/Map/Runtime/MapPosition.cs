namespace CityRush.World.Map.Runtime
{
    public readonly struct MapPosition
    {
        public readonly int ZoneIndex;
        public readonly int Row;
        public readonly int Col;

        public MapPosition(int zoneIndex, int row, int col)
        {
            ZoneIndex = zoneIndex;
            Row = row;
            Col = col;
        }
    }
}
