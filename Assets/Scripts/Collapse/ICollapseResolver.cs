using Match3.Data;
using Match3.Model;

namespace Match3.Collapse
{
    /// <summary>
    /// After matched tiles are cleared (set to null) on the model, this
    /// service drops the remaining tiles down to fill the gaps and spawns
    /// new random tiles for whatever is still empty at the top. It
    /// mutates the board to its final resting state immediately and
    /// returns the movement/spawn deltas so the view layer can animate
    /// what just happened.
    /// </summary>
    public interface ICollapseResolver
    {
        CollapseResult ResolveCollapseAndRefill(BoardModel board, BoardConfig config);
    }
}
