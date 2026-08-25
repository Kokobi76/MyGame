using System.Collections.Generic;
using UnityEngine;
using Match3.Data;
using Match3.Model;
using Match3.Generation;

namespace Match3.Collapse
{
    /// <summary>
    /// Default <see cref="ICollapseResolver"/> implementation. Processes
    /// each column independently: compact existing tiles downward, then
    /// fill whatever is left empty at the top with fresh random tiles.
    /// Refill deliberately does not exclude any type, unlike initial
    /// generation — new cascades are the point of a match-3 board.
    /// </summary>
    public sealed class CollapseResolver : ICollapseResolver
    {
        private readonly TileTypeRandomizer _randomizer;

        public CollapseResolver(TileTypeRandomizer randomizer)
        {
            _randomizer = randomizer;
        }

        public CollapseResult ResolveCollapseAndRefill(BoardModel board, BoardConfig config)
        {
            List<TileMovement> movements = new List<TileMovement>();
            List<TileSpawn> spawns = new List<TileSpawn>();

            for (int x = 0; x < board.Width; x++)
            {
                CollapseColumn(board, x, movements);
                RefillColumn(board, config, x, spawns);
            }

            return new CollapseResult(movements, spawns);
        }

        /// <summary>Compacts non-empty tiles in a column toward row 0, preserving their relative order.</summary>
        private void CollapseColumn(BoardModel board, int x, List<TileMovement> movements)
        {
            int writeY = 0;
            for (int readY = 0; readY < board.Height; readY++)
            {
                Tile tile = board.GetTile(x, readY);
                if (tile == null)
                {
                    continue;
                }

                if (readY != writeY)
                {
                    board.SetTile(x, readY, null);
                    board.SetTile(x, writeY, tile);
                    movements.Add(new TileMovement(new Vector2Int(x, readY), new Vector2Int(x, writeY)));
                }

                writeY++;
            }
        }

        /// <summary>
        /// After a collapse, every empty cell in a column is guaranteed to
        /// be a contiguous block at the top. Spawning all of them the same
        /// <paramref name="board"/>-height's worth of rows above the stack
        /// makes them fall together as one queue, at equal speed, without
        /// visually overtaking each other.
        /// </summary>
        private void RefillColumn(BoardModel board, BoardConfig config, int x, List<TileSpawn> spawns)
        {
            List<int> emptyRows = new List<int>();
            for (int y = 0; y < board.Height; y++)
            {
                if (board.IsEmpty(new Vector2Int(x, y)))
                {
                    emptyRows.Add(y);
                }
            }

            int emptyCount = emptyRows.Count;
            for (int i = 0; i < emptyCount; i++)
            {
                int y = emptyRows[i];
                int typeId = _randomizer.GetRandomTypeId(config);
                Tile newTile = new Tile(typeId);

                board.SetTile(x, y, newTile);
                spawns.Add(new TileSpawn(new Vector2Int(x, y), newTile, emptyCount));
            }
        }
    }
}
