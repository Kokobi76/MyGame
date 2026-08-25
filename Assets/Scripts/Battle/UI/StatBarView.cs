using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Match3.Battle.UI
{
    /// <summary>
    /// One labeled bar: a <see cref="Slider"/> whose fill reflects
    /// current/max, plus an optional "123 / 456" text readout. Reusable
    /// for HP, VHP, or any future current/max stat — bind one instance
    /// per bar and drive it from <see cref="CharacterHudView"/>.
    /// </summary>
    public sealed class StatBarView : MonoBehaviour
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private string _valueFormat = "{0:0} / {1:0}";

        public void SetValue(float current, float max)
        {
            if (_slider != null)
            {
                _slider.maxValue = Mathf.Max(max, 0.0001f);
                _slider.value = current;
            }

            if (_valueText != null)
            {
                _valueText.text = string.Format(_valueFormat, current, max);
            }
        }
    }
}
