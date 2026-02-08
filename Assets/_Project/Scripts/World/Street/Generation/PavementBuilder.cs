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
            Build(patternData, 0, 0);
        }

        public void Build(StreetPatternData patternData, int leftBleedTiles, int rightBleedTiles)
        {
            if (patternData == null || patternData.pattern == null || patternData.pattern.Length == 0)
                return;

            int repeat = Mathf.Max(1, patternData.repeat);
            int patternLen = patternData.pattern.Length;

            int mainCount = repeat * patternLen;
            int totalCount = Mathf.Max(0, leftBleedTiles) + mainCount + Mathf.Max(0, rightBleedTiles);

            int firstTileIndex = patternData.pattern[0];
            int lastTileIndex = patternData.pattern[patternLen - 1];

            float startXLocal = -Mathf.Max(0, leftBleedTiles) * _tileWidth;
            float yLocal = _y - _parent.position.y;

            for (int i = 0; i < totalCount; i++)
            {
                int tileIndex;

                if (i < leftBleedTiles)
                {
                    tileIndex = firstTileIndex;
                }
                else if (i >= leftBleedTiles + mainCount)
                {
                    tileIndex = lastTileIndex;
                }
                else
                {
                    int mainIndex = i - leftBleedTiles;
                    int local = mainIndex % patternLen;
                    tileIndex = patternData.pattern[local];
                }

                if (tileIndex < 0 || tileIndex >= _tiles.Length)
                    continue;

                var instance = Object.Instantiate(_tiles[tileIndex], _parent);
                instance.transform.localPosition = new Vector3(startXLocal + (i * _tileWidth), yLocal, 0f);

                var sr = instance.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sortingOrder = StreetSorting.Pavement;
            }
        }
    }
}
