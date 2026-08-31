using System;
using UnityEngine;
using DG.Tweening;
using TMPro;

namespace Match3.Battle.View
{
    /// <summary>
    /// One floating combat text instance: rises and fades out, then
    /// invokes a completion callback so the pool can reclaim it. Uses
    /// <see cref="DOVirtual.Float"/> to drive the fade manually (setting
    /// alpha frame by frame) rather than a TMP-specific DOTween shortcut
    /// like <c>DOFade</c>, since those require the DOTween Pro / TMP
    /// integration module which isn't guaranteed to be installed —
    /// <see cref="DOVirtual"/> is part of core DOTween.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class FloatingTextView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _riseDistance = 1f;
        [SerializeField] private float _duration = 0.8f;

        private void Awake()
        {
            if (_text == null)
            {
                _text = GetComponent<TMP_Text>();
            }
        }

        public void Play(string label, Color color, Action onComplete)
        {
            _text.text = label;
            SetAlpha(color, 1f);

            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + (Vector3.up * _riseDistance);

            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOMove(endPosition, _duration).SetEase(Ease.OutQuad));
            sequence.Join(DOVirtual.Float(1f, 0f, _duration, alpha => SetAlpha(color, alpha)).SetEase(Ease.InQuad));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>Called by the pool before a recycled instance is reused, so no stale tween lingers on it.</summary>
        public void ResetVisualState()
        {
            transform.DOKill();
            _text.DOKill();
        }

        private void SetAlpha(Color baseColor, float alpha)
        {
            _text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
    }
}
