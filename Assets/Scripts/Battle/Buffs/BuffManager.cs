using System.Collections.Generic;
using Match3.Battle.Model;

namespace Match3.Battle.Buffs
{
    /// <summary>
    /// Owns every active <see cref="BuffInstance"/> on one character.
    /// One instance lives on each <see cref="CharacterState"/>.
    ///
    /// Stun is deliberately excluded from the normal per-move
    /// <see cref="TickTurn"/> sweep and ticked only via
    /// <see cref="ConsumeStunCharge"/>, called specifically when the
    /// stunned side's own turn is skipped — otherwise a "2 Turn" Stun
    /// could expire from the OPPONENT's moves ticking by instead of the
    /// stunned side's own, which would defeat the point of it.
    /// </summary>
    public sealed class BuffManager
    {
        private readonly List<BuffInstance> _activeBuffs = new List<BuffInstance>();

        public IReadOnlyList<BuffInstance> ActiveBuffs => _activeBuffs;

        /// <summary>Applies a buff, respecting Invincible's immunity to negative buffs and the definition's stacking rule if already active.</summary>
        public void Apply(BuffDefinition definition, CharacterState source)
        {
            if (definition.IsNegative && HasControlBuff(ControlBuffType.Invincible))
            {
                return;
            }

            BuffInstance existing = FindActive(definition);
            if (existing != null)
            {
                existing.Reapply();
                return;
            }

            _activeBuffs.Add(new BuffInstance(definition, source));

            if (definition.Category == BuffCategory.Effect && definition.ControlType == ControlBuffType.Invincible)
            {
                ClearAllNegativeBuffs();
            }
        }

        /// <summary>Advances every Turn-duration buff by one move, except Stun (see class remarks).</summary>
        public void TickTurn()
        {
            foreach (BuffInstance buff in _activeBuffs)
            {
                if (IsStunBuff(buff))
                {
                    continue;
                }
                buff.TickTurn();
            }
            RemoveExpired();
        }

        public void TickCycle()
        {
            foreach (BuffInstance buff in _activeBuffs)
            {
                buff.TickCycle();
            }
            RemoveExpired();
        }

        /// <summary>Call when this character's turn is skipped due to Stun — consumes exactly one of its remaining turns.</summary>
        public void ConsumeStunCharge()
        {
            FindActiveControlBuff(ControlBuffType.Stun)?.TickTurn();
            RemoveExpired();
        }

        public bool HasControlBuff(ControlBuffType type)
        {
            return FindActiveControlBuff(type) != null;
        }

        /// <summary>Runs a Battle Stat's base value through every active Buff/Debuff modifier targeting it, in the order they were applied.</summary>
        public float GetModifiedStat(ModifiableStat stat, float baseValue)
        {
            float runningValue = baseValue;
            foreach (BuffInstance buff in _activeBuffs)
            {
                if (buff.Definition.Category != BuffCategory.Buff)
                {
                    continue;
                }
                foreach (StatModifier modifier in buff.Definition.StatModifiers)
                {
                    if (modifier.Stat == stat)
                    {
                        runningValue = modifier.Apply(runningValue);
                    }
                }
            }
            return runningValue;
        }

        private static bool IsStunBuff(BuffInstance buff)
        {
            return buff.Definition.Category == BuffCategory.Effect && buff.Definition.ControlType == ControlBuffType.Stun;
        }

        private void ClearAllNegativeBuffs()
        {
            _activeBuffs.RemoveAll(buff => buff.Definition.IsNegative);
        }

        private void RemoveExpired()
        {
            _activeBuffs.RemoveAll(buff => buff.IsExpired);
        }

        private BuffInstance FindActive(BuffDefinition definition)
        {
            foreach (BuffInstance buff in _activeBuffs)
            {
                if (buff.Definition == definition)
                {
                    return buff;
                }
            }
            return null;
        }

        private BuffInstance FindActiveControlBuff(ControlBuffType type)
        {
            foreach (BuffInstance buff in _activeBuffs)
            {
                if (buff.Definition.Category == BuffCategory.Effect && buff.Definition.ControlType == type)
                {
                    return buff;
                }
            }
            return null;
        }
    }
}
