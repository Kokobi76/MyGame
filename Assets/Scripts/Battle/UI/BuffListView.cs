using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using Match3.Battle.Model;
using Match3.Battle.Buffs;
using Match3.Battle.Gameplay;

namespace Match3.Battle.UI
{
    /// <summary>
    /// Lists one character's currently active Buffs/Debuffs/Effects — a
    /// pure monitoring display (no interaction; casting/removing buffs
    /// isn't done from here). Shows each buff's name, remaining
    /// Turn/Cycle count if it has one, and stack count if &gt;1. Bind one
    /// instance per side, same reusable-prefab pattern as
    /// <see cref="CharacterHudView"/>. Uses TMP rich-text color tags
    /// (Rich Text must stay enabled on the assigned Text component — the
    /// TMP default) to show negative buffs in red and positive ones in
    /// green.
    /// </summary>
    public sealed class BuffListView : MonoBehaviour
    {
        [SerializeField] private BattleController _battleController;
        [SerializeField] private BattleSide _side;
        [SerializeField] private TMP_Text _listText;
        [SerializeField] private string _emptyText = "-";
        [SerializeField] private Color _negativeColor = new Color(0.9f, 0.29f, 0.3f);
        [SerializeField] private Color _positiveColor = new Color(0.3f, 0.85f, 0.4f);

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
            if (_isSubscribed || _battleController == null)
            {
                return;
            }

            _battleController.BuffsChanged += BattleController_BuffsChanged;
            _isSubscribed = true;

            Refresh();
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _battleController.BuffsChanged -= BattleController_BuffsChanged;
            _isSubscribed = false;
        }

        private void BattleController_BuffsChanged(BattleSide changedSide)
        {
            if (changedSide == _side)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_listText == null)
            {
                return;
            }

            CharacterState state = GetState();
            if (state == null)
            {
                return;
            }

            IReadOnlyList<BuffInstance> activeBuffs = state.Buffs.ActiveBuffs;
            if (activeBuffs.Count == 0)
            {
                _listText.text = _emptyText;
                return;
            }

            StringBuilder builder = new StringBuilder();
            foreach (BuffInstance buff in activeBuffs)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }
                builder.Append(FormatBuffLine(buff));
            }
            _listText.text = builder.ToString();
        }

        private string FormatBuffLine(BuffInstance buff)
        {
            string name = buff.Definition.DisplayName;
            string stackSuffix = buff.StackCount > 1 ? $" x{buff.StackCount}" : string.Empty;
            string durationSuffix = FormatDurationSuffix(buff);
            Color color = buff.Definition.IsNegative ? _negativeColor : _positiveColor;
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{colorHex}>{name}{stackSuffix}{durationSuffix}</color>";
        }

        private static string FormatDurationSuffix(BuffInstance buff)
        {
            switch (buff.Definition.GetDuration().Type)
            {
                case BuffDurationType.Turn:
                    return $" ({buff.RemainingCount}t)";
                case BuffDurationType.Cycle:
                    return $" ({buff.RemainingCount}c)";
                case BuffDurationType.Permanence:
                    return " (\u221e)";
                default:
                    return string.Empty;
            }
        }

        private CharacterState GetState()
        {
            return _side == BattleSide.Player ? _battleController.PlayerState : _battleController.EnemyState;
        }
    }
}
