namespace Match3.Battle.Model
{
    public enum BattleSide
    {
        Player,
        Enemy
    }

    public static class BattleSideExtensions
    {
        public static BattleSide GetOpposite(this BattleSide side)
        {
            return side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;
        }
    }
}
