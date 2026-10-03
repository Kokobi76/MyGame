using System.Collections.Generic;
using UnityEngine;
using Match3.Battle.Data;
using Match3.Battle.Buffs;
using Match3.Battle.View;

namespace Match3.Battle.Skills
{
    /// <summary>
    /// The 3 skill archetypes from the design doc. Turn Count is FIXED
    /// per category, per the doc's own table (Buff Skill = No, Opponent
    /// Skill = Yes, Tiles Skill = Yes) — see
    /// <see cref="SkillDefinition.CostsMove"/>, which is computed from
    /// this rather than a separate Inspector toggle. Each category also
    /// reads a different subset of <see cref="SkillDefinition"/>'s other
    /// fields (see the Header groupings below) — nothing enforces that
    /// at the data level for THOSE fields, a misconfigured asset simply
    /// has its irrelevant ones ignored by whichever resolver reads them.
    /// </summary>
    public enum SkillCategory
    {
        /// <summary>Only creates Buff/Debuff/Effect objects. Never costs a move — can be cast repeatedly in one turn, limited only by Mana.</summary>
        BuffSkill,

        /// <summary>Deals direct damage to the opponent (Melee or Ranged) and may also apply buffs. Always costs a move — at most 1 per move.</summary>
        OpponentSkill,

        /// <summary>Destroys board tiles in a Destroy Area shape and/or fires objects at the opponent directly; any buffs must be negative and Opponent-targeted (enforced by BattleController — see the doc's "Tiles Skill only creates Debuff" rule). Always costs a move — at most 1 per move.</summary>
        TilesSkill
    }

