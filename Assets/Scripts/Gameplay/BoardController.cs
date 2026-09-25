using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Match3.Data;
using Match3.Model;
using Match3.Generation;
using Match3.Matching;
using Match3.Swap;
using Match3.Collapse;
using Match3.StateMachine;
using Match3.View;
using Match3.InputHandling;
using Match3.Utilities;

namespace Match3.Gameplay
{
    /// <summary>
    /// The board's presenter: owns the model and the services that
    /// operate on it, reacts to input, and drives the gameplay loop
    /// (generate -> idle -> swap -> resolve cascades -> idle) with
    /// coroutines. Contains no matching/collapse/generation logic itself
    /// — it only sequences calls to the single-responsibility services
    /// that do, and keeps the view in sync.
    /// </summary>
    public sealed class BoardController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BoardConfig _config;

        [Header("References")]
        [SerializeField] private BoardView _boardView;
        [SerializeField] private BoardInputController _inputController;

        private BoardModel _boardModel;
        private IBoardGenerator _boardGenerator;
        private IMatchFinder _matchFinder;
        private ISwapValidator _swapValidator;
        private ICollapseResolver _collapseResolver;

        private BoardStateMachine _stateMachine;
        private IBoardState _idleState;
        private IBoardState _swappingState;
        private IBoardState _resolvingState;

        /// <summary>Read-only view of the live board state, for AI move selection, UI, or debug tooling.</summary>
        public IReadOnlyBoardModel Board => _boardModel;

        /// <summary>Whether the board is currently idle (not mid-swap or mid-resolve). Used by Battle-layer code (e.g. Skill casting) that needs to know the board itself isn't busy before starting something new.</summary>
        public bool IsIdle => _stateMachine.CurrentState is IdleState;

