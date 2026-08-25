using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Match3.View;

namespace Match3.InputHandling
{
    /// <summary>
    /// Detects a mouse/touch drag from one tile toward a neighbor and
    /// raises <see cref="SwapRequested"/> with the two grid cells
    /// involved. Uses the grid's known geometry (via <see cref="BoardView"/>)
    /// to resolve screen position straight to a cell, so tiles do not need
    /// colliders.
    ///
    /// Pointer reading is isolated to <see cref="TryGetPointerDownThisFrame"/>
    /// and <see cref="TryGetPointerUpThisFrame"/>, compiled against
    /// whichever backend the project has enabled under Player Settings >
    /// Active Input Handling: the Input System package when Unity defines
    /// <c>ENABLE_INPUT_SYSTEM</c>, otherwise the legacy Input Manager. No
    /// other member of this class needs to know which one is active, so
    /// this file works unmodified whether the project is set to "Input
    /// System Package (New)", "Input Manager (Old)", or "Both".
    /// </summary>
    public sealed class BoardInputController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardView _boardView;
        [SerializeField] private Camera _inputCamera;

        [Header("Settings")]
        [SerializeField] private float _dragThresholdPixels = 30f;

        private bool _isDragging;
        private bool _isBoardIdle = true;
        private bool _isExternallyAllowed = true;
        private float _distanceToBoardPlane;
        private Vector2 _dragStartScreenPosition;
        private Vector2Int _dragStartGridPosition;

        private bool IsInputEnabled => _isBoardIdle && _isExternallyAllowed;

        public event Action<Vector2Int, Vector2Int> SwapRequested;

        private void Awake()
        {
            if (_inputCamera == null)
            {
                _inputCamera = Camera.main;
            }

            if (_inputCamera != null && _boardView != null)
            {
                _distanceToBoardPlane = Mathf.Abs(_inputCamera.transform.position.z - _boardView.transform.position.z);
            }
        }

        private void Update()
        {
            if (!IsInputEnabled)
            {
                _isDragging = false;
                return;
            }

            if (TryGetPointerDownThisFrame(out Vector2 downPosition))
            {
                TryBeginDrag(downPosition);
            }
            else if (_isDragging && TryGetPointerUpThisFrame(out Vector2 upPosition))
            {
                TryEndDrag(upPosition);
            }
        }

        /// <summary>Called by <see cref="Match3.Gameplay.BoardController"/> based on the board's Idle/Swapping/Resolving state.</summary>
        public void SetInputEnabled(bool isEnabled)
        {
            _isBoardIdle = isEnabled;
            if (!IsInputEnabled)
            {
                _isDragging = false;
            }
        }

        /// <summary>
        /// Independent extra gate for systems built on top of the board
        /// (e.g. a turn-based layer that only lets one side act at a
        /// time). Combined with <see cref="SetInputEnabled"/> via AND, so
        /// either caller can block input without needing to know about
        /// the other's reason for doing so.
        /// </summary>
        public void SetExternalGate(bool isAllowed)
        {
            _isExternallyAllowed = isAllowed;
            if (!IsInputEnabled)
            {
                _isDragging = false;
            }
        }

        private void TryBeginDrag(Vector2 screenPosition)
        {
            if (!TryGetGridPosition(screenPosition, out Vector2Int gridPosition))
            {
                return;
            }

            _isDragging = true;
            _dragStartScreenPosition = screenPosition;
            _dragStartGridPosition = gridPosition;
        }

        private void TryEndDrag(Vector2 screenPosition)
        {
            _isDragging = false;

            Vector2 dragDelta = screenPosition - _dragStartScreenPosition;
            if (dragDelta.magnitude < _dragThresholdPixels)
            {
                return;
            }

            Vector2Int direction = GetDominantDirection(dragDelta);
            Vector2Int targetGridPosition = _dragStartGridPosition + direction;

            OnSwapRequested(_dragStartGridPosition, targetGridPosition);
        }

        private bool TryGetGridPosition(Vector2 screenPosition, out Vector2Int gridPosition)
        {
            gridPosition = default;
            if (_boardView == null || _inputCamera == null)
            {
                return false;
            }

            Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, _distanceToBoardPlane);
            Vector3 worldPosition = _inputCamera.ScreenToWorldPoint(screenPoint);
            return _boardView.TryGetGridPosition(worldPosition, out gridPosition);
        }

        private static Vector2Int GetDominantDirection(Vector2 dragDelta)
        {
            if (Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y))
            {
                return dragDelta.x > 0 ? Vector2Int.right : Vector2Int.left;
            }
            return dragDelta.y > 0 ? Vector2Int.up : Vector2Int.down;
        }

        private void OnSwapRequested(Vector2Int from, Vector2Int to)
        {
            SwapRequested?.Invoke(from, to);
        }

#if ENABLE_INPUT_SYSTEM
        private static bool TryGetPointerDownThisFrame(out Vector2 position)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }
            position = default;
            return false;
        }

        private static bool TryGetPointerUpThisFrame(out Vector2 position)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }
            position = default;
            return false;
        }
#else
        private static bool TryGetPointerDownThisFrame(out Vector2 position)
        {
            if (Input.GetMouseButtonDown(0))
            {
                position = Input.mousePosition;
                return true;
            }
            position = default;
            return false;
        }

        private static bool TryGetPointerUpThisFrame(out Vector2 position)
        {
            if (Input.GetMouseButtonUp(0))
            {
                position = Input.mousePosition;
                return true;
            }
            position = default;
            return false;
        }
#endif
    }
}
