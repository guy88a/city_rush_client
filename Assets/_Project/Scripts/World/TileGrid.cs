using UnityEngine;

namespace CityRush.World
{
    // Step 1: Core data structure for storing logical tile grid (no rendering yet)
    public class TileGrid
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        // Stores tile IDs (index to spritesheet atlas)
        private int[,] _tiles;

        public TileGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new int[width, height];
        }

        public void SetTile(int x, int y, int id)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            _tiles[x, y] = id;
        }

        public int GetTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return -1;
            return _tiles[x, y];
        }
    }
}
