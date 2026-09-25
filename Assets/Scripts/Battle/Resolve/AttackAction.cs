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

        /// <summary>
        /// True for Skill-sourced damage — per the design doc, "Shield
        /// không có tác dụng chặn skill" (Shield has no effect blocking
        /// skills). Defaults to false, so every tile-triggered Slash/Sword
        /// attack (built the original way, via the constructor below and
        /// never touching this property) behaves exactly as before this
        /// field existed. Only <see cref="Match3.Battle.Skills.SkillDamageResolver"/>
        /// sets it to true, right after construction — a settable
        /// property rather than a constructor parameter specifically so
        /// the constructor itself never had to change.
        /// </summary>
        public bool BypassesShield { get; set; }

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
