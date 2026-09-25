using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Match3.Battle.Model;
using Match3.Battle.Gameplay;
using Match3.Battle.Skills;

namespace Match3.Battle.UI
{
    /// <summary>
    /// One of a character's (up to 3) active skill slot buttons. Reads
    /// which <see cref="SkillDefinition"/> occupies <see cref="_slotIndex"/>
    /// from that side's <see cref="SkillLoadout"/> (via
    /// <see cref="BattleController.GetSkillLoadout"/>), shows its name
    /// (and Mana cost, if any), casts it on click via
    /// <see cref="BattleController.TryCastSkill"/>, and disables itself
    /// whenever the cast wouldn't currently be legal (wrong turn, not
    /// enough Mana, mid-resolve, battle over, or an empty slot). Bind one
    /// instance per slot — 3 for Player, 3 for Enemy (Enemy's are handy
    /// for testing even though nothing plays them automatically yet) —
    /// the same prefab works for every slot, only Side and Slot Index
    /// differ, exactly like <see cref="CharacterHudView"/>'s Side field.
    ///
    /// There's no separate "test a raw Buff" UI: wrap the
    /// <see cref="Match3.Battle.Buffs.BuffDefinition"/> you want to try in
    /// a Buff Skill (Category = Buff Skill) and put it in a slot — Buff
    /// Skills cost no move and can be cast repeatedly as long as Mana
    /// allows, so this doubles as the buff-testing UI.
    /// </summary>
    public sealed class SkillSlotView : MonoBehaviour
    {
        [SerializeField] private BattleController _battleController;
        [SerializeField] private BattleSide _side;
        [SerializeField] [Min(0)] private int _slotIndex;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;

        private bool _isSubscribed;
        private SkillDefinition _boundSkill;

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
            if (_isSubscribed || _battleController == null || _battleController.TurnTracker == null)
            {
                return;
            }

            if (_button != null)
            {
                _button.onClick.AddListener(HandleButtonClicked);
            }
            _battleController.CharacterStatsChanged += BattleController_CharacterStatsChanged;
            _battleController.SkillCast += BattleController_SkillCast;
            _battleController.BattleEnded += BattleController_BattleEnded;
            _battleController.TurnTracker.SideChanged += TurnTracker_SideChanged;
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
            _battleController.CharacterStatsChanged -= BattleController_CharacterStatsChanged;
            _battleController.SkillCast -= BattleController_SkillCast;
            _battleController.BattleEnded -= BattleController_BattleEnded;
            _battleController.TurnTracker.SideChanged -= TurnTracker_SideChanged;
            _isSubscribed = false;
        }

        private void HandleButtonClicked()
        {
            if (_boundSkill == null)
            {
                return;
            }
            _battleController.TryCastSkill(_side, _boundSkill);
            // Refresh follows from the CharacterStatsChanged/SkillCast/SideChanged events this triggers.
        }

        private void BattleController_CharacterStatsChanged(BattleSide changedSide)
        {
            Refresh();
        }

        private void BattleController_SkillCast(BattleSide side, SkillDefinition skill)
        {
            Refresh();
        }

        private void BattleController_BattleEnded(BattleSide winningSide)
        {
            Refresh();
        }

        private void TurnTracker_SideChanged(BattleSide side)
        {
            Refresh();
        }

        private void Refresh()
        {
            SkillLoadout loadout = _battleController.GetSkillLoadout(_side);
            _boundSkill = loadout != null ? loadout.GetSlot(_slotIndex) : null;

            if (_button != null)
            {
                _button.interactable = _boundSkill != null && _battleController.CanCastSkill(_side, _boundSkill);
            }

            if (_label != null)
            {
                _label.text = _boundSkill == null
                    ? "-"
                    : (_boundSkill.RequiresMana ? $"{_boundSkill.DisplayName}\n{_boundSkill.ManaCost:0} MP" : _boundSkill.DisplayName);
            }
        }
    }
}
