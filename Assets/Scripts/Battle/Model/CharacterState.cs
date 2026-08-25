using UnityEngine;
using Match3.Battle.Data;

namespace Match3.Battle.Model
{
    /// <summary>
    /// Mutable runtime stats for one character, derived from a
    /// <see cref="CharacterConfig"/>. Pure C#, mutated exclusively by
    /// <see cref="Match3.Battle.Resolve.BattleResolveProcessor"/> so all
    /// stat rules live in one place.
    ///
    /// VHP and Mana currently have no consumer in the design doc beyond
    /// accumulating from matched tiles (no damage mitigation, no spend);
    /// they're modeled here as plain capped/uncapped accumulators ready
    /// for whatever future ability system uses them.
    /// </summary>
    public sealed class CharacterState
    {
        private readonly CharacterConfig _config;

        public CharacterConfig Config => _config;
        public float CurrentHp { get; private set; }
        public float CurrentVhp { get; private set; }
        public float Mana { get; private set; }
        public int ShieldCount { get; private set; }
        public int ShieldStack { get; private set; }

        public bool IsDefeated => CurrentHp <= 0f;

        public CharacterState(CharacterConfig config)
        {
            _config = config;
            CurrentHp = config.MaxHp;
            CurrentVhp = 0f;
            Mana = 0f;
            ShieldCount = 0;
            ShieldStack = 0;
        }

        public void HealHp(float amount)
        {
            CurrentHp = Mathf.Clamp(CurrentHp + amount, 0f, _config.MaxHp);
        }

        public void HealVhp(float amount)
        {
            CurrentVhp = Mathf.Clamp(CurrentVhp + amount, 0f, _config.MaxVhp);
        }

        public void AddMana(float amount)
        {
            Mana = Mathf.Clamp(Mana + amount, 0f, _config.MaxMana);
        }

        /// <summary>Adds to Shield Count, then converts every full multiple of <paramref name="shieldCountPerStack"/> into a Shield Stack, keeping the remainder.</summary>
        public void AddShieldCount(int amount, int shieldCountPerStack)
        {
            ShieldCount += amount;

            int newStacks = ShieldCount / shieldCountPerStack;
            if (newStacks <= 0)
            {
                return;
            }

            ShieldStack += newStacks;
            ShieldCount -= newStacks * shieldCountPerStack;
        }

        /// <summary>
        /// Applies incoming damage, first consuming one Shield Stack (if
        /// any) to fully negate it instead. Returns the amount actually
        /// removed from HP (0 when the hit was blocked).
        /// </summary>
        public float TakeDamage(float rawDamage)
        {
            if (ShieldStack > 0)
            {
                ShieldStack--;
                return 0f;
            }

            float appliedDamage = Mathf.Max(0f, rawDamage);
            CurrentHp = Mathf.Clamp(CurrentHp - appliedDamage, 0f, _config.MaxHp);
            return appliedDamage;
        }

        /// <summary>
        /// Applies damage that bypasses Shield Stack entirely. Used for
        /// the move-timeout penalty, which reuses Swordrain/Slash as its
        /// damage amount but doesn't arise from an actual Slash/Sword
        /// tile resolve — the design doc's Shield Stack rule specifically
        /// covers damage "from Slash and Sword", not this penalty.
        /// </summary>
        public float TakeUnblockableDamage(float rawDamage)
        {
            float appliedDamage = Mathf.Max(0f, rawDamage);
            CurrentHp = Mathf.Clamp(CurrentHp - appliedDamage, 0f, _config.MaxHp);
            return appliedDamage;
        }
    }
}
