using UnityEngine;
using Match3.Battle.Data;
using Match3.Battle.Buffs;

namespace Match3.Battle.Model
{
    /// <summary>
    /// Runtime state for one character. Main Stats (MaxHp, MaxVhp,
    /// Attack, MagicAttack) are derived once, at construction, from the
    /// character's Primal Stats via <see cref="StatDerivationConfig"/>.
    /// SlashDamage/SwordrainDamage are ALSO derived once as a base value,
    /// but read through <see cref="Buffs"/> on every access, so any
    /// active Buff/Debuff modifier is automatically reflected everywhere
    /// (damage calculation, UI, and Skill damage via
    /// <see cref="Match3.Battle.Skills.SkillDamageResolver"/>) without
    /// those callers needing to know buffs exist.
    ///
    /// Battle Stats (CurrentHp, CurrentVhp, Mana, ShieldCount,
    /// ShieldStack) are mutable and change throughout the battle.
    /// Healing respects the Recovery Block control buff; damage respects
    /// Invincible.
    ///
    /// VHP currently has no consumer in the design doc beyond
    /// accumulating from matched tiles (no damage mitigation, no spend);
    /// Mana is now spent by Skill casts (see <see cref="TrySpendMana"/>)
    /// but otherwise still only accumulates from matched tiles — both are
    /// modeled here as plain capped accumulators ready for whatever
    /// future ability system extends them further.
    /// </summary>
    public sealed class CharacterState
    {
        private readonly CharacterConfig _config;
        private readonly float _baseSlashDamage;
        private readonly float _baseSwordrainDamage;

        public CharacterConfig Config => _config;
        public BuffManager Buffs { get; } = new BuffManager();

        // Main Stats — derived once from Primal Stats, effectively
        // read-only for the rest of the battle.
        public float MaxHp { get; }
        public float MaxVhp { get; }
        public float Attack { get; }
        public float MagicAttack { get; }

        /// <summary>Base Slash Damage (from Attack) run through any active Buff/Debuff modifiers targeting it.</summary>
        public float SlashDamage => Buffs.GetModifiedStat(ModifiableStat.SlashDamage, _baseSlashDamage);

        /// <summary>Base Swordrain Damage (from Magic Attack) run through any active Buff/Debuff modifiers targeting it.</summary>
        public float SwordrainDamage => Buffs.GetModifiedStat(ModifiableStat.SwordrainDamage, _baseSwordrainDamage);

        // Battle Stats — mutable, change throughout the battle.
        public float CurrentHp { get; private set; }
        public float CurrentVhp { get; private set; }
        public float Mana { get; private set; }
        public int ShieldCount { get; private set; }
        public int ShieldStack { get; private set; }

        public bool IsDefeated => CurrentHp <= 0f;

        public CharacterState(CharacterConfig config, StatDerivationConfig derivation)
        {
            _config = config;

            Attack = derivation.CalculateAttack(config.Strength);
            MagicAttack = derivation.CalculateMagicAttack(config.Intelligence);
            MaxHp = derivation.CalculateMaxHp(config.Endurance);
            _baseSlashDamage = derivation.CalculateSlashDamage(Attack);
            _baseSwordrainDamage = derivation.CalculateSwordrainDamage(MagicAttack);
            MaxVhp = derivation.CalculateMaxVhp(MaxHp);

            CurrentHp = MaxHp;
            CurrentVhp = 0f;
            Mana = 0f;
            ShieldCount = 0;
            ShieldStack = 0;
        }

        public void HealHp(float amount)
        {
            if (Buffs.HasControlBuff(ControlBuffType.RecoveryBlock))
            {
                return;
            }
            CurrentHp = Mathf.Clamp(CurrentHp + amount, 0f, MaxHp);
        }

        public void HealVhp(float amount)
        {
            if (Buffs.HasControlBuff(ControlBuffType.RecoveryBlock))
            {
                return;
            }
            CurrentVhp = Mathf.Clamp(CurrentVhp + amount, 0f, MaxVhp);
        }

        public void AddMana(float amount)
        {
            Mana = Mathf.Clamp(Mana + amount, 0f, _config.MaxMana);
        }

        /// <summary>
        /// Spends Mana for a skill cast if enough is available, leaving
        /// Mana untouched and returning false otherwise. Callers should
        /// validate affordability before starting any other part of a
        /// skill's resolution, so a cast never half-applies its effects
        /// for want of Mana.
        /// </summary>
        public bool TrySpendMana(float amount)
        {
            if (Mana < amount)
            {
                return false;
            }
            Mana -= amount;
            return true;
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
        /// Applies incoming damage. Invincible blocks it entirely (0
        /// applied); otherwise one Shield Stack (if any) fully negates it
        /// instead. Returns the amount actually removed from HP.
        /// </summary>
        public float TakeDamage(float rawDamage)
        {
            if (Buffs.HasControlBuff(ControlBuffType.Invincible))
            {
                return 0f;
            }

            if (ShieldStack > 0)
            {
                ShieldStack--;
                return 0f;
            }

            float appliedDamage = Mathf.Max(0f, rawDamage);
            CurrentHp = Mathf.Clamp(CurrentHp - appliedDamage, 0f, MaxHp);
            return appliedDamage;
        }

        /// <summary>
        /// Applies damage that bypasses Shield Stack entirely (still
        /// respects Invincible). Used for the move-timeout penalty (which
        /// reuses Swordrain/Slash as its damage amount but doesn't arise
        /// from an actual Slash/Sword tile resolve) and for Skill-sourced
        /// damage — the design doc's Shield Stack rule only covers damage
        /// "from Slash and Sword" tiles, and separately states Shield has
        /// no effect against skills at all.
        /// </summary>
        public float TakeUnblockableDamage(float rawDamage)
        {
            if (Buffs.HasControlBuff(ControlBuffType.Invincible))
            {
                return 0f;
            }

            float appliedDamage = Mathf.Max(0f, rawDamage);
            CurrentHp = Mathf.Clamp(CurrentHp - appliedDamage, 0f, MaxHp);
            return appliedDamage;
        }
    }
}
