using UnityEngine;

namespace Match3.Battle.Resolve
{
    /// <summary>
    /// Every Tile Resolve formula in the design doc involves a percentage
    /// or a split across several objects, either of which can produce a
    /// fractional result (e.g. 0.3 HP). Rounds any such result to a whole
    /// number with a floor of 1: a resolved match should always apply and
    /// display as a meaningful whole-number effect, never an invisible
    /// fraction of a point or a confusing "0".
    /// </summary>
    public static class ResolveMath
    {
        public static int RoundToMeaningfulAmount(float rawValue)
        {
            return Mathf.Max(Mathf.RoundToInt(rawValue), 1);
        }
    }
}
