using System.Collections.Generic;
using Match3.Matching;
using Match3.Battle.Data;
using Match3.Battle.Model;

namespace Match3.Battle.Resolve
{
    /// <summary>
    /// Applies the Tile Resolve rules for one resolved match-and-cascade
    /// pass (a <see cref="MatchSearchResult"/>) to the acting character
    /// and their opponent. Pure C#, no MonoBehaviour.
    ///
    /// HP/VHP/Mana/Shield are instant, applied directly here. Slash and
    /// Sword damage are NOT applied here — they're described as
    /// <see cref="AttackAction"/>s on the returned outcome, since damage
    /// only lands once the attack's visual (melee lunge, projectile
    /// volley) actually reaches the defender.
    ///
    /// Every calculated amount is rounded to a whole number with a floor
    /// of 1 via <see cref="ResolveMath"/> — a percentage-of-a-percentage
    /// formula can easily produce a fraction of a point, which would
    /// otherwise apply and display as an invisible non-effect.
    ///
    /// Formula reference (design doc):
    /// - HP tile: Current HP += HpHealPercentPerTile% * MaxHP, per tile.
    /// - VHP tile: Current VHP += VhpHealPercentPerTile% * MaxVHP, per tile.
    /// - Mana tile: Mana += n (flat, capped at MaxMana).
    /// - Shield tile: Shield Count += n, then converts to Shield Stacks.
    /// - Slash tile: Melee = SlashDamage * n. Ranged = SlashDamage * n * ranged%, split across RangedObjectCount objects.
    /// - Sword tile: SwordrainDamage * n * sword%, fired as n objects (one per Sword tile matched — each therefore deals a constant SwordrainDamage * sword% on its own), plus a bonus Slash hit sized by the same n at swordTriggeredSlash% damage.
    /// </summary>
    public sealed class BattleResolveProcessor
    {
        private readonly BattleTileConfig _tileConfig;
        private readonly BattleTuningConfig _tuning;

        public BattleResolveProcessor(BattleTileConfig tileConfig, BattleTuningConfig tuning)
        {
            _tileConfig = tileConfig;
            _tuning = tuning;
        }

        public BattleResolveOutcome Resolve(MatchSearchResult matchResult, BattleSide actingSide, CharacterState actingCharacter)
        {
            BattleResolveOutcome outcome = new BattleResolveOutcome();
            IReadOnlyDictionary<TileKind, int> counts = CountTilesByKind(matchResult);

            ApplyHpHeal(counts, actingCharacter, outcome);
            ApplyVhpHeal(counts, actingCharacter, outcome);
            ApplyManaGain(counts, actingCharacter, outcome);
            ApplyShieldGain(counts, actingCharacter, outcome);
            ApplySlashDamage(counts, actingSide, actingCharacter, outcome);
            ApplySwordDamage(counts, actingSide, actingCharacter, outcome);

            int extraMoveCount = CountExtraMoveMatches(matchResult);
            if (extraMoveCount > 0)
            {
                outcome.AddExtraMovesEarned(extraMoveCount);
            }

            return outcome;
        }

        private IReadOnlyDictionary<TileKind, int> CountTilesByKind(MatchSearchResult matchResult)
        {
            Dictionary<TileKind, int> counts = new Dictionary<TileKind, int>();
            foreach (MatchGroup group in matchResult.Groups)
            {
                TileKind kind = _tileConfig.GetKind(group.TypeId);
                int tileCount = group.Positions.Count;
                counts[kind] = counts.TryGetValue(kind, out int existing) ? existing + tileCount : tileCount;
            }
            return counts;
        }

        /// <summary>Counts every match group that reaches the extra-turn length — a single move can earn more than one (a multi-group cascade step, or several 4+ matches across a cascade).</summary>
        private int CountExtraMoveMatches(MatchSearchResult matchResult)
        {
            int count = 0;
            foreach (MatchGroup group in matchResult.Groups)
            {
                if (group.Positions.Count >= _tuning.ExtraTurnMatchLength)
                {
                    count++;
                }
            }
            return count;
        }

        private void ApplyHpHeal(IReadOnlyDictionary<TileKind, int> counts, CharacterState character, BattleResolveOutcome outcome)
        {
            int n = GetCount(counts, TileKind.Hp);
            if (n <= 0)
            {
                return;
            }

            float rawAmount = character.MaxHp * (_tuning.HpHealPercentPerTile / 100f) * n;
            int amount = ResolveMath.RoundToMeaningfulAmount(rawAmount);
            character.HealHp(amount);
            outcome.AddHpHealed(amount);
        }

