using System.Collections.Generic;
using UnityEngine;

namespace Match3.Matching
{
    public enum MatchOrientation
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// One contiguous run of same-type tiles that satisfies the minimum
    /// match length (e.g. three-in-a-row). A single resolve step can
    /// contain several of these, including overlapping L/T shapes.
    /// </summary>
    public sealed class MatchGroup
    {
        public IReadOnlyList<Vector2Int> Positions { get; }
        public int TypeId { get; }
        public MatchOrientation Orientation { get; }

        public MatchGroup(IReadOnlyList<Vector2Int> positions, int typeId, MatchOrientation orientation)
        {
            Positions = positions;
            TypeId = typeId;
            Orientation = orientation;
        }
    }
}
