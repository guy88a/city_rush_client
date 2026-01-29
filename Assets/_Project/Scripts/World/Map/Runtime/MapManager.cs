namespace CityRush.World.Map.Runtime
{
    public sealed class MapManager
    {
        private readonly MapData _mapData;
        private readonly MapPosition _playerSpawnPosition;
        private MapPosition _currentPosition;
        private MapNavigationSnapshot _currentNavigation;

        public MapPosition CurrentPosition => _currentPosition;
        public MapNavigationSnapshot CurrentNavigation => _currentNavigation;

        public MapManager(MapData mapData)
        {
            _mapData = mapData;

            // Initial position comes from MapData.PlayerSpawn
            _currentPosition = new MapPosition(
                zoneIndex: 0,
                row: mapData.PlayerSpawn.y,
                col: mapData.PlayerSpawn.x
            );
            _playerSpawnPosition = _currentPosition;

            RebuildNavigationSnapshot();
        }

        // ------------------------------------------------------------
        // Read API
        // ------------------------------------------------------------

        public MapNeighbor GetNeighbor(MapDirection direction)
        {
            return direction switch
            {
                MapDirection.Left => _currentNavigation.Left,
                MapDirection.Right => _currentNavigation.Right,
                MapDirection.Up => _currentNavigation.Up,
                MapDirection.Down => _currentNavigation.Down,
                _ => default
            };
        }

        public bool CanMove(MapDirection direction)
        {
            var neighbor = GetNeighbor(direction);
            return neighbor.Exists && !neighbor.IsEmpty;
        }

        public StreetRef GetStreetRef(MapDirection direction)
        {
            var neighbor = GetNeighbor(direction);
            return neighbor.Exists && !neighbor.IsEmpty
                ? neighbor.Street
                : null;
        }

        public StreetRef GetCurrentStreet()
        {
            var zone = _mapData.Zones[_currentPosition.ZoneIndex];
            return zone.Structure[_currentPosition.Row].Streets[_currentPosition.Col];
        }

        // ------------------------------------------------------------
        // Write API (called ONLY after successful street load)
        // ------------------------------------------------------------

        public void CommitMove(MapDirection direction)
        {
            var neighbor = GetNeighbor(direction);

            if (!neighbor.Exists || neighbor.TargetPosition == null)
                return;

            _currentPosition = neighbor.TargetPosition.Value;
            RebuildNavigationSnapshot();
        }

        // ------------------------------------------------------------
        // Internal snapshot building
        // ------------------------------------------------------------

        private void RebuildNavigationSnapshot()
        {
            _currentNavigation = new MapNavigationSnapshot(
                _currentPosition,
                GetStreetAt(_currentPosition),
                BuildNeighbor(MapDirection.Left),
                BuildNeighbor(MapDirection.Right),
                BuildNeighbor(MapDirection.Up),
                BuildNeighbor(MapDirection.Down)
            );
        }

        private StreetRef GetStreetAt(MapPosition pos)
        {
            var zone = _mapData.Zones[pos.ZoneIndex];
            return zone.Structure[pos.Row].Streets[pos.Col];
        }

        private MapNeighbor BuildNeighbor(MapDirection direction)
        {
            int targetRow = _currentPosition.Row;
            int targetCol = _currentPosition.Col;

            switch (direction)
            {
                case MapDirection.Left: targetCol--; break;
                case MapDirection.Right: targetCol++; break;
                case MapDirection.Up: targetRow--; break;
                case MapDirection.Down: targetRow++; break;
            }

            if (!IsInsideGrid(targetRow, targetCol))
                return new MapNeighbor(false, false, null, null);

            var zone = _mapData.Zones[_currentPosition.ZoneIndex];
            var rowData = zone.Structure[targetRow];

            if (rowData == null || targetCol >= rowData.Streets.Count)
                return new MapNeighbor(false, false, null, null);

            var streetRef = rowData.Streets[targetCol];

            if (streetRef == null)
                return new MapNeighbor(false, false, null, null);

            bool isEmpty = IsStreetEmpty(streetRef);

            return new MapNeighbor(
                exists: true,
                isEmpty: isEmpty,
                street: streetRef,
                targetPosition: new MapPosition(
                    _currentPosition.ZoneIndex,
                    targetRow,
                    targetCol
                )
            );
        }

        private bool IsInsideGrid(int row, int col)
        {
            var zone = _mapData.Zones[_currentPosition.ZoneIndex];

            if (row < 0 || col < 0)
                return false;

            if (row >= zone.Structure.Count)
                return false;

            if (col >= zone.Structure[row].Streets.Count)
                return false;

            return true;
        }

        public void ResetToPlayerSpawn()
        {
            _currentPosition = _playerSpawnPosition;
            RebuildNavigationSnapshot();
        }

        // ------------------------------------------------------------
        // Emptiness rule (data-only, no loading yet)
        // ------------------------------------------------------------

        private bool IsStreetEmpty(StreetRef streetRef)
        {
            // Placeholder rule for now:
            // StreetRef exists => assumed non-empty
            // This will be extended later when we safely inspect StreetData
            return false;
        }
    }
}
