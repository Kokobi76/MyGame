using System.Collections.Generic;
using Match3.Data;

namespace Match3.Generation
{
    /// <summary>
    /// Single source of truth for picking a random tile TypeId. Shared by
    /// <see cref="BoardGenerator"/> (initial generation, with exclusions
    /// to avoid pre-existing matches) and
    /// <see cref="Match3.Collapse.CollapseResolver"/> (refills, with no
    /// exclusions so natural cascades can still happen).
    /// </summary>
    public sealed class TileTypeRandomizer
    {
        public int GetRandomTypeId(BoardConfig config, ISet<int> excludedTypeIds = null)
        {
            int typeCount = config.TileTypes.Count;

            if (excludedTypeIds == null || excludedTypeIds.Count == 0)
            {
                return UnityEngine.Random.Range(0, typeCount);
            }

            List<int> candidateTypeIds = new List<int>();
            for (int typeId = 0; typeId < typeCount; typeId++)
            {
                if (!excludedTypeIds.Contains(typeId))
                {
                    candidateTypeIds.Add(typeId);
                }
            }

            // Not enough tile variety to honor every exclusion: fall back
            // to an unrestricted pick rather than looping forever.
            if (candidateTypeIds.Count == 0)
            {
                return UnityEngine.Random.Range(0, typeCount);
            }

            return candidateTypeIds[UnityEngine.Random.Range(0, candidateTypeIds.Count)];
        }
    }
}
