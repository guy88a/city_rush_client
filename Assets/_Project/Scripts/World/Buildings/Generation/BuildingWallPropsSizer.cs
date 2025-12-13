using CityRush.World.Buildings.Data;

namespace CityRush.World.Buildings.Generation
{
    public static class BuildingWallPropsSizer
    {
        public static void EnsureSize(
            BuildingPropsGrid grid,
            int floorsCount,
            int widthModules
        )
        {
            if (grid == null)
                return;

            if (floorsCount < 0)
                floorsCount = 0;

            if (widthModules < 0)
                widthModules = 0;

            // Ensure floor rows count

            while (grid.Floors.Count < floorsCount)
                grid.Floors.Add(new FloorPropsRow());

            while (grid.Floors.Count > floorsCount)
                grid.Floors.RemoveAt(grid.Floors.Count - 1);

            // Ensure module slots per floor

            for (int f = 0; f < grid.Floors.Count; f++)
            {
                var row = grid.Floors[f];

                if (row.Modules == null)
                    row.Modules = new();

                while (row.Modules.Count < widthModules)
                    row.Modules.Add(string.Empty);

                while (row.Modules.Count > widthModules)
                    row.Modules.RemoveAt(row.Modules.Count - 1);
            }
        }
    }
}
