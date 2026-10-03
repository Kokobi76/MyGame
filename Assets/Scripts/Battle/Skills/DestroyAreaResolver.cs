using System.Collections.Generic;
using UnityEngine;
using Match3.Model;

namespace Match3.Battle.Skills
{
    /// <summary>
    /// One resolved Tiles Skill board-destroy: the anchor cell used for
    /// each rolled board-object (for the visual — one projectile flies to
    /// each anchor, see <see cref="Match3.Battle.View.AttackVisualController.PlayBoardDestroyAttack"/>)
    /// and the final, de-duplicated set of cells all of them destroy
    /// together (for the actual board mutation, see
    /// <see cref="Match3.Gameplay.BoardController.DestroyPositionsRoutine"/>).
    /// </summary>
    public readonly struct TilesSkillDestroyPlan
    {
        public IReadOnlyList<Vector2Int> Anchors { get; }
        public IReadOnlyCollection<Vector2Int> Positions { get; }

        public TilesSkillDestroyPlan(IReadOnlyList<Vector2Int> anchors, IReadOnlyCollection<Vector2Int> positions)
        {
            Anchors = anchors;
            Positions = positions;
        }
    }

    /// <summary>
    /// Turns a Tiles Skill's Destroy Area configuration into an actual
    /// destroy plan. Pure board-geometry — knows nothing about tile
    /// contents beyond what's needed to pick anchors (a destroy effect
    /// only makes sense anchored on a filled cell) and guarantee the full
    /// configured shape always fits on the board, CENTERED on the chosen
    /// anchor rather than cornered on it.
    ///
    /// Centering convention for <see cref="DestroyAreaShape.Block"/> (see
    /// <see cref="SkillDefinition.BlockSize"/> for its size, 1-7): an ODD
    /// size centers exactly on the anchor tile — the anchor IS the middle
    /// tile (e.g. a 3x3 spans exactly 1 tile in every direction from it).
    /// An EVEN size has no single center tile, so it's built as
    /// <c>(size-1)/2</c> tiles below/left of the anchor and the rest
    /// above/right of it (e.g. a 2x2 is just the anchor and its up-right
    /// neighbor) — the destroyed cells still form a full N x N block, but
    /// the true geometric center sits half a cell up-and-right of the
    /// anchor; the visual target for these is nudged there instead of
    /// landing on the anchor tile itself (see
    /// <see cref="SkillDefinition.NeedsEvenBlockCenterOffset"/> and
    /// <see cref="Match3.Battle.View.AttackVisualController.PlayBoardDestroyAttack"/>).
    /// Row/Column/All are never affected by centering (they always span
    /// their full board dimension regardless of anchor); Special treats
    /// the anchor as whatever the designer's own offset list considers
    /// its center (offset (0,0) is the anchor itself).
    /// </summary>
    public static class DestroyAreaResolver
    {
        /// <summary>
        /// Rolls the skill's configured Objects-Attack-Board count, then
        /// picks that many DISTINCT anchors — each one guaranteed to fit
        /// the FULL configured shape on the board, centered on it (see
        /// class remarks) — and unions every application's cells into one
        /// destroy set. Adjacent/overlapping applications simply merge,
        /// since the result is a set. Returns an empty plan if the roll is
        /// 0 or no cell can host the shape at all; if there are fewer
        /// valid anchors than the rolled count, every valid anchor is
        /// used exactly once rather than looping.
        /// </summary>
        public static TilesSkillDestroyPlan ResolveDestroyPlan(SkillDefinition skill, IReadOnlyBoardModel board)
        {
            List<Vector2Int> anchors = new List<Vector2Int>();
            HashSet<Vector2Int> positions = new HashSet<Vector2Int>();

            int objectCount = SkillRandom.RollObjectCount(skill.ObjectsAttackBoardMin, skill.ObjectsAttackBoardMax);
            if (objectCount <= 0)
            {
                return new TilesSkillDestroyPlan(anchors, positions);
            }

            List<Vector2Int> availableAnchors = GetValidAnchors(board, skill.DestroyArea, skill.BlockSize, skill.SpecialShapeOffsets);
            int anchorCount = Mathf.Min(objectCount, availableAnchors.Count);

            for (int i = 0; i < anchorCount; i++)
            {
                int pickIndex = UnityEngine.Random.Range(0, availableAnchors.Count);
                Vector2Int anchor = availableAnchors[pickIndex];
                availableAnchors.RemoveAt(pickIndex); // never pick the same anchor twice in one cast

                anchors.Add(anchor);
                AddCells(positions, skill.DestroyArea, skill.BlockSize, anchor, skill.SpecialShapeOffsets, board.Width, board.Height);
            }

            return new TilesSkillDestroyPlan(anchors, positions);
        }

