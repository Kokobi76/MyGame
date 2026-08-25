namespace Match3.Model
{
    /// <summary>
    /// Immutable data for a single tile. Deliberately plain C# (no
    /// MonoBehaviour, no position) so the board state can be created,
    /// copied and reasoned about without touching the scene.
    /// A tile's grid position is defined by its slot in <see cref="BoardModel"/>,
    /// not stored on the tile itself.
    /// </summary>
    public sealed class Tile
    {
        public int TypeId { get; }

        public Tile(int typeId)
        {
            TypeId = typeId;
        }
    }
}
