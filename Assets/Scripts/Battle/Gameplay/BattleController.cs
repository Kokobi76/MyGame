using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Match3.Model;
using Match3.Matching;
using Match3.Swap;
using Match3.Gameplay;
using Match3.InputHandling;
using Match3.Battle.Data;
using Match3.Battle.Model;
using Match3.Battle.Resolve;
using Match3.Battle.Turn;
using Match3.Battle.AI;
using Match3.Battle.View;
using Match3.Battle.Buffs;
using Match3.Battle.Skills;

namespace Match3.Battle.Gameplay
{
    /// <summary>
    /// Mirrors the live values of one <see cref="CharacterState"/> as
    /// plain serialized fields purely so Unity's default Inspector shows
    /// them (read-only in spirit — they're overwritten every time the
    /// source character's stats change, so editing them by hand has no
    /// lasting effect). No gameplay code reads from this.
    /// </summary>
    [Serializable]
    public struct CharacterDebugSnapshot
    {
        public float CurrentHp;
        public float MaxHp;
        public float CurrentVhp;
        public float MaxVhp;
        public float Mana;
        public float MaxMana;
        public int ShieldCount;
        public int ShieldStack;
    }

    /// <summary>
    /// Top-level orchestrator for a 1v1 turn-based battle played on the
    /// core Match3 board. Subscribes to <see cref="BoardController"/>'s
    /// events, applies Tile Resolve effects to the two
    /// <see cref="CharacterState"/>s via <see cref="BattleResolveProcessor"/>,
    /// plays out Slash/Sword attacks visually (queued, one at a time)
    /// through <see cref="AttackVisualController"/> — applying their
    /// damage only once each object actually connects — spawns floating
    /// combat text for every effect via
    /// <see cref="FloatingCombatTextController"/>, and drives turn order,
    /// including automated moves (Enemy always; Player when auto-play is
    /// enabled) and the Player's move timer. Player always moves first.
    ///
    /// Also resolves Skill casts via <see cref="TryCastSkill"/> — Buff
    /// application, direct opponent damage (reusing the same
    /// attack-queue pipeline as tile-triggered Slash/Sword), and Tiles
    /// Skill board destruction (via <see cref="BoardController.DestroyPositionsRoutine"/>)
    /// — sharing the same Mana bookkeeping and, for Opponent/Tiles Skill,
    /// the same turn-completion path as a normal move. Each side's active
    /// skills come from its own <see cref="SkillLoadout"/> (up to 3 slots,
    /// read by <see cref="Match3.Battle.UI.SkillSlotView"/>).
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
        [Header("Core Board")]
        [SerializeField] private BoardController _boardController;
        [SerializeField] private BoardInputController _inputController;

        [Header("Battle Configuration")]
        [SerializeField] private BattleTileConfig _tileConfig;
        [SerializeField] private BattleTuningConfig _tuningConfig;
        [SerializeField] private StatDerivationConfig _statDerivation;
        [SerializeField] private CharacterConfig _playerConfig;
        [SerializeField] private CharacterConfig _enemyConfig;

        [Header("Skills")]
        [Tooltip("Each side's up-to-3 active skill slots. Optional — leave unassigned if this battle doesn't use skills yet.")]
        [SerializeField] private SkillLoadout _playerSkillLoadout;
        [SerializeField] private SkillLoadout _enemySkillLoadout;

        [Header("Visuals")]
        [SerializeField] private AttackVisualController _attackVisuals;
        [SerializeField] private FloatingCombatTextController _floatingText;

        [Header("Automated Move Pacing")]
        [Tooltip("Delay before an automated side (Enemy, or an auto-playing Player) plays its move — purely for readability, no gameplay effect.")]
        [SerializeField] private float _automatedMoveDelaySeconds = 0.5f;

        [Header("Player Move Timer")]
        [Tooltip("Seconds the Player has to make a move before the Enemy punishes them. Ignored while the Player is auto-playing.")]
        [SerializeField] private float _playerMoveTimeLimitSeconds = 20f;

        [Header("Debug (read-only, updates during Play)")]
        [SerializeField] private CharacterDebugSnapshot _playerDebugSnapshot;
        [SerializeField] private CharacterDebugSnapshot _enemyDebugSnapshot;

