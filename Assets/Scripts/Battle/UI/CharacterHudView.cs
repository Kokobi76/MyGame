using UnityEngine;
using TMPro;
using Match3.Battle.Model;
using Match3.Battle.Gameplay;

namespace Match3.Battle.UI
{
    /// <summary>
    /// Displays one character's full stat block: HP, VHP, Mana (all
    /// bars), plus Swordrain, Slash and Shield as text. Bind one instance
    /// per side (set <see cref="_side"/> accordingly) — one on the
    /// Player's HUD panel, one on the Enemy's. The same prefab works for
    /// both; only the Side field differs.
    /// </summary>
    public sealed class CharacterHudView : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private BattleController _battleController;
        [SerializeField] private BattleSide _side;

        [Header("Bars")]
        [SerializeField] private StatBarView _hpBar;
        [SerializeField] private StatBarView _vhpBar;
        [SerializeField] private StatBarView _manaBar;

        [Header("Text Readouts")]
        [SerializeField] private TMP_Text _swordrainText;
        [SerializeField] private TMP_Text _slashText;
        [SerializeField] private TMP_Text _shieldText;

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
            if (_isSubscribed || _battleController == null || GetState() == null)
            {
                return;
            }

            _battleController.CharacterStatsChanged += BattleController_CharacterStatsChanged;
            _isSubscribed = true;

            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _battleController.CharacterStatsChanged -= BattleController_CharacterStatsChanged;
            _isSubscribed = false;
        }

        private void BattleController_CharacterStatsChanged(BattleSide changedSide)
        {
            if (changedSide == _side)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            CharacterState state = GetState();
            if (state == null)
            {
                return;
            }

            if (_hpBar != null)
            {
                _hpBar.SetValue(state.CurrentHp, state.MaxHp);
            }
            if (_vhpBar != null)
            {
                _vhpBar.SetValue(state.CurrentVhp, state.MaxVhp);
            }
            if (_manaBar != null)
            {
                _manaBar.SetValue(state.Mana, state.Config.MaxMana);
            }
            if (_swordrainText != null)
            {
                _swordrainText.text = $"Swordrain: {state.SwordrainDamage:0}";
            }
            if (_slashText != null)
            {
                _slashText.text = $"Slash: {state.SlashDamage:0}";
            }
            if (_shieldText != null)
            {
                int shieldCountPerStack = _battleController.TuningConfig.ShieldCountPerStack;
                _shieldText.text = $"Shield: {state.ShieldStack} stack ({state.ShieldCount}/{shieldCountPerStack})";
            }
        }

        private CharacterState GetState()
        {
            return _side == BattleSide.Player ? _battleController.PlayerState : _battleController.EnemyState;
        }
    }
}
