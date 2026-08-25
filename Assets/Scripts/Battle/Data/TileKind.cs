namespace Match3.Battle.Data
{
    /// <summary>
    /// Gameplay meaning of a matched tile, independent of its visual
    /// TypeId on the board. Mapped from TypeId via <see cref="BattleTileConfig"/>.
    /// </summary>
    public enum TileKind
    {
        Hp,
        Vhp,
        Mana,
        Sword,
        Slash,
        Shield
    }
}