        private CharacterState _playerState;
        private CharacterState _enemyState;
        private TurnCycleTracker _turnTracker;
        private BattleResolveProcessor _resolveProcessor;
        private MoveTimer _moveTimer;

        private readonly Queue<AttackAction> _pendingAttacks = new Queue<AttackAction>();
        private bool _isProcessingAttackQueue;

        private IMoveSelector _enemyMoveSelector;
        private IMoveSelector _playerAutoMoveSelector;

        private bool _isPlayerAutoPlayEnabled;
        private int _extraMovesEarnedThisTurn;
        private bool _isBattleOver;

        // Skill casting — see TryCastSkill / CastSkillRoutine. A side's
        // own Costs Move flag (fixed per Category, see SkillDefinition)
        // already limits Opponent/Tiles Skill to at most 1 per move —
        // casting one calls CompleteMove, which moves the turn tracker on
        // to a new move slot (same side via a banked extra move, or the
        // other side) before another cast could be evaluated. Buff Skill
        // never costs a move and is intentionally NOT limited beyond
        // Mana, per spec — so no "already used a skill this turn"
        // bookkeeping is needed at all.
        private bool _isCastingSkill;

        public CharacterState PlayerState => _playerState;
        public CharacterState EnemyState => _enemyState;
        public TurnCycleTracker TurnTracker => _turnTracker;
        public MoveTimer MoveTimer => _moveTimer;
        public AiDifficulty EnemyAiDifficulty { get; private set; }
        public bool IsPlayerAutoPlayEnabled => _isPlayerAutoPlayEnabled;
        public BattleTuningConfig TuningConfig => _tuningConfig;

        public event Action<BattleSide> CharacterStatsChanged;
        public event Action<BattleSide> BattleEnded;
        public event Action<BattleSide, SkillDefinition> SkillCast;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            _playerState = new CharacterState(_playerConfig, _statDerivation);
            _enemyState = new CharacterState(_enemyConfig, _statDerivation);
            _turnTracker = new TurnCycleTracker(BattleSide.Player);
            _resolveProcessor = new BattleResolveProcessor(_tileConfig, _tuningConfig);

            _moveTimer = new MoveTimer();
            _moveTimer.Expired += MoveTimer_Expired;

            _playerAutoMoveSelector = new RandomMoveSelector();
            EnemyAiDifficulty = RollEnemyAiDifficulty();
            _enemyMoveSelector = CreateMoveSelector(EnemyAiDifficulty);

