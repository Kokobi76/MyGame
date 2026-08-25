using UnityEngine;

namespace Match3.Data
{
    /// <summary>
    /// Visual data for a single tile type (color/gem variant).
    /// The tile's numeric TypeId is implicit: it is the index of this
    /// asset inside <see cref="BoardConfig.TileTypes"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "TileTypeData", menuName = "Match3/Tile Type Data")]
    public sealed class TileTypeData : ScriptableObject
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _color = Color.white;

        public Sprite Sprite => _sprite;
        public Color Color => _color;
    }
}
