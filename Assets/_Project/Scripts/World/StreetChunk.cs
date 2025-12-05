using UnityEngine;

namespace CityRush.World
{
    // Step 2: A chunk of a street loaded from external tile data
    public class StreetChunk
    {
        public string ChunkName { get; private set; }
        public int Width => _grid.Width;
        public int Height => _grid.Height;

        private TileGrid _grid;

        public StreetChunk(string name)
        {
            ChunkName = name;
        }

        /// <summary>
        /// Loads the chunk from a 2D tile ID array.
        /// Automatically sizes the TileGrid.
        /// </summary>
        public void LoadFromData(int[][] data)
        {
            if (data == null || data.Length == 0 || data[0].Length == 0)
            {
                Debug.LogError($"StreetChunk '{ChunkName}' received invalid data.");
                return;
            }

            int width = data.Length;
            int height = data[0].Length;

            _grid = new TileGrid(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int id = data[x][y];

                    // Optional: clamp invalid IDs
                    if (id < -1)
                        id = -1;

                    _grid.SetTile(x, y, id);
                }
            }
        }

        public int GetTile(int x, int y)
        {
            return _grid.GetTile(x, y);
        }
    }
}
