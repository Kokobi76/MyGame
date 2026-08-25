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
