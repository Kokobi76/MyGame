using System.Collections.Generic;
using UnityEngine;

namespace Match3.Battle.Skills
{
    /// <summary>
    /// One character's active skill bar — up to 3 slots, in order. A
    /// separate asset (rather than a field on
    /// <see cref="Match3.Battle.Data.CharacterConfig"/>) so the core
    /// Character Config doesn't need to know the Skill System exists;
    /// assign one loadout asset per side on <see cref="Match3.Battle.Gameplay.BattleController"/>.
    /// <see cref="Match3.Battle.UI.SkillSlotView"/> reads
    /// <see cref="GetSlot"/> to know which skill each of its 3 buttons
    /// casts.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillLoadout", menuName = "Match3/Battle/Skill Loadout")]
    public sealed class SkillLoadout : ScriptableObject
    {
        private const int MaxSlots = 3;

        [Tooltip("Up to 3 active skill slots, in order. Leave a slot empty (None) to leave it unused. Trimmed to 3 automatically if more are added.")]
        [SerializeField] private List<SkillDefinition> _skills = new List<SkillDefinition>();

        /// <summary>The skill in the given slot (0-2), or null if that slot is empty or out of range.</summary>
        public SkillDefinition GetSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _skills.Count ? _skills[slotIndex] : null;
        }

        private void OnValidate()
        {
            while (_skills.Count > MaxSlots)
            {
                _skills.RemoveAt(_skills.Count - 1);
            }
        }
    }
}
