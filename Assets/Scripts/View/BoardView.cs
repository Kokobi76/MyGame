using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Match3.Data;
using Match3.Model;
using Match3.Collapse;

namespace Match3.View
{
    /// <summary>
    /// Owns every <see cref="TileView"/> currently on screen, keeps them
    /// in lockstep with wherever the corresponding <see cref="BoardModel"/>
    /// state ends up, and builds the DOTween sequences that animate each
    /// gameplay step. Knows nothing about matching or turn rules — it
    /// only turns grid deltas into motion.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BoardConfig _config;
        [SerializeField] private TileView _tilePrefab;
        [SerializeField] private Transform _tileContainer;

        [Header("Pool Settings")]
        [SerializeField] private int _poolDefaultCapacity = 64;
        [SerializeField] private int _poolMaxSize = 256;

        private TileView[,] _tileViews;
        private TileViewPool _tileViewPool;

        private void Awake()
        {
            if (_config == null || _tilePrefab == null)
            {
                Debug.LogError("BoardView requires a BoardConfig and a TileView prefab to be assigned.", this);
                enabled = false;
                return;
            }

            EnsureTileContainer();
            _tileViewPool = new TileViewPool(_tilePrefab, _tileContainer, _poolDefaultCapacity, _poolMaxSize);
            _tileViews = new TileView[_config.Width, _config.Height];
        }

