using UnityEngine;
using CityRush.World.Street.Data;

namespace CityRush.World.Street.Generation
{
    public class PavementBuilder
    {
        private readonly Transform _parent;
        private readonly GameObject[] _tiles;
        private readonly float _tileWidth;
        private readonly float _y;
        private readonly float xOffset = -20;

        public PavementBuilder(
            Transform parent,
            GameObject[] tiles,
            float tileWidth,
            float yPosition)
        {
            _parent = parent;
            _tiles = tiles;
            _tileWidth = tileWidth;
            _y = yPosition;
        }

        public void Build(StreetPatternData patternData)
        {
            if (patternData == null || patternData.pattern == null)
                return;

            int repeat = Mathf.Max(1, patternData.repeat);
            int index = 0;

            for (int r = 0; r < repeat; r++)
            {
                for (int i = 0; i < patternData.pattern.Length; i++)
                {
                    int tileIndex = patternData.pattern[i];

                    if (tileIndex < 0 || tileIndex >= _tiles.Length)
                        continue;

                    Vector3 position = new Vector3(
                        (index * _tileWidth) + xOffset,
                        _y,
                        0f
                    );

                    var instance = Object.Instantiate(
                        _tiles[tileIndex],
                        position,
                        Quaternion.identity,
                        _parent
                    );

                    var sr = instance.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = StreetSorting.Pavement;
                    }

                    index++;
                }
            }
        }
    }
}
