using System.Collections.Generic;

namespace Match3.Battle.Resolve
{
    /// <summary>
    /// Everything one resolve pass (a single match-and-cascade step) did.
    /// Healing/Mana/Shield are already-applied instant effects; Slash and
    /// Sword damage is NOT applied yet — each is described as an
    /// <see cref="AttackAction"/> for the caller to play out visually and
    /// apply once it connects (see <see cref="BattleResolveProcessor"/>).
    /// </summary>
    public sealed class BattleResolveOutcome
    {
        private readonly List<AttackAction> _attacks = new List<AttackAction>();

        public float HpHealed { get; private set; }
        public float VhpHealed { get; private set; }
        public float ManaGained { get; private set; }
        public int ShieldCountGained { get; private set; }
        public int ShieldStacksGained { get; private set; }
        public bool GrantsExtraMove { get; private set; }
        public IReadOnlyList<AttackAction> Attacks => _attacks;

        public void AddHpHealed(float amount) => HpHealed += amount;
        public void AddVhpHealed(float amount) => VhpHealed += amount;
        public void AddManaGained(float amount) => ManaGained += amount;
        public void AddShieldCountGained(int amount) => ShieldCountGained += amount;
        public void AddShieldStacksGained(int amount) => ShieldStacksGained += amount;
        public void AddAttack(AttackAction attack) => _attacks.Add(attack);
        public void MarkGrantsExtraMove() => GrantsExtraMove = true;
    }
}