        public Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            return GetBoardOrigin() + new Vector3(gridPosition.x * _config.CellSize, gridPosition.y * _config.CellSize, 0f);
        }

        /// <summary>Inverse of <see cref="GetWorldPosition"/>, used by input to turn a click/tap into a grid cell.</summary>
        public bool TryGetGridPosition(Vector3 worldPosition, out Vector2Int gridPosition)
        {
            Vector3 localPosition = worldPosition - GetBoardOrigin();
            int x = Mathf.RoundToInt(localPosition.x / _config.CellSize);
            int y = Mathf.RoundToInt(localPosition.y / _config.CellSize);
            gridPosition = new Vector2Int(x, y);
            return IsWithinBounds(gridPosition);
        }

        /// <summary>Spawns a view for every filled cell of the given board and plays its spawn-in tween.</summary>
        public Sequence SpawnInitialBoard(BoardModel board)
        {
            Sequence sequence = DOTween.Sequence();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    Vector2Int gridPosition = new Vector2Int(x, y);
                    Tile tile = board.GetTile(gridPosition);
                    if (tile == null)
                    {
                        continue;
                    }

                    TileView tileView = CreateTileViewAt(gridPosition, tile.TypeId, GetWorldPosition(gridPosition));
                    sequence.Join(tileView.AnimateSpawn(_config.SpawnDuration));
                }
            }
            return sequence;
        }

        /// <summary>
        /// Animates whatever views currently occupy cells a/b swapping
        /// places. Calling this a second time with the same positions
        /// animates the swap back, mirroring <see cref="BoardModel.SwapTiles"/>.
        /// </summary>
        public Sequence AnimateSwap(Vector2Int a, Vector2Int b)
        {
            TileView tileViewA = GetTileView(a);
            TileView tileViewB = GetTileView(b);

            Vector3 worldPositionA = GetWorldPosition(a);
            Vector3 worldPositionB = GetWorldPosition(b);

            Sequence sequence = DOTween.Sequence();
            if (tileViewA != null)
            {
                sequence.Join(tileViewA.AnimateMoveTo(worldPositionB, _config.SwapDuration));
            }
            if (tileViewB != null)
            {
                sequence.Join(tileViewB.AnimateMoveTo(worldPositionA, _config.SwapDuration));
            }

            SetTileView(a, tileViewB);
            SetTileView(b, tileViewA);

            return sequence;
        }

        /// <summary>Plays the removal tween for every matched position and returns the view to the pool once it finishes.</summary>
        public Sequence AnimateMatchedRemoval(IEnumerable<Vector2Int> positions)
        {
            Sequence sequence = DOTween.Sequence();
            foreach (Vector2Int position in positions)
            {
                TileView tileView = GetTileView(position);
                if (tileView == null)
                {
                    continue;
                }

                SetTileView(position, null);
                sequence.Join(tileView.AnimateRemoval(_config.DestroyDuration).OnComplete(() => _tileViewPool.Release(tileView)));
            }
            return sequence;
        }

        /// <summary>Animates existing tiles falling into their new slots and new tiles dropping in from above the board.</summary>
        public Sequence AnimateCollapseAndRefill(CollapseResult collapseResult)
        {
            Sequence sequence = DOTween.Sequence();

            foreach (TileMovement movement in collapseResult.Movements)
            {
                TileView tileView = GetTileView(movement.FromPosition);
                if (tileView == null)
                {
                    continue;
                }

                SetTileView(movement.FromPosition, null);
                SetTileView(movement.ToPosition, tileView);

                Vector3 targetWorldPosition = GetWorldPosition(movement.ToPosition);
                sequence.Join(tileView.AnimateMoveTo(targetWorldPosition, _config.FallDuration));
            }

            foreach (TileSpawn spawn in collapseResult.Spawns)
            {
                Vector3 spawnWorldPosition = GetWorldPosition(spawn.ToPosition) + new Vector3(0f, spawn.DropRowCount * _config.CellSize, 0f);
                TileView tileView = CreateTileViewAt(spawn.ToPosition, spawn.Tile.TypeId, spawnWorldPosition);

                Vector3 targetWorldPosition = GetWorldPosition(spawn.ToPosition);
                sequence.Join(tileView.AnimateMoveTo(targetWorldPosition, _config.FallDuration));
            }

            return sequence;
        }

        /// <summary>Releases every currently visible tile back to the pool. Used before a deadlock reshuffle.</summary>
        public Sequence AnimateClearBoard()
        {
            Sequence sequence = DOTween.Sequence();
            for (int y = 0; y < _config.Height; y++)
            {
                for (int x = 0; x < _config.Width; x++)
                {
                    Vector2Int gridPosition = new Vector2Int(x, y);
                    TileView tileView = GetTileView(gridPosition);
                    if (tileView == null)
                    {
                        continue;
                    }

                    SetTileView(gridPosition, null);
                    sequence.Join(tileView.AnimateRemoval(_config.DestroyDuration).OnComplete(() => _tileViewPool.Release(tileView)));
                }
            }
            return sequence;
        }

        /// <summary>
        /// World position of grid cell (0,0) — the board's bottom-left
        /// corner. Computed fresh from <see cref="BoardConfig"/> every
        /// call (Width, Height, CellSize, BoardOffset) so the board is
        /// always centered on this GameObject's position plus the
        /// configured offset, with no manual repositioning needed when
        /// those values change (e.g. a different board size per level).
        /// </summary>
        private Vector3 GetBoardOrigin()
        {
            Vector3 halfExtents = new Vector3((_config.Width - 1) * 0.5f * _config.CellSize, (_config.Height - 1) * 0.5f * _config.CellSize, 0f);
            Vector3 offset = new Vector3(_config.BoardOffset.x, _config.BoardOffset.y, 0f);
            return transform.position + offset - halfExtents;
        }

        private TileView CreateTileViewAt(Vector2Int gridPosition, int typeId, Vector3 worldPosition)
        {
            TileTypeData tileTypeData = _config.GetTileTypeData(typeId);
            TileView tileView = _tileViewPool.Get();
            tileView.Initialize(typeId, tileTypeData.Sprite, tileTypeData.Color);
            tileView.transform.position = worldPosition;
            SetTileView(gridPosition, tileView);
            return tileView;
        }

        private TileView GetTileView(Vector2Int gridPosition)
        {
            return IsWithinBounds(gridPosition) ? _tileViews[gridPosition.x, gridPosition.y] : null;
        }

        private void SetTileView(Vector2Int gridPosition, TileView tileView)
        {
            if (!IsWithinBounds(gridPosition))
            {
                return;
            }
            _tileViews[gridPosition.x, gridPosition.y] = tileView;
        }

        private bool IsWithinBounds(Vector2Int gridPosition)
        {
            return gridPosition.x >= 0 && gridPosition.x < _config.Width &&
                   gridPosition.y >= 0 && gridPosition.y < _config.Height;
        }

        private void EnsureTileContainer()
        {
            if (_tileContainer != null)
            {
                return;
            }

            GameObject containerObject = new GameObject("Tiles");
            containerObject.transform.SetParent(transform);
            containerObject.transform.localPosition = Vector3.zero;
            _tileContainer = containerObject.transform;
        }
    }
}
