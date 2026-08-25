using Match3.Battle.Data;
using Match3.Battle.Model;

namespace Match3.Battle.Resolve
{
    public enum AttackKind
    {
        Slash,
        Sword
    }

    /// <summary>
    /// A queued attack still waiting to be played out visually. No damage
    /// has been applied to any <see cref="CharacterState"/> yet — that
    /// only happens once <see cref="Match3.Battle.Gameplay.BattleController"/>
    /// plays this attack's visual (via <see cref="Match3.Battle.View.AttackVisualController"/>)
    /// and it actually connects with the defender.
    /// </summary>
    public sealed class AttackAction
    {
        public BattleSide AttackerSide { get; }
        public AttackKind Kind { get; }
        public AttackType AttackType { get; }
        public float TotalDamage { get; }
        public int ObjectCount { get; }

        public AttackAction(BattleSide attackerSide, AttackKind kind, AttackType attackType, float totalDamage, int objectCount)
        {
            AttackerSide = attackerSide;
            Kind = kind;
            AttackType = attackType;
            TotalDamage = totalDamage;
            ObjectCount = objectCount;
        }
    }
}