        private void ApplyVhpHeal(IReadOnlyDictionary<TileKind, int> counts, CharacterState character, BattleResolveOutcome outcome)
        {
            int n = GetCount(counts, TileKind.Vhp);
            if (n <= 0)
            {
                return;
            }

            float rawAmount = character.MaxVhp * (_tuning.VhpHealPercentPerTile / 100f) * n;
            int amount = ResolveMath.RoundToMeaningfulAmount(rawAmount);
            character.HealVhp(amount);
            outcome.AddVhpHealed(amount);
        }

        private void ApplyManaGain(IReadOnlyDictionary<TileKind, int> counts, CharacterState character, BattleResolveOutcome outcome)
        {
            int n = GetCount(counts, TileKind.Mana);
            if (n <= 0)
            {
                return;
            }

            // Already a whole count of tiles, but routed through
            // ResolveMath for consistency in case this formula ever
            // gains a percentage component.
            int amount = ResolveMath.RoundToMeaningfulAmount(n);
            character.AddMana(amount);
            outcome.AddManaGained(amount);
        }

        private void ApplyShieldGain(IReadOnlyDictionary<TileKind, int> counts, CharacterState character, BattleResolveOutcome outcome)
        {
            int n = GetCount(counts, TileKind.Shield);
            if (n <= 0)
            {
                return;
            }

            int stacksBefore = character.ShieldStack;
            character.AddShieldCount(n, _tuning.ShieldCountPerStack);

            outcome.AddShieldCountGained(n);
            outcome.AddShieldStacksGained(character.ShieldStack - stacksBefore);
        }

        private void ApplySlashDamage(IReadOnlyDictionary<TileKind, int> counts, BattleSide actingSide, CharacterState attacker, BattleResolveOutcome outcome)
        {
            int n = GetCount(counts, TileKind.Slash);
            if (n <= 0)
            {
                return;
            }

            int damage = ResolveMath.RoundToMeaningfulAmount(CalculateSlashDamage(attacker, n));
            int objectCount = GetSlashObjectCount(attacker);
            outcome.AddAttack(new AttackAction(actingSide, AttackKind.Slash, attacker.Config.AttackType, damage, objectCount));
        }

        private void ApplySwordDamage(IReadOnlyDictionary<TileKind, int> counts, BattleSide actingSide, CharacterState attacker, BattleResolveOutcome outcome)
        {
            int n = GetCount(counts, TileKind.Sword);
            if (n <= 0)
            {
                return;
            }

            float rawSwordDamage = attacker.SwordrainDamage * n * (_tuning.SwordDamagePercentPerTile / 100f);
            int swordDamage = ResolveMath.RoundToMeaningfulAmount(rawSwordDamage);
            outcome.AddAttack(new AttackAction(actingSide, AttackKind.Sword, AttackType.Ranged, swordDamage, n));

            // Sword tiles also proc a bonus Slash hit, sized by the same
            // Sword tile count (confirmed design decision), at reduced
            // damage — played as a second, separate attack right after.
            float triggeredSlashMultiplier = _tuning.SwordTriggeredSlashDamagePercent / 100f;
            float rawTriggeredSlashDamage = CalculateSlashDamage(attacker, n) * triggeredSlashMultiplier;
            int triggeredSlashDamage = ResolveMath.RoundToMeaningfulAmount(rawTriggeredSlashDamage);
            int slashObjectCount = GetSlashObjectCount(attacker);
            outcome.AddAttack(new AttackAction(actingSide, AttackKind.Slash, attacker.Config.AttackType, triggeredSlashDamage, slashObjectCount));
        }

        /// <summary>
        /// Melee deals a straight SlashDamage*n hit. Ranged spreads
        /// SlashDamage*n*rangedPercent across the character's configured
        /// object count — the total is the same either way; only the
        /// per-object split (rounded again, once the attack visually
        /// plays out) differs.
        /// </summary>
        private float CalculateSlashDamage(CharacterState attacker, int slashTileCount)
        {
            if (attacker.Config.AttackType == AttackType.Melee)
            {
                return attacker.SlashDamage * slashTileCount;
            }

            return attacker.SlashDamage * slashTileCount * (_tuning.SlashRangedDamagePercentPerTile / 100f);
        }

        private static int GetSlashObjectCount(CharacterState attacker)
        {
            return attacker.Config.AttackType == AttackType.Ranged ? attacker.Config.RangedObjectCount : 1;
        }

        private static int GetCount(IReadOnlyDictionary<TileKind, int> counts, TileKind kind)
        {
            return counts.TryGetValue(kind, out int count) ? count : 0;
        }
    }
}