            UpdateDebugSnapshot(BattleSide.Player);
            UpdateDebugSnapshot(BattleSide.Enemy);
        }

        private void Start()
        {
            // Deferred to Start(), not Awake(): CharacterView's own Awake()
            // (which caches its SpriteRenderer) is not guaranteed to have
            // run yet at this point in Awake — Unity only guarantees ALL
            // Awake() calls finish before ANY Start() call.
            _attackVisuals.GetView(BattleSide.Player).SetAppearance(_playerConfig.Sprite, _playerConfig.Color);
            _attackVisuals.GetView(BattleSide.Enemy).SetAppearance(_enemyConfig.Sprite, _enemyConfig.Color);

            ApplyInputGate();
        }

        private void OnEnable()
        {
            _boardController.MatchesResolved += BoardController_MatchesResolved;
            _boardController.CascadeCompleted += BoardController_CascadeCompleted;
            _turnTracker.CycleCompleted += TurnTracker_CycleCompleted;
        }

        private void OnDisable()
        {
            _boardController.MatchesResolved -= BoardController_MatchesResolved;
            _boardController.CascadeCompleted -= BoardController_CascadeCompleted;
            _turnTracker.CycleCompleted -= TurnTracker_CycleCompleted;
        }

        private void Update()
        {
            if (_moveTimer.IsRunning)
            {
                _moveTimer.Tick(Time.unscaledDeltaTime);
            }
        }

        /// <summary>
        /// Turns the Player's auto-play mode on or off. While enabled,
        /// the Player's moves are chosen the same way the Enemy's are
        /// (always "Pick Random" for the Player, per spec), the move
        /// timer is suspended, and dragging tiles is disabled. Toggling
        /// this on immediately plays the Player's pending move if it's
        /// currently their turn.
        /// </summary>
        public void SetPlayerAutoPlay(bool isEnabled)
        {
            _isPlayerAutoPlayEnabled = isEnabled;
            ApplyInputGate();

            if (isEnabled && IsCurrentSideAutomated() && !_isBattleOver)
            {
                StartCoroutine(TakeAutomatedTurnRoutine());
            }
        }

        /// <summary>
        /// Applies a buff to one side, sourced by the other side's
        /// character (or the same side, for a self-buff — pass the same
        /// value for both). This is the entry point the Skill System
        /// (see <see cref="TryCastSkill"/>) calls to grant Buff/Debuff/
        /// Effect objects; also directly usable for testing buffs on
        /// their own.
        /// </summary>
        public void ApplyBuff(BattleSide targetSide, BuffDefinition definition, BattleSide sourceSide)
        {
            GetCharacter(targetSide).Buffs.Apply(definition, GetCharacter(sourceSide));
        }

        /// <summary>The side's assigned active-skill bar (up to 3 slots), or null if that side has none assigned. Read by <see cref="Match3.Battle.UI.SkillSlotView"/>.</summary>
        public SkillLoadout GetSkillLoadout(BattleSide side)
        {
            return side == BattleSide.Player ? _playerSkillLoadout : _enemySkillLoadout;
        }

        /// <summary>
        /// Attempts to cast a skill for one side right now. Returns false
        /// without changing anything if the cast isn't currently legal —
        /// see <see cref="CanCastSkill"/> for the exact conditions. This
        /// is the entry point <see cref="Match3.Battle.UI.SkillSlotView"/>
        /// calls; it is NOT wired into AI move selection — Enemy and an
        /// auto-playing Player still only ever swap tiles.
        /// </summary>
        public bool TryCastSkill(BattleSide casterSide, SkillDefinition skill)
        {
            if (!CanCastSkill(casterSide, skill))
            {
                return false;
            }

            StartCoroutine(CastSkillRoutine(casterSide, skill));
            return true;
        }

        /// <summary>
        /// Whether <paramref name="casterSide"/> could legally cast
        /// <paramref name="skill"/> right now: it must be their turn, the
        /// battle must still be going, nothing else may currently be
        /// resolving (another skill cast, an attack sequence, or the
        /// board itself being mid-swap/mid-cascade), and if the skill
        /// requires Mana they must have enough. There is no separate
        /// "already used a skill this turn" check — see the field remarks
        /// on <see cref="_isCastingSkill"/> for why that's unnecessary.
        /// Exposed publicly so UI (e.g. a skill button) can grey itself
        /// out without needing to duplicate this logic.
        /// </summary>
        public bool CanCastSkill(BattleSide casterSide, SkillDefinition skill)
        {
            if (_isBattleOver || skill == null)
            {
                return false;
            }
            if (_isCastingSkill || _isProcessingAttackQueue || !_boardController.IsIdle)
            {
                return false;
            }
            if (_turnTracker.CurrentSide != casterSide)
            {
                return false;
            }

            CharacterState caster = GetCharacter(casterSide);
            if (skill.RequiresMana && caster.Mana < skill.ManaCost)
            {
                return false;
            }
            return true;
        }

        private bool ValidateReferences()
        {
            if (_boardController == null)
            {
                Debug.LogError("BattleController requires a BoardController reference.", this);
                return false;
            }
            if (_inputController == null)
            {
                Debug.LogError("BattleController requires a BoardInputController reference.", this);
                return false;
            }
            if (_tileConfig == null || _tuningConfig == null)
            {
                Debug.LogError("BattleController requires BattleTileConfig and BattleTuningConfig references.", this);
                return false;
            }
            if (_statDerivation == null)
            {
                Debug.LogError("BattleController requires a StatDerivationConfig reference.", this);
                return false;
            }
            if (_playerConfig == null || _enemyConfig == null)
            {
                Debug.LogError("BattleController requires a CharacterConfig for both Player and Enemy.", this);
                return false;
            }
            if (_attackVisuals == null)
            {
                Debug.LogError("BattleController requires an AttackVisualController reference.", this);
                return false;
            }
            if (_floatingText == null)
            {
                Debug.LogError("BattleController requires a FloatingCombatTextController reference.", this);
                return false;
            }
            return true;
        }

        private void BoardController_MatchesResolved(MatchSearchResult matchResult)
        {
            if (_isBattleOver)
            {
                return;
            }

            BattleSide actingSide = _turnTracker.CurrentSide;
            CharacterState actingCharacter = GetCharacter(actingSide);

            BattleResolveOutcome outcome = _resolveProcessor.Resolve(matchResult, actingSide, actingCharacter);
            _extraMovesEarnedThisTurn += outcome.ExtraMovesEarned;

            OnCharacterStatsChanged(actingSide);
            SpawnInstantEffectFloatingText(actingSide, outcome);

            foreach (AttackAction attack in outcome.Attacks)
            {
                _pendingAttacks.Enqueue(attack);
            }

            if (!_isProcessingAttackQueue && _pendingAttacks.Count > 0)
            {
                StartCoroutine(ProcessAttackQueueRoutine());
            }
        }

        private void BoardController_CascadeCompleted()
        {
            if (_isBattleOver)
            {
                return;
            }
            StartCoroutine(FinishTurnRoutine());
        }

        /// <summary>Waits for any attacks queued by this move to finish playing out and applying their damage before the turn actually advances.</summary>
        private IEnumerator FinishTurnRoutine()
        {
            yield return new WaitUntil(() => !_isProcessingAttackQueue && _pendingAttacks.Count == 0);

            if (_isBattleOver)
            {
                yield break;
            }

            _turnTracker.CompleteMove(_extraMovesEarnedThisTurn);
            _extraMovesEarnedThisTurn = 0;
            TickAllBuffsTurn();

            ApplyInputGate();

            if (IsCurrentSideAutomated())
            {
                StartCoroutine(TakeAutomatedTurnRoutine());
            }
        }

        private IEnumerator ProcessAttackQueueRoutine()
        {
            _isProcessingAttackQueue = true;

            while (_pendingAttacks.Count > 0)
            {
                if (_isBattleOver)
                {
                    _pendingAttacks.Clear();
                    break;
                }

                AttackAction attack = _pendingAttacks.Dequeue();
                yield return PlayAttack(attack);
            }

            _isProcessingAttackQueue = false;
        }

        private IEnumerator PlayAttack(AttackAction attack)
        {
            CharacterState defender = GetCharacter(attack.AttackerSide.GetOpposite());

            if (attack.Kind == AttackKind.Slash && attack.AttackType == AttackType.Melee)
            {
                yield return _attackVisuals.PlayMeleeAttack(attack.AttackerSide, () => ApplyAttackDamage(defender, attack.TotalDamage, attack.BypassesShield));
                yield break;
            }

            int perObjectDamage = ResolveMath.RoundToMeaningfulAmount(attack.TotalDamage / Mathf.Max(attack.ObjectCount, 1));
            if (attack.Kind == AttackKind.Slash)
            {
                yield return _attackVisuals.PlayRangedSlashAttack(attack.AttackerSide, attack.ObjectCount, () => ApplyAttackDamage(defender, perObjectDamage, attack.BypassesShield));
            }
            else
            {
                yield return _attackVisuals.PlaySwordAttack(attack.AttackerSide, attack.ObjectCount, () => ApplyAttackDamage(defender, perObjectDamage, attack.BypassesShield));
            }
        }

        /// <summary>
        /// Applies one landed hit's damage. If the defender has a Shield
        /// Stack AND this hit doesn't bypass it, the hit is fully blocked
        /// (no HP lost) and the floating text shows the shield
        /// consumption instead of a damage number. Skill-sourced hits set
        /// <paramref name="bypassesShield"/> so Shield never blocks them
        /// at all, per the design doc.
        /// </summary>
        private void ApplyAttackDamage(CharacterState defender, float damage, bool bypassesShield)
        {
            if (_isBattleOver)
            {
                return;
            }

            int shieldStacksBefore = defender.ShieldStack;
            float appliedDamage = bypassesShield ? defender.TakeUnblockableDamage(damage) : defender.TakeDamage(damage);
            bool wasBlockedByShield = !bypassesShield && defender.ShieldStack < shieldStacksBefore;

            BattleSide defenderSide = defender == _playerState ? BattleSide.Player : BattleSide.Enemy;
            OnCharacterStatsChanged(defenderSide);

            Vector3 textPosition = _attackVisuals.GetView(defenderSide).TargetPoint;
            if (wasBlockedByShield)
            {
                _floatingText.Spawn(textPosition, FloatingTextKind.ShieldBlock, 1);
            }
            else
            {
                _floatingText.Spawn(textPosition, FloatingTextKind.Damage, Mathf.Max(Mathf.RoundToInt(appliedDamage), 1));
            }

            if (defender.IsDefeated)
            {
                EndBattle(defenderSide.GetOpposite());
            }
        }

        private void MoveTimer_Expired()
        {
            if (_isBattleOver)
            {
                return;
            }

            // int penaltyDamage = Mathf.RoundToInt(Mathf.Max(_enemyConfig.SwordrainDamage, _enemyConfig.SlashDamage));
            // _playerState.TakeUnblockableDamage(penaltyDamage);
            // OnCharacterStatsChanged(BattleSide.Player);
            // _floatingText.Spawn(_attackVisuals.GetView(BattleSide.Player).TargetPoint, FloatingTextKind.Damage, penaltyDamage);

            if (_playerState.IsDefeated)
            {
                EndBattle(BattleSide.Enemy);
                return;
            }

            _turnTracker.ForceAdvanceTurn();
            TickAllBuffsTurn();
            ApplyInputGate();

            if (IsCurrentSideAutomated())
            {
                StartCoroutine(TakeAutomatedTurnRoutine());
            }
        }

        /// <summary>
        /// Resolves one skill cast end to end: spends Mana, applies every
        /// configured buff (a Tiles Skill only ever applies the ones that
        /// are both negative and Opponent-targeted — anything else in its
        /// list is skipped, per the doc's "Tiles Skill only creates
        /// Debuff" rule), queues and plays out any direct-damage attacks
        /// (reusing the normal attack queue), then — for a Tiles Skill
        /// with any Objects-Attack-Board roll — destroys the resolved
        /// Destroy Area on the board and waits for whatever that triggers
        /// to settle. Turn completion is decided last: a free (Buff
        /// Skill) cast never touches the turn tracker at all; Destroy
        /// Area = All always force-advances (and wipes banked extra
        /// moves) regardless of Category, per the doc; otherwise the
        /// skill's own (Category-fixed) Costs Move flag decides.
        /// </summary>
        private IEnumerator CastSkillRoutine(BattleSide casterSide, SkillDefinition skill)
        {
            _isCastingSkill = true;

            CharacterState caster = GetCharacter(casterSide);
            BattleSide opponentSide = casterSide.GetOpposite();

            if (skill.RequiresMana && !caster.TrySpendMana(skill.ManaCost))
            {
                // Shouldn't happen — CanCastSkill already checked this —
                // but bail out cleanly rather than half-apply a cast if
                // it somehow does.
                _isCastingSkill = false;
                yield break;
            }

            OnCharacterStatsChanged(casterSide);
            OnSkillCast(casterSide, skill);

            _inputController.SetExternalGate(false);
            _moveTimer.Stop();

            foreach (BuffDefinition buffDefinition in skill.BuffsToApply)
            {
                if (skill.Category == SkillCategory.TilesSkill && !(buffDefinition.IsNegative && buffDefinition.Target == BuffTarget.Opponent))
                {
                    continue;
                }
                BattleSide targetSide = buffDefinition.Target == BuffTarget.Self ? casterSide : opponentSide;
                ApplyBuff(targetSide, buffDefinition, casterSide);
            }

            foreach (AttackAction attack in BuildDirectAttacks(casterSide, caster, skill))
            {
                _pendingAttacks.Enqueue(attack);
            }
            if (!_isProcessingAttackQueue && _pendingAttacks.Count > 0)
            {
                StartCoroutine(ProcessAttackQueueRoutine());
            }
            yield return new WaitUntil(() => !_isProcessingAttackQueue && _pendingAttacks.Count == 0);

            bool forcedFullBoardClear = false;

            if (!_isBattleOver && skill.Category == SkillCategory.TilesSkill)
            {
                HashSet<Vector2Int> destroyPositions = DestroyAreaResolver.ResolveBoardDestroyPositions(skill, _boardController.Board);
                if (destroyPositions.Count > 0)
                {
                    forcedFullBoardClear = skill.DestroyArea == DestroyAreaShape.All;

                    yield return _boardController.DestroyPositionsRoutine(new List<Vector2Int>(destroyPositions));

                    // The forced destroy (and any natural cascade it set
                    // off) may itself have enqueued Slash/Sword attacks
                    // via the normal BoardController_MatchesResolved
                    // handler — wait for those too before finishing.
                    yield return new WaitUntil(() => !_isProcessingAttackQueue && _pendingAttacks.Count == 0);
                }
            }

            if (!_isBattleOver)
            {
                if (forcedFullBoardClear)
                {
                    _turnTracker.ForceAdvanceTurn();
                    _extraMovesEarnedThisTurn = 0;
                    TickAllBuffsTurn();
                }
                else if (skill.CostsMove)
                {
                    _turnTracker.CompleteMove(_extraMovesEarnedThisTurn);
                    _extraMovesEarnedThisTurn = 0;
                    TickAllBuffsTurn();
                }
                // else: a free (Buff Skill) cast — turn tracker untouched
                // entirely; the caster keeps their turn.

                ApplyInputGate();

                if (IsCurrentSideAutomated())
                {
                    StartCoroutine(TakeAutomatedTurnRoutine());
                }
            }

            _isCastingSkill = false;
        }

        /// <summary>Builds this skill's direct-opponent-damage AttackActions, if any, from its Damage Modifiers — Opponent Skill uses its own configured Attack Range/object count, Tiles Skill rolls its Objects-Attack-Opponent range and always fires Ranged (it has no Melee option). Buff Skill (or any skill with no Damage Modifiers configured) naturally produces none.</summary>
        private List<AttackAction> BuildDirectAttacks(BattleSide casterSide, CharacterState caster, SkillDefinition skill)
        {
            if (skill.DamageModifiers.Count == 0)
            {
                return new List<AttackAction>();
            }

            if (skill.Category == SkillCategory.OpponentSkill)
            {
                return SkillDamageResolver.ResolveAttacks(skill.DamageModifiers, casterSide, caster, skill.AttackRange, skill.RangedObjectCount, bypassesShield: true);
            }

            if (skill.Category == SkillCategory.TilesSkill)
            {
                int objectCount = SkillRandom.RollObjectCount(skill.ObjectsAttackOpponentMin, skill.ObjectsAttackOpponentMax);
                if (objectCount <= 0)
                {
                    return new List<AttackAction>();
                }
                return SkillDamageResolver.ResolveAttacks(skill.DamageModifiers, casterSide, caster, AttackType.Ranged, objectCount, bypassesShield: true);
            }

            return new List<AttackAction>();
        }

        private IEnumerator TakeAutomatedTurnRoutine()
        {
            yield return new WaitForSeconds(_automatedMoveDelaySeconds);

            // Re-check rather than trusting the state at the moment this
            // coroutine was started: it may have been superseded by a
            // toggle (e.g. auto-play switched off again) during the delay.
            if (_isBattleOver || !IsCurrentSideAutomated())
            {
                yield break;
            }

            IMoveSelector selector = GetActiveMoveSelector();
            IReadOnlyList<EvaluatedMove> validMoves = _boardController.GetAllValidMoves();
            if (selector.TryGetMove(validMoves, out SwapMove move))
            {
                _boardController.RequestSwap(move.From, move.To);
            }
        }

        private bool IsCurrentSideAutomated()
        {
            return _turnTracker.CurrentSide == BattleSide.Enemy
                || (_turnTracker.CurrentSide == BattleSide.Player && _isPlayerAutoPlayEnabled);
        }

        private IMoveSelector GetActiveMoveSelector()
        {
            return _turnTracker.CurrentSide == BattleSide.Enemy ? _enemyMoveSelector : _playerAutoMoveSelector;
        }

        /// <summary>Only lets the Player drag tiles during their own, non-auto-played turn — combined (AND) with the board's own Idle/Busy gate. Also starts/stops the move timer to match.</summary>
        private void ApplyInputGate()
        {
            ResolveStunSkips();

            bool isPlayerManualTurn = _turnTracker.CurrentSide == BattleSide.Player && !_isPlayerAutoPlayEnabled && !_isBattleOver;
            _inputController.SetExternalGate(isPlayerManualTurn);

            if (isPlayerManualTurn)
            {
                _moveTimer.Start(_playerMoveTimeLimitSeconds);
            }
            else
            {
                _moveTimer.Stop();
            }
        }

        /// <summary>
        /// If the side about to act is Stunned, skips their turn entirely
        /// (no input, no AI move) and consumes one of Stun's remaining
        /// turns — repeating in the unlikely case the next side is also
        /// Stunned. A hard iteration cap guards against any future buff
        /// interaction accidentally looping forever.
        /// </summary>
        private void ResolveStunSkips()
        {
            const int maxIterations = 8;
            int iterations = 0;

            while (!_isBattleOver && iterations < maxIterations)
            {
                BuffManager currentSideBuffs = GetCharacter(_turnTracker.CurrentSide).Buffs;
                if (!currentSideBuffs.HasControlBuff(ControlBuffType.Stun))
                {
                    break;
                }

                currentSideBuffs.ConsumeStunCharge();
                _turnTracker.ForceAdvanceTurn();
                TickAllBuffsTurn();
                iterations++;
            }
        }

        private void EndBattle(BattleSide winningSide)
        {
            _isBattleOver = true;
            _inputController.SetExternalGate(false);
            _moveTimer.Stop();
            OnBattleEnded(winningSide);
        }

        private CharacterState GetCharacter(BattleSide side)
        {
            return side == BattleSide.Player ? _playerState : _enemyState;
        }

        private void TurnTracker_CycleCompleted(int cycleCount)
        {
            TickAllBuffsCycle();
        }

        private void TickAllBuffsTurn()
        {
            _playerState.Buffs.TickTurn();
            _enemyState.Buffs.TickTurn();
        }

        private void TickAllBuffsCycle()
        {
            _playerState.Buffs.TickCycle();
            _enemyState.Buffs.TickCycle();
        }

        private static AiDifficulty RollEnemyAiDifficulty()
        {
            return UnityEngine.Random.value < 0.5f ? AiDifficulty.Smart : AiDifficulty.Random;
        }

        private IMoveSelector CreateMoveSelector(AiDifficulty difficulty)
        {
            return difficulty == AiDifficulty.Smart
                ? new SmartMoveSelector(_tuningConfig.ExtraTurnMatchLength)
                : new RandomMoveSelector();
        }

        /// <summary>Spawns floating text for the instant (non-attack) effects of one resolve pass — HP/VHP heal, Mana gain, Shield gain — each only if it actually occurred.</summary>
        private void SpawnInstantEffectFloatingText(BattleSide side, BattleResolveOutcome outcome)
        {
            Vector3 position = _attackVisuals.GetView(side).TargetPoint;

            if (outcome.HpHealed > 0f)
            {
                _floatingText.Spawn(position, FloatingTextKind.HpHeal, Mathf.RoundToInt(outcome.HpHealed));
            }
            if (outcome.VhpHealed > 0f)
            {
                _floatingText.Spawn(position, FloatingTextKind.VhpHeal, Mathf.RoundToInt(outcome.VhpHealed));
            }
            if (outcome.ManaGained > 0f)
            {
                _floatingText.Spawn(position, FloatingTextKind.ManaGain, Mathf.RoundToInt(outcome.ManaGained));
            }
            if (outcome.ShieldCountGained > 0)
            {
                _floatingText.Spawn(position, FloatingTextKind.ShieldGain, outcome.ShieldCountGained);
            }
        }

        private void OnCharacterStatsChanged(BattleSide side)
        {
            UpdateDebugSnapshot(side);
            CharacterStatsChanged?.Invoke(side);
        }

        private void OnBattleEnded(BattleSide winningSide)
        {
            BattleEnded?.Invoke(winningSide);
        }

        private void OnSkillCast(BattleSide side, SkillDefinition skill)
        {
            SkillCast?.Invoke(side, skill);
        }

        private void UpdateDebugSnapshot(BattleSide side)
        {
            CharacterState state = GetCharacter(side);
            CharacterDebugSnapshot snapshot = new CharacterDebugSnapshot
            {
                CurrentHp = state.CurrentHp,
                MaxHp = state.MaxHp,
                CurrentVhp = state.CurrentVhp,
                MaxVhp = state.MaxVhp,
                Mana = state.Mana,
                MaxMana = state.Config.MaxMana,
                ShieldCount = state.ShieldCount,
                ShieldStack = state.ShieldStack
            };

            if (side == BattleSide.Player)
            {
                _playerDebugSnapshot = snapshot;
            }
            else
            {
                _enemyDebugSnapshot = snapshot;
            }
        }
    }
}
