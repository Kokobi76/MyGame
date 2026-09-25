using System.Collections.Generic;
using UnityEngine;
using Match3.Battle.Buffs;
using Match3.Battle.Data;
using Match3.Battle.Model;
using Match3.Battle.Resolve;

namespace Match3.Battle.Skills
{
    /// <summary>
    /// Turns a skill's Damage Modifiers (see <see cref="SkillDefinition"/>)
    /// into queued <see cref="AttackAction"/>s, reusing the exact same
    /// attack-queue/visual-playback pipeline tile-triggered Slash/Sword
    /// damage already uses — no new visual code needed. Pure C#, mirrors
    /// <see cref="BattleResolveProcessor"/>'s shape but takes its formula
    /// as a parameter instead of a fixed config dependency, since the
    /// formula IS the per-skill data here.
    /// </summary>
    public static class SkillDamageResolver
    {
        /// <summary>
        /// Resolves every configured damage modifier into one
        /// <see cref="AttackAction"/> each. A modifier targeting
        /// <see cref="ModifiableStat.SwordrainDamage"/> always plays as a
        /// Ranged volley — matching how Sword tiles already behave in
        /// <see cref="BattleResolveProcessor"/> (a "rain of swords" reads
        /// as ranged regardless of Attack Type); a modifier targeting
        /// <see cref="ModifiableStat.SlashDamage"/> uses
        /// <paramref name="slashAttackType"/>. <paramref name="bypassesShield"/>
        /// is set on the resulting AttackAction via its (settable, added
        /// this session) BypassesShield property, added purely so the
        /// original AttackAction constructor never had to change.
        /// </summary>
        public static List<AttackAction> ResolveAttacks(
            IReadOnlyList<StatModifier> damageModifiers,
            BattleSide casterSide,
            CharacterState caster,
            AttackType slashAttackType,
            int rangedObjectCount,
            bool bypassesShield)
        {
            List<AttackAction> attacks = new List<AttackAction>();
            if (damageModifiers == null)
            {
                return attacks;
            }

            foreach (StatModifier modifier in damageModifiers)
            {
                bool isSword = modifier.Stat == ModifiableStat.SwordrainDamage;
                float baseDamage = isSword ? caster.SwordrainDamage : caster.SlashDamage;
                int damage = ResolveMath.RoundToMeaningfulAmount(modifier.Apply(baseDamage));

                AttackKind kind = isSword ? AttackKind.Sword : AttackKind.Slash;
                AttackType attackType = isSword ? AttackType.Ranged : slashAttackType;
                int objectCount = attackType == AttackType.Ranged ? Mathf.Max(rangedObjectCount, 1) : 1;

                AttackAction attack = new AttackAction(casterSide, kind, attackType, damage, objectCount);
                attack.BypassesShield = bypassesShield;
                attacks.Add(attack);
            }

            return attacks;
        }
    }
}
