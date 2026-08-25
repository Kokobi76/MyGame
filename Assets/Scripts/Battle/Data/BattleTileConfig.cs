using System.Collections.Generic;
using UnityEngine;
using Match3.Data;

namespace Match3.Battle.Data
{
    /// <summary>
    /// One tile type's gameplay meaning. Kept as a small serializable
    /// entry (referencing the existing <see cref="TileTypeData"/> asset)
    /// rather than a positional index, so reordering
    /// <see cref="BoardConfig.TileTypes"/> in the Inspector can't
    /// silently desync the mapping.
    /// </summary>
    [System.Serializable]
    public sealed class TileKindEntry
    {
        [SerializeField] private TileTypeData _tileType;
        [SerializeField] private TileKind _kind;

        public TileTypeData TileType => _tileType;
        public TileKind Kind => _kind;
    }

    /// <summary>
    /// Assigns a <see cref="TileKind"/> to every tile type used by a
    /// <see cref="BoardConfig"/>. Lives entirely outside
    /// <c>Match3.Data</c>/<c>Match3.Model</c> so the core board package
    /// stays generic — it has no idea "HP tile" or "Sword tile" exist.
    /// </summary>
    [CreateAssetMenu(fileName = "BattleTileConfig", menuName = "Match3/Battle/Battle Tile Config")]
    public sealed class BattleTileConfig : ScriptableObject
    {
        [SerializeField] private BoardConfig _boardConfig;
        [SerializeField] private List<TileKindEntry> _tileKinds = new List<TileKindEntry>();

        private Dictionary<int, TileKind> _kindByTypeId;

        public TileKind GetKind(int typeId)
        {
            EnsureLookupBuilt();
            return _kindByTypeId.TryGetValue(typeId, out TileKind kind) ? kind : default;
        }

        private void OnEnable()
        {
            _kindByTypeId = null;
        }

        private void EnsureLookupBuilt()
        {
            if (_kindByTypeId != null)
            {
                return;
            }

            _kindByTypeId = new Dictionary<int, TileKind>();
            if (_boardConfig == null)
            {
                Debug.LogError("BattleTileConfig requires a BoardConfig reference.", this);
                return;
            }

            for (int typeId = 0; typeId < _boardConfig.TileTypes.Count; typeId++)
            {
                TileTypeData tileType = _boardConfig.TileTypes[typeId];
                TileKindEntry entry = _tileKinds.Find(e => e.TileType == tileType);
                if (entry == null)
                {
                    Debug.LogWarning($"BattleTileConfig has no TileKind mapped for tile type index {typeId}.", this);
                    continue;
                }
                _kindByTypeId[typeId] = entry.Kind;
            }
        }
    }
}
