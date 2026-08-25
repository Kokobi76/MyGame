using System.Collections.Generic;
using UnityEngine;
using Match3.Model;

namespace Match3.Collapse
{
    /// <summary>An existing tile sliding from one cell down to another within the same column.</summary>
    public readonly struct TileMovement
    {
        public Vector2Int FromPosition { get; }
        public Vector2Int ToPosition { get; }

        public TileMovement(Vector2Int fromPosition, Vector2Int toPosition)
        {
            FromPosition = fromPosition;
            ToPosition = toPosition;
        }
    }

    /// <summary>A brand new tile created to refill an empty cell, plus how many rows above the board it should fall from.</summary>
    public readonly struct TileSpawn
    {
        public Vector2Int ToPosition { get; }
        public Tile Tile { get; }
        public int DropRowCount { get; }

        public TileSpawn(Vector2Int toPosition, Tile tile, int dropRowCount)
        {
            ToPosition = toPosition;
            Tile = tile;
            DropRowCount = dropRowCount;
        }
    }

    /// <summary>
    /// The full set of visual deltas produced by one collapse-and-refill
    /// pass, consumed by <see cref="Match3.View.BoardView.AnimateCollapseAndRefill"/>.
    /// </summary>
    public sealed class CollapseResult
    {
        public IReadOnlyList<TileMovement> Movements { get; }
        public IReadOnlyList<TileSpawn> Spawns { get; }

        public CollapseResult(IReadOnlyList<TileMovement> movements, IReadOnlyList<TileSpawn> spawns)
        {
            Movements = movements;
            Spawns = spawns;
        }
    }
}
