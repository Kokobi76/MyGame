namespace Match3.Battle.Skills
{
    /// <summary>
    /// The Destroy Area shapes from the design doc's Tiles Skill row.
    /// Each anchors at one board cell (picked randomly among currently
    /// filled cells, never the same cell twice within one cast — see
    /// <see cref="DestroyAreaResolver"/>) and expands from there.
    /// </summary>
    public enum DestroyAreaShape
    {
        Single1x1,
        Block2x2,
        Block3x3,
        Row,
        Column,
        Special,
        All
    }
}
