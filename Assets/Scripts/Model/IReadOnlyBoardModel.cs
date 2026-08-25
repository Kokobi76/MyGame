using UnityEngine;

namespace Match3.Model
{
    /// <summary>
    /// Read-only view of a <see cref="BoardModel"/>: everything external
    /// systems (AI move selection, UI, debug tools) need to inspect board
    /// state, without exposing <see cref="BoardModel.SetTile"/> or
    /// <see cref="BoardModel.SwapTiles"/>. <see cref="BoardModel"/>
    /// implements this in addition to its full mutable API, which stays
    /// internal to the gameplay layer.
    /// </summary>
    public interface IReadOnlyBoardModel
    {
        int Width { get; }
        int Height { get; }
        bool IsWithinBounds(Vector2Int position);
        bool IsWithinBounds(int x, int y);
        Tile GetTile(Vector2Int position);
        Tile GetTile(int x, int y);
        bool IsEmpty(Vector2Int position);
    }
}
