using UnityEngine;

namespace Match3.Battle.Skills
{
    /// <summary>
    /// Tiny shared helper for rolling a skill's configured object-count
    /// ranges (Objects Attack Opponent / Objects Attack Board Min-Max).
    /// Kept separate from <see cref="SkillDefinition"/> itself so the
    /// definition stays pure data, the same way
    /// <see cref="Match3.Generation.TileTypeRandomizer"/> is kept
    /// separate from <see cref="Match3.Data.BoardConfig"/>.
    /// </summary>
    public static class SkillRandom
    {
        /// <summary>Rolls a random count in [min, max], inclusive — safe even if Min/Max were swapped in the Inspector.</summary>
        public static int RollObjectCount(int min, int max)
        {
            int lower = Mathf.Min(min, max);
            int upper = Mathf.Max(min, max);
            return UnityEngine.Random.Range(lower, upper + 1);
        }
    }
}