        public event Action BoardGenerated;
        public event Action<Vector2Int, Vector2Int> TilesSwapped;
        public event Action<MatchSearchResult> MatchesResolved;
        public event Action CascadeCompleted;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            InitializeServices();
            InitializeStateMachine();
        }

        private void Start()
        {
            StartCoroutine(InitializeBoardRoutine());
        }

        private void OnEnable()
        {
            _inputController.SwapRequested += InputController_SwapRequested;
        }

        private void OnDisable()
        {
            _inputController.SwapRequested -= InputController_SwapRequested;
        }

        /// <summary>
        /// Requests a swap between two adjacent cells. Safe to call from
        /// any source — player drag input, AI, or a scripted sequence —
        /// since it re-validates board state, adjacency and bounds before
        /// acting, and silently no-ops if the request isn't currently
        /// legal (e.g. the board is mid-animation).
        /// </summary>
        public void RequestSwap(Vector2Int from, Vector2Int to)
        {
            if (!(_stateMachine.CurrentState is IdleState))
            {
                return;
            }

            // Callers only ever propose an orthogonal neighbor, but a
            // position near the edge of the grid can still point outside
            // it, so bounds must be re-checked here regardless of source.
            if (!_swapValidator.AreAdjacent(from, to) || !_boardModel.IsWithinBounds(to))
            {
                return;
            }

            StartCoroutine(HandleSwapRoutine(from, to));
        }

        /// <summary>Every currently legal swap on the board, each with its best resulting match length. Intended for move-selection (AI, hints), not the core resolve loop.</summary>
        public IReadOnlyList<EvaluatedMove> GetAllValidMoves()
        {
            return _swapValidator.GetAllValidMoves(_boardModel, _config);
        }

        /// <summary>
        /// Directly destroys the given cells with no match required —
        /// used by the Skill System's Tiles Skill Destroy Area effect
        /// (see <c>Match3.Battle.Skills</c>). Deliberately self-contained
        /// (its own removal/collapse/refill/cascade loop, not sharing
        /// code with <see cref="ResolveCascadeRoutine"/>) so that method
        /// and everything else in the normal swap path stay completely
        /// untouched by this addition. Cells with no tile (already empty)
        /// are silently ignored; a no-op if every given cell turns out
        /// empty. Fires <see cref="MatchesResolved"/> for every removal
        /// pass, same as a real match, so Battle-layer resolve code
        /// (HP/damage/etc. from whatever tile kinds were destroyed)
        /// applies identically either way — but deliberately does NOT
        /// fire <see cref="CascadeCompleted"/>: that event is reserved
        /// for the normal swap-triggered flow, which
        /// <c>Match3.Battle.Gameplay.BattleController</c> reacts to by
        /// completing a move/turn, and a skill-triggered destroy decides
        /// its own turn completion afterward instead (its Costs Move
        /// flag). The caller simply awaits this routine to know when the
        /// board is idle again.
        /// </summary>
        public IEnumerator DestroyPositionsRoutine(IReadOnlyList<Vector2Int> positions)
        {
            if (!(_stateMachine.CurrentState is IdleState) || positions == null || positions.Count == 0)
            {
                yield break;
            }

            MatchSearchResult forcedResult = BuildForcedDestroyResult(positions);
            if (!forcedResult.HasMatches)
            {
                yield break;
            }

            _stateMachine.TransitionTo(_resolvingState);

            yield return ClearAndCollapseOnce(forcedResult);

            while (true)
            {
                MatchSearchResult matchResult = _matchFinder.FindMatches(_boardModel, _config);
                if (!matchResult.HasMatches)
                {
                    break;
                }
                yield return ClearAndCollapseOnce(matchResult);
            }

            if (!_swapValidator.HasAnyValidMove(_boardModel, _config))
            {
                yield return ShuffleBoardRoutine();
            }

            _stateMachine.TransitionTo(_idleState);
        }

        private bool ValidateReferences()
        {
            if (_config == null)
            {
                Debug.LogError("BoardController requires a BoardConfig reference.", this);
                return false;
            }
            if (_boardView == null)
            {
                Debug.LogError("BoardController requires a BoardView reference.", this);
                return false;
            }
            if (_inputController == null)
            {
                Debug.LogError("BoardController requires a BoardInputController reference.", this);
                return false;
            }
            if (_config.TileTypes.Count < _config.MinMatchLength)
            {
                Debug.LogError("BoardConfig needs at least as many tile types as the minimum match length.", this);
                return false;
            }
            return true;
        }

        private void InitializeServices()
        {
            TileTypeRandomizer randomizer = new TileTypeRandomizer();
            _matchFinder = new MatchFinder();
            _boardGenerator = new BoardGenerator(randomizer, _matchFinder);
            _swapValidator = new SwapValidator(_matchFinder);
            _collapseResolver = new CollapseResolver(randomizer);
            _boardModel = new BoardModel(_config.Width, _config.Height);
        }

        private void InitializeStateMachine()
        {
            _idleState = new IdleState();
            _swappingState = new SwappingState();
            _resolvingState = new ResolvingState();

            _stateMachine = new BoardStateMachine();
            _stateMachine.StateChanged += StateMachine_StateChanged;
        }

        private IEnumerator InitializeBoardRoutine()
        {
            _boardGenerator.GenerateInitialBoard(_boardModel, _config);
            yield return _boardView.SpawnInitialBoard(_boardModel).AsCoroutine();

            OnBoardGenerated();
            _stateMachine.Initialize(_idleState);
        }

        private void InputController_SwapRequested(Vector2Int from, Vector2Int to)
        {
            RequestSwap(from, to);
        }

        private void StateMachine_StateChanged(IBoardState state)
        {
            _inputController.SetInputEnabled(state is IdleState);
        }

        private IEnumerator HandleSwapRoutine(Vector2Int from, Vector2Int to)
        {
            _stateMachine.TransitionTo(_swappingState);

            _boardModel.SwapTiles(from, to);
            yield return _boardView.AnimateSwap(from, to).AsCoroutine();

            bool resultsInMatch = _matchFinder.FindMatches(_boardModel, _config).HasMatches;
            if (!resultsInMatch)
            {
                _boardModel.SwapTiles(from, to);
                yield return _boardView.AnimateSwap(from, to).AsCoroutine();
                _stateMachine.TransitionTo(_idleState);
                yield break;
            }

            OnTilesSwapped(from, to);

            _stateMachine.TransitionTo(_resolvingState);
            yield return ResolveCascadeRoutine();
            _stateMachine.TransitionTo(_idleState);
        }

        private IEnumerator ResolveCascadeRoutine()
        {
            while (true)
            {
                MatchSearchResult matchResult = _matchFinder.FindMatches(_boardModel, _config);
                if (!matchResult.HasMatches)
                {
                    break;
                }

                yield return _boardView.AnimateMatchedRemoval(matchResult.AllMatchedPositions).AsCoroutine();
                foreach (Vector2Int position in matchResult.AllMatchedPositions)
                {
                    _boardModel.SetTile(position, null);
                }
                OnMatchesResolved(matchResult);

                CollapseResult collapseResult = _collapseResolver.ResolveCollapseAndRefill(_boardModel, _config);
                yield return _boardView.AnimateCollapseAndRefill(collapseResult).AsCoroutine();
            }

            if (!_swapValidator.HasAnyValidMove(_boardModel, _config))
            {
                yield return ShuffleBoardRoutine();
            }

            OnCascadeCompleted();
        }

        /// <summary>
        /// One removal+collapse+refill pass, used only by
        /// <see cref="DestroyPositionsRoutine"/> — a small, deliberate
        /// duplicate of the same 3 steps <see cref="ResolveCascadeRoutine"/>
        /// performs inline, kept as its own method purely so that
        /// existing method is never touched to add this feature.
        /// </summary>
        private IEnumerator ClearAndCollapseOnce(MatchSearchResult matchResult)
        {
            yield return _boardView.AnimateMatchedRemoval(matchResult.AllMatchedPositions).AsCoroutine();
            foreach (Vector2Int position in matchResult.AllMatchedPositions)
            {
                _boardModel.SetTile(position, null);
            }
            OnMatchesResolved(matchResult);

            CollapseResult collapseResult = _collapseResolver.ResolveCollapseAndRefill(_boardModel, _config);
            yield return _boardView.AnimateCollapseAndRefill(collapseResult).AsCoroutine();
        }

        /// <summary>Packages raw destroyed positions into the same MatchSearchResult shape a real match produces, grouped by TypeId, so Battle-layer resolve code (which only cares about "how many of each tile kind cleared") can't tell a forced destroy apart from a real match. Positions with no tile (already empty) are skipped. Orientation is set arbitrarily since nothing downstream reads it for a forced destroy.</summary>
        private MatchSearchResult BuildForcedDestroyResult(IReadOnlyList<Vector2Int> positions)
        {
            Dictionary<int, List<Vector2Int>> positionsByTypeId = new Dictionary<int, List<Vector2Int>>();
            foreach (Vector2Int position in positions)
            {
                Tile tile = _boardModel.GetTile(position);
                if (tile == null)
                {
                    continue;
                }

                if (!positionsByTypeId.TryGetValue(tile.TypeId, out List<Vector2Int> group))
                {
                    group = new List<Vector2Int>();
                    positionsByTypeId[tile.TypeId] = group;
                }
                group.Add(position);
            }

            List<MatchGroup> groups = new List<MatchGroup>();
            foreach (KeyValuePair<int, List<Vector2Int>> entry in positionsByTypeId)
            {
                groups.Add(new MatchGroup(entry.Value, entry.Key, MatchOrientation.Horizontal));
            }
            return new MatchSearchResult(groups);
        }

        private IEnumerator ShuffleBoardRoutine()
        {
            yield return _boardView.AnimateClearBoard().AsCoroutine();
            _boardGenerator.ShuffleBoard(_boardModel, _config);
            yield return _boardView.SpawnInitialBoard(_boardModel).AsCoroutine();
        }

        private void OnBoardGenerated()
        {
            BoardGenerated?.Invoke();
        }

        private void OnTilesSwapped(Vector2Int from, Vector2Int to)
        {
            TilesSwapped?.Invoke(from, to);
        }

        private void OnMatchesResolved(MatchSearchResult matchResult)
        {
            MatchesResolved?.Invoke(matchResult);
        }

        private void OnCascadeCompleted()
        {
            CascadeCompleted?.Invoke();
        }
    }
}
