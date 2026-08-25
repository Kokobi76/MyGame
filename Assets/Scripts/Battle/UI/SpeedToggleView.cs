using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Match3.Battle.Gameplay;

namespace Match3.Battle.UI
{
    /// <summary>Button that cycles game speed 1x -&gt; 2x -&gt; 4x -&gt; 1x via <see cref="GameSpeedController"/>.</summary>
    public sealed class SpeedToggleView : MonoBehaviour
    {
        [SerializeField] private GameSpeedController _speedController;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;

        private bool _isSubscribed;

        private void OnEnable()
        {
            TrySubscribe();
        }

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
            if (_isSubscribed || _speedController == null)
            {
                return;
            }

            if (_button != null)
            {
                _button.onClick.AddListener(HandleButtonClicked);
            }
            _speedController.SpeedChanged += SpeedController_SpeedChanged;
            _isSubscribed = true;

            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleButtonClicked);
            }
            _speedController.SpeedChanged -= SpeedController_SpeedChanged;
            _isSubscribed = false;
        }

        private void HandleButtonClicked()
        {
            _speedController.CycleSpeed();
        }

        private void SpeedController_SpeedChanged(float multiplier)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_label != null)
            {
                _label.text = $"x{_speedController.CurrentSpeedMultiplier:0}";
            }
        }
    }
}
