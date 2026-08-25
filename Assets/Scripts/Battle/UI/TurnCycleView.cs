using UnityEngine;
using TMPro;
using Match3.Battle.Model;
using Match3.Battle.Gameplay;

namespace Match3.Battle.UI
{
    /// <summary>Shows turn count, cycle count, whose turn it currently is, and the Player's remaining move time.</summary>
    public sealed class TurnCycleView : MonoBehaviour
    {
        [SerializeField] private BattleController _battleController;

        [Header("Text Readouts")]
        [SerializeField] private TMP_Text _turnCountText;
        [SerializeField] private TMP_Text _cycleCountText;
        [SerializeField] private TMP_Text _currentSideText;
        [SerializeField] private TMP_Text _timerText;

        private bool _isSubscribed;

        private void OnEnable()
        {
            TrySubscribe();
        }

        // Unity does not guarantee BattleController.Awake() (which
        // creates TurnTracker/MoveTimer) has already run by the time this
        // component's own OnEnable fires — only that ALL Awake calls finish
        // before ANY Start call. Start() is the guaranteed-safe fallback;
        // OnEnable() alone is what makes re-showing this view (after a
        // disable/enable, e.g. a toggled panel) refresh correctly.
        private void Start()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void TrySubscribe()
        {
            if (_isSubscribed || _battleController == null || _battleController.TurnTracker == null || _battleController.MoveTimer == null)
            {
                return;
            }

            _battleController.TurnTracker.SideChanged += TurnTracker_SideChanged;
            _battleController.TurnTracker.CycleCompleted += TurnTracker_CycleCompleted;
            _battleController.MoveTimer.RemainingSecondsChanged += MoveTimer_RemainingSecondsChanged;
            _isSubscribed = true;

            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _battleController.TurnTracker.SideChanged -= TurnTracker_SideChanged;
            _battleController.TurnTracker.CycleCompleted -= TurnTracker_CycleCompleted;
            _battleController.MoveTimer.RemainingSecondsChanged -= MoveTimer_RemainingSecondsChanged;
            _isSubscribed = false;
        }

        private void TurnTracker_SideChanged(BattleSide side)
        {
            Refresh();
        }

        private void TurnTracker_CycleCompleted(int cycleCount)
        {
            Refresh();
        }

        private void MoveTimer_RemainingSecondsChanged(float remainingSeconds)
        {
            if (_timerText == null)
            {
                return;
            }
            _timerText.text = remainingSeconds > 0f ? $"{remainingSeconds:0.0}s" : "-";
        }

        private void Refresh()
        {
            if (_turnCountText != null)
            {
                _turnCountText.text = $"Turn {_battleController.TurnTracker.TurnCount}";
            }
            if (_cycleCountText != null)
            {
                _cycleCountText.text = $"Cycle {_battleController.TurnTracker.CycleCount}";
            }
            if (_currentSideText != null)
            {
                _currentSideText.text = $"{_battleController.TurnTracker.CurrentSide}'s turn";
            }
        }
    }
}
