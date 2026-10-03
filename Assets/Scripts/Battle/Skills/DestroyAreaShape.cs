namespace Match3.Battle.Skills
{
    /// <summary>
    /// The Destroy Area shapes from the design doc's Tiles Skill row.
    /// <see cref="Block"/> replaces what used to be separate
    /// Single1x1/Block2x2/Block3x3 entries — its size (1-7) is
    /// <see cref="SkillDefinition.BlockSize"/> instead, so any N x N can
    /// be picked without growing this enum. Each shape anchors at one
    /// board cell (picked randomly among currently filled cells, never
    /// the same cell twice within one cast) and expands from there,
    /// centered on that cell — see <see cref="DestroyAreaResolver"/>.
    /// </summary>
    public enum DestroyAreaShape
    {
        Block,
        Row,
        Column,
        Special,
        All
    }
}
