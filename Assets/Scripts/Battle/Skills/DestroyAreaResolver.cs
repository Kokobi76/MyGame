using System.Collections.Generic;
using UnityEngine;
using Match3.Model;

namespace Match3.Battle.Skills
{
    /// <summary>
    /// Turns a Tiles Skill's Destroy Area configuration into the actual
    /// set of board cells it destroys. Pure board-geometry — knows
    /// nothing about tile contents beyond what's needed to pick anchors
    /// (a destroy effect only makes sense anchored on a filled cell) and
    /// stay in bounds.
    /// </summary>
    public static class DestroyAreaResolver
    {
        /// <summary>
        /// Rolls the skill's configured Objects-Attack-Board count, then
        /// unions however many Destroy Area applications that many
        /// objects produce. Each application anchors at a distinct
        /// randomly-picked currently-filled cell — no two objects in the
        /// same cast ever anchor at the same cell — but the AREAS two
        /// different anchors cover can still end up adjacent or
        /// overlapping (e.g. two 2x2 blocks touching), in which case they
        /// simply merge into one bigger destroyed region, since the
        /// result is a set. Returns an empty set if the roll is 0 or the
        /// board has no filled cells; if there are fewer filled cells
        /// than the rolled object count, every filled cell is used
        /// exactly once rather than looping.
        /// </summary>
        public static HashSet<Vector2Int> ResolveBoardDestroyPositions(SkillDefinition skill, IReadOnlyBoardModel board)
        {
            HashSet<Vector2Int> positions = new HashSet<Vector2Int>();

            int objectCount = SkillRandom.RollObjectCount(skill.ObjectsAttackBoardMin, skill.ObjectsAttackBoardMax);
            if (objectCount <= 0)
            {
                return positions;
            }

            List<Vector2Int> availableAnchors = GetFilledCells(board);
            int anchorCount = Mathf.Min(objectCount, availableAnchors.Count);

            for (int i = 0; i < anchorCount; i++)
            {
                int pickIndex = UnityEngine.Random.Range(0, availableAnchors.Count);
                Vector2Int anchor = availableAnchors[pickIndex];
                availableAnchors.RemoveAt(pickIndex); // never pick the same anchor twice in one cast

                AddCells(positions, skill.DestroyArea, anchor, skill.SpecialShapeOffsets, board.Width, board.Height);
            }

            return positions;
        }

        /// <summary>Expands one shape application anchored at <paramref name="anchor"/> into <paramref name="destination"/>, clamped to the board bounds.</summary>
        public static void AddCells(HashSet<Vector2Int> destination, DestroyAreaShape shape, Vector2Int anchor, IReadOnlyList<Vector2Int> specialOffsets, int boardWidth, int boardHeight)
        {
            switch (shape)
            {
                case DestroyAreaShape.Single1x1:
                    AddIfInBounds(destination, anchor, boardWidth, boardHeight);
                    break;
                case DestroyAreaShape.Block2x2:
                    AddBlock(destination, anchor, 2, boardWidth, boardHeight);
                    break;
                case DestroyAreaShape.Block3x3:
                    AddBlock(destination, anchor, 3, boardWidth, boardHeight);
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

        /// <summary>Anchor is the block's bottom-left cell, matching the board's own bottom-left-origin coordinate system (see BoardView).</summary>
        private static void AddBlock(HashSet<Vector2Int> destination, Vector2Int anchor, int size, int boardWidth, int boardHeight)
        {
            for (int dx = 0; dx < size; dx++)
            {
                for (int dy = 0; dy < size; dy++)
                {
                    AddIfInBounds(destination, anchor + new Vector2Int(dx, dy), boardWidth, boardHeight);
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

        private static List<Vector2Int> GetFilledCells(IReadOnlyBoardModel board)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (!board.IsEmpty(position))
                    {
                        cells.Add(position);
                    }
                }
            }
            return cells;
        }
    }
}
