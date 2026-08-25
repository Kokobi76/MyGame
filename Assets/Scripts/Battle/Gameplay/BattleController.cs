using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    /// damage only once each object actually connects — and drives turn
    /// order, including automated moves (Enemy always; Player when
    /// auto-play is enabled) and the Player's move timer. Player always
    /// moves first.
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
        [Header("Core Board")]
        [SerializeField] private BoardController _boardController;
        [SerializeField] private BoardInputController _inputController;

        [Header("Battle Configuration")]
        [SerializeField] private BattleTileConfig _tileConfig;
        [SerializeField] private BattleTuningConfig _tuningConfig;
        [SerializeField] private CharacterConfig _playerConfig;
        [SerializeField] private CharacterConfig _enemyConfig;

        [Header("Attack Visuals")]
        [SerializeField] private AttackVisualController _attackVisuals;

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
        private bool _extraMoveEarnedThisTurn;
        private bool _isBattleOver;

        public CharacterState PlayerState => _playerState;
        public CharacterState EnemyState => _enemyState;
        public TurnCycleTracker TurnTracker => _turnTracker;
        public MoveTimer MoveTimer => _moveTimer;
        public AiDifficulty EnemyAiDifficulty { get; private set; }
        public bool IsPlayerAutoPlayEnabled => _isPlayerAutoPlayEnabled;
        public BattleTuningConfig TuningConfig => _tuningConfig;

        public event Action<BattleSide> CharacterStatsChanged;
        public event Action<BattleSide> BattleEnded;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            _playerState = new CharacterState(_playerConfig);
            _enemyState = new CharacterState(_enemyConfig);
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
        }

        private void OnDisable()
        {
            _boardController.MatchesResolved -= BoardController_MatchesResolved;
            _boardController.CascadeCompleted -= BoardController_CascadeCompleted;
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
            _extraMoveEarnedThisTurn |= outcome.GrantsExtraMove;

            OnCharacterStatsChanged(actingSide);

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

            _turnTracker.CompleteMove(_extraMoveEarnedThisTurn);
            _extraMoveEarnedThisTurn = false;

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
                yield return _attackVisuals.PlayMeleeAttack(attack.AttackerSide, () => ApplyAttackDamage(defender, attack.TotalDamage));
                yield break;
            }

            float perObjectDamage = attack.TotalDamage / Mathf.Max(attack.ObjectCount, 1);
            if (attack.Kind == AttackKind.Slash)
            {
                yield return _attackVisuals.PlayRangedSlashAttack(attack.AttackerSide, attack.ObjectCount, () => ApplyAttackDamage(defender, perObjectDamage));
            }
            else
            {
                yield return _attackVisuals.PlaySwordAttack(attack.AttackerSide, attack.ObjectCount, () => ApplyAttackDamage(defender, perObjectDamage));
            }
        }

        private void ApplyAttackDamage(CharacterState defender, float damage)
        {
            if (_isBattleOver)
            {
                return;
            }

            defender.TakeDamage(damage);
            BattleSide defenderSide = defender == _playerState ? BattleSide.Player : BattleSide.Enemy;
            OnCharacterStatsChanged(defenderSide);

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

            float penaltyDamage = Mathf.Max(_enemyConfig.SwordrainDamage, _enemyConfig.SlashDamage);
            _playerState.TakeUnblockableDamage(penaltyDamage);
            OnCharacterStatsChanged(BattleSide.Player);

            if (_playerState.IsDefeated)
            {
                EndBattle(BattleSide.Enemy);
                return;
            }

            _turnTracker.ForceAdvanceTurn();
            ApplyInputGate();

            if (IsCurrentSideAutomated())
            {
                StartCoroutine(TakeAutomatedTurnRoutine());
            }
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

        private void OnCharacterStatsChanged(BattleSide side)
        {
            UpdateDebugSnapshot(side);
            CharacterStatsChanged?.Invoke(side);
        }

        private void OnBattleEnded(BattleSide winningSide)
        {
            BattleEnded?.Invoke(winningSide);
        }

        private void UpdateDebugSnapshot(BattleSide side)
        {
            CharacterState state = GetCharacter(side);
            CharacterDebugSnapshot snapshot = new CharacterDebugSnapshot
            {
                CurrentHp = state.CurrentHp,
                MaxHp = state.Config.MaxHp,
                CurrentVhp = state.CurrentVhp,
                MaxVhp = state.Config.MaxVhp,
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