        /// <summary>Expands one shape application anchored/centered at <paramref name="anchor"/> into <paramref name="destination"/>. Anchors always come from <see cref="GetValidAnchors"/>, which already guarantees the shape fits, but bounds are still checked here as a defensive no-op.</summary>
        public static void AddCells(HashSet<Vector2Int> destination, DestroyAreaShape shape, int blockSize, Vector2Int anchor, IReadOnlyList<Vector2Int> specialOffsets, int boardWidth, int boardHeight)
        {
            switch (shape)
            {
                case DestroyAreaShape.Block:
                    AddBlock(destination, anchor, blockSize, boardWidth, boardHeight);
                    break;
                case DestroyAreaShape.Row:
                    for (int x = 0; x < boardWidth; x++)
                    {
                        AddIfInBounds(destination, new Vector2Int(x, anchor.y), boardWidth, boardHeight);
                    }
                    break;
                case DestroyAreaShape.Column:
                    for (int y = 0; y < boardHeight; y++)
                    {
                        AddIfInBounds(destination, new Vector2Int(anchor.x, y), boardWidth, boardHeight);
                    }
                    break;
                case DestroyAreaShape.Special:
                    if (specialOffsets != null)
                    {
                        foreach (Vector2Int offset in specialOffsets)
                        {
                            AddIfInBounds(destination, anchor + offset, boardWidth, boardHeight);
                        }
                    }
                    break;
                case DestroyAreaShape.All:
                    for (int x = 0; x < boardWidth; x++)
                    {
                        for (int y = 0; y < boardHeight; y++)
                        {
                            destination.Add(new Vector2Int(x, y));
                        }
                    }
                    break;
            }
        }

        /// <summary>How many cells a Block extends toward the LOWER coordinate on each axis from the anchor. This single formula centers both odd sizes (anchor = the exact middle tile) and even sizes (anchor = the lower-left of the central 4) — see the class remarks.</summary>
        private static int GetLowerExtent(int size)
        {
            return (size - 1) / 2;
        }

        private static void AddBlock(HashSet<Vector2Int> destination, Vector2Int anchor, int size, int boardWidth, int boardHeight)
        {
            int lower = GetLowerExtent(size);
            for (int dx = 0; dx < size; dx++)
            {
                for (int dy = 0; dy < size; dy++)
                {
                    Vector2Int cell = new Vector2Int(anchor.x - lower + dx, anchor.y - lower + dy);
                    AddIfInBounds(destination, cell, boardWidth, boardHeight);
                }
            }
        }

        private static void AddIfInBounds(HashSet<Vector2Int> destination, Vector2Int position, int boardWidth, int boardHeight)
        {
            if (position.x >= 0 && position.x < boardWidth && position.y >= 0 && position.y < boardHeight)
            {
                destination.Add(position);
            }
        }

        /// <summary>Every currently-filled cell from which the given shape would fit ENTIRELY on the board, centered on it — the pool anchors are picked from, so a rolled shape is never silently clipped at an edge. Row/Column/All are never clipped by anchor choice at all (a Row/Column always spans the full board dimension regardless of which cell anchors it), so every filled cell qualifies for those.</summary>
        private static List<Vector2Int> GetValidAnchors(IReadOnlyBoardModel board, DestroyAreaShape shape, int blockSize, IReadOnlyList<Vector2Int> specialOffsets)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (board.IsEmpty(position))
                    {
                        continue;
                    }
                    if (ShapeFullyFits(shape, blockSize, position, specialOffsets, board.Width, board.Height))
                    {
                        cells.Add(position);
                    }
                }
            }
            return cells;
        }

        private static bool ShapeFullyFits(DestroyAreaShape shape, int blockSize, Vector2Int anchor, IReadOnlyList<Vector2Int> specialOffsets, int boardWidth, int boardHeight)
        {
            switch (shape)
            {
                case DestroyAreaShape.Block:
                    return FitsBlock(anchor, blockSize, boardWidth, boardHeight);
                case DestroyAreaShape.Special:
                    return FitsSpecial(anchor, specialOffsets, boardWidth, boardHeight);
                case DestroyAreaShape.Row:
                case DestroyAreaShape.Column:
                case DestroyAreaShape.All:
                default:
                    return true;
            }
        }

        private static bool FitsBlock(Vector2Int anchor, int size, int boardWidth, int boardHeight)
        {
            int lower = GetLowerExtent(size);
            int minX = anchor.x - lower;
            int minY = anchor.y - lower;
            int maxX = minX + size - 1;
            int maxY = minY + size - 1;
            return minX >= 0 && minY >= 0 && maxX < boardWidth && maxY < boardHeight;
        }

        private static bool FitsSpecial(Vector2Int anchor, IReadOnlyList<Vector2Int> specialOffsets, int boardWidth, int boardHeight)
        {
            if (specialOffsets == null || specialOffsets.Count == 0)
            {
                return true;
            }
            foreach (Vector2Int offset in specialOffsets)
            {
                Vector2Int cell = anchor + offset;
                if (cell.x < 0 || cell.x >= boardWidth || cell.y < 0 || cell.y >= boardHeight)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
