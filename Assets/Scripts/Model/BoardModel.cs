using UnityEngine;

namespace Match3.Model
{
    /// <summary>
    /// Pure data model of the match-3 grid. Holds a 2D array of
    /// <see cref="Tile"/> references and exposes only basic grid
    /// operations (read, write, swap, bounds check). It has no notion of
    /// matching rules, animation or input; those are separate, single
    /// responsibility services that operate on this model.
    /// An empty cell is represented by a null <see cref="Tile"/>.
    /// </summary>
    public sealed class BoardModel : IReadOnlyBoardModel
    {
        private readonly Tile[,] _tiles;

        public int Width { get; }
        public int Height { get; }

        public BoardModel(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new Tile[width, height];
        }

        public bool IsWithinBounds(Vector2Int position)
        {
            return IsWithinBounds(position.x, position.y);
        }

        public bool IsWithinBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public Tile GetTile(Vector2Int position)
        {
            return GetTile(position.x, position.y);
        }

        public Tile GetTile(int x, int y)
        {
            return IsWithinBounds(x, y) ? _tiles[x, y] : null;
        }

        public void SetTile(Vector2Int position, Tile tile)
        {
            SetTile(position.x, position.y, tile);
        }

        public void SetTile(int x, int y, Tile tile)
        {
            if (!IsWithinBounds(x, y))
            {
                return;
            }
            _tiles[x, y] = tile;
        }

        public bool IsEmpty(Vector2Int position)
        {
            return GetTile(position) == null;
        }

        /// <summary>
        /// Swaps whichever tiles currently occupy the two positions.
        /// Calling this twice with the same arguments restores the
        /// original state, which the gameplay layer relies on to
        /// cheaply "try" and revert an invalid player swap.
        /// </summary>
        public void SwapTiles(Vector2Int a, Vector2Int b)
        {
            Tile tileA = GetTile(a);
            Tile tileB = GetTile(b);
            SetTile(a, tileB);
            SetTile(b, tileA);
        }
    }
}
