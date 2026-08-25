using UnityEngine;
using UnityEngine.UI;
using Match3.Battle.Gameplay;

namespace Match3.Battle.UI
{
    /// <summary>Toggle that turns the Player's auto-play mode on/off via <see cref="BattleController.SetPlayerAutoPlay"/>.</summary>
    public sealed class AutoPlayToggleView : MonoBehaviour
    {
        [SerializeField] private BattleController _battleController;
        [SerializeField] private Toggle _toggle;

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
            if (_isSubscribed || _toggle == null || _battleController == null)
            {
                return;
            }

            _toggle.SetIsOnWithoutNotify(_battleController.IsPlayerAutoPlayEnabled);
            _toggle.onValueChanged.AddListener(HandleToggleValueChanged);
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
            _isSubscribed = false;
        }

        private void HandleToggleValueChanged(bool isEnabled)
        {
            _battleController.SetPlayerAutoPlay(isEnabled);
        }
    }
}