    /// <summary>
    /// The designer-facing "recipe" for one skill — everything
    /// <see cref="Match3.Battle.Gameplay.BattleController.TryCastSkill"/>
    /// needs to resolve a cast. One asset per skill; assign up to 3 per
    /// character via <see cref="SkillLoadout"/>.
    ///
    /// Damage is expressed by reusing <see cref="StatModifier"/> — the
    /// exact same Sign (+,-,*,/, flat-or-%) shape the design doc uses for
    /// both Buff stat modifiers and a Skill's Source Damage column. Add a
    /// modifier targeting <see cref="ModifiableStat.SlashDamage"/> for a
    /// "Slash" source, one targeting
    /// <see cref="ModifiableStat.SwordrainDamage"/> for "Sword", or both
    /// for "Sword + Slash" — each produces its own queued attack (see
    /// <see cref="SkillDamageResolver"/>). An empty list means no direct
    /// damage (Buff Skill).
    /// </summary>
    [CreateAssetMenu(fileName = "SkillDefinition", menuName = "Match3/Battle/Skill Definition")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _displayName = "Skill";
        [TextArea] [SerializeField] private string _description;
        [SerializeField] private SkillCategory _category = SkillCategory.BuffSkill;

        [Header("Cost")]
        [Tooltip("Most skills consume Mana per the design doc (\"Hầu hết là sẽ tiêu hao\"), but it's optional per-skill.")]
        [SerializeField] private bool _requiresMana = true;
        [Tooltip("Only used when Requires Mana is checked.")]
        [SerializeField] [Min(0f)] private float _manaCost = 20f;

        [Header("Buffs — the Buff/Debuff/Effect objects this skill creates")]
        [Tooltip("Every Buff Skill uses this; Opponent/Tiles Skill may optionally also carry buffs alongside their damage. Each definition's own Target (Self/Opponent) decides who it lands on. A Tiles Skill only ever applies entries that are both negative and Opponent-targeted — anything else in the list is skipped for a Tiles Skill at cast time, per the doc.")]
        [SerializeField] private List<BuffDefinition> _buffsToApply = new List<BuffDefinition>();

        [Header("Damage — Source Damage + Sign (Opponent Skill; also Tiles Skill's opponent-facing objects)")]
        [SerializeField] private List<StatModifier> _damageModifiers = new List<StatModifier>();

        [Header("Attack Range — Opponent Skill only")]
        [Tooltip("Tiles Skill's objects have no Melee option (see Tiles Skill Objects below) — this is only read for Opponent Skill.")]
        [SerializeField] private AttackType _attackRange = AttackType.Melee;
        [Tooltip("Object count used when Attack Range is Ranged. Unused for Melee (always 1 object).")]
        [SerializeField] [Min(1)] private int _rangedObjectCount = 1;

        [Header("Tiles Skill Objects — Tiles Skill only")]
        [Tooltip("How many objects fly at the opponent directly, splitting this skill's damage across them (always Ranged — Tiles Skill has no Melee option). Rolled as a random count in [min, max] at cast time.")]
        [SerializeField] [Min(0)] private int _objectsAttackOpponentMin;
        [SerializeField] [Min(0)] private int _objectsAttackOpponentMax;
        [Tooltip("How many objects fly into the board — each one destroys one Destroy Area's worth of tiles, anchored at a random currently-filled cell, never the same cell twice in one cast (no targeting UI yet). Adjacent/overlapping applications simply merge into one bigger region. Rolled as a random count in [min, max] at cast time.")]
        [SerializeField] [Min(0)] private int _objectsAttackBoardMin;
        [SerializeField] [Min(0)] private int _objectsAttackBoardMax = 1;
        [SerializeField] private DestroyAreaShape _destroyArea = DestroyAreaShape.Block;
        [Tooltip("Only used when Destroy Area is Block. Size of the N x N square, 1-7. Always CENTERED on the chosen anchor tile: odd sizes (1,3,5,7) center exactly on that tile; even sizes (2,4,6) have no single center tile, so the object's flight target is nudged to the true geometric midpoint instead (see AttackVisualController.PlayBoardDestroyAttack) — the destroyed cells themselves still form a full N x N block either way.")]
        [SerializeField] [Range(1, 7)] private int _blockSize = 3;
        [Tooltip("Only used when Destroy Area is Special. Cell offsets from the anchor — (0,0) is the anchor cell itself, treated as the center of whatever custom shape you're building.")]
        [SerializeField] private List<Vector2Int> _specialShapeOffsets = new List<Vector2Int>();

        [Header("Board-Destroy Visual — Tiles Skill only")]
        [Tooltip("Prefab flown into the board for THIS skill's board-destroy objects (see AttackProjectileView — drag any prefab built from that component; its own Flight Duration field controls this skill's object speed). Leave empty to use Attack Visual Controller's own default board-destroy prefab instead.")]
        [SerializeField] private AttackProjectileView _boardObjectPrefab;
        [Tooltip("Extra world-space offset added on top of the caster's normal projectile spawn point, for THIS skill's board-destroy objects only — e.g. a positive Y to make them fall from higher up. (0,0,0) = spawn exactly at the caster's usual point.")]
        [SerializeField] private Vector3 _boardObjectSpawnOffset;

        public string DisplayName => _displayName;
        public string Description => _description;
        public SkillCategory Category => _category;

        /// <summary>Fixed per Category per the design doc's table — Buff Skill is always free, Opponent/Tiles Skill always cost a move. Not designer-configurable.</summary>
        public bool CostsMove => _category != SkillCategory.BuffSkill;

        public bool RequiresMana => _requiresMana;
        public float ManaCost => _manaCost;
        public IReadOnlyList<BuffDefinition> BuffsToApply => _buffsToApply;
        public IReadOnlyList<StatModifier> DamageModifiers => _damageModifiers;
        public AttackType AttackRange => _attackRange;
        public int RangedObjectCount => _rangedObjectCount;
        public int ObjectsAttackOpponentMin => _objectsAttackOpponentMin;
        public int ObjectsAttackOpponentMax => _objectsAttackOpponentMax;
        public int ObjectsAttackBoardMin => _objectsAttackBoardMin;
        public int ObjectsAttackBoardMax => _objectsAttackBoardMax;
        public DestroyAreaShape DestroyArea => _destroyArea;
        public int BlockSize => _blockSize;
        public IReadOnlyList<Vector2Int> SpecialShapeOffsets => _specialShapeOffsets;
        public AttackProjectileView BoardObjectPrefab => _boardObjectPrefab;
        public Vector3 BoardObjectSpawnOffset => _boardObjectSpawnOffset;

        /// <summary>True when Destroy Area is an even-sized Block (2/4/6) — these have no single center tile, so the visual target needs nudging to the true midpoint. See <see cref="Match3.Battle.View.AttackVisualController.PlayBoardDestroyAttack"/>.</summary>
        public bool NeedsEvenBlockCenterOffset => _destroyArea == DestroyAreaShape.Block && _blockSize % 2 == 0;
    }
}
