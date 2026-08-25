using System.Collections.Generic;
using UnityEngine;

namespace Match3.Data
{
    /// <summary>
    /// Designer-facing configuration for a match-3 board: grid size, tile
    /// variety, matching rules and animation timings.
    /// </summary>
    [CreateAssetMenu(fileName = "BoardConfig", menuName = "Match3/Board Config")]
    public sealed class BoardConfig : ScriptableObject
    {
        [Header("Grid")]
        [SerializeField] private int _width = 8;
        [SerializeField] private int _height = 8;
        [SerializeField] private float _cellSize = 1f;

        [Header("Placement")]
        [Tooltip("Extra world-space shift applied on top of automatic centering. (0,0) keeps the board centered on the BoardView GameObject's position.")]
        [SerializeField] private Vector2 _boardOffset = Vector2.zero;

        [Header("Tile Types")]
        [SerializeField] private List<TileTypeData> _tileTypes = new List<TileTypeData>();

        [Header("Matching")]
        [SerializeField] [Range(3, 5)] private int _minMatchLength = 3;

        [Header("Timing")]
        [SerializeField] private float _swapDuration = 0.25f;
        [SerializeField] private float _fallDuration = 0.3f;
        [SerializeField] private float _spawnDuration = 0.25f;
        [SerializeField] private float _destroyDuration = 0.2f;

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public Vector2 BoardOffset => _boardOffset;
        public IReadOnlyList<TileTypeData> TileTypes => _tileTypes;
        public int MinMatchLength => _minMatchLength;
        public float SwapDuration => _swapDuration;
        public float FallDuration => _fallDuration;
        public float SpawnDuration => _spawnDuration;
        public float DestroyDuration => _destroyDuration;

        /// <summary>
        /// Looks up the visual data for a tile type. The typeId is simply
        /// the index inside <see cref="TileTypes"/>, so there is no separate
        /// id field to keep in sync.
        /// </summary>
        public TileTypeData GetTileTypeData(int typeId)
        {
            if (typeId < 0 || typeId >= _tileTypes.Count)
            {
                return null;
            }
            return _tileTypes[typeId];
        }
    }
}
