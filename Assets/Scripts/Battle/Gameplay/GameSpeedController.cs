using System;
using UnityEngine;

namespace Match3.Battle.Gameplay
{
    /// <summary>
    /// Cycles the game's playback speed 1x -&gt; 2x -&gt; 4x -&gt; 1x via
    /// <see cref="Time.timeScale"/>, which DOTween's animations and any
    /// <c>WaitForSeconds</c>-based coroutine delay (e.g. the AI's move
    /// pacing) respect automatically. Deliberately standalone — nothing
    /// else needs to reference this for the battle to function; it only
    /// exists to fast-forward what the player is watching.
    ///
    /// The player's own <see cref="Match3.Battle.Turn.MoveTimer"/> is
    /// ticked with unscaled time specifically so it is NOT affected by
    /// this — speeding up playback shouldn't shrink the player's real
    /// thinking window.
    /// </summary>
    public sealed class GameSpeedController : MonoBehaviour
    {
        private static readonly float[] SpeedSteps = { 1f, 2f, 4f };

        private int _currentStepIndex;

        public float CurrentSpeedMultiplier => SpeedSteps[_currentStepIndex];

        public event Action<float> SpeedChanged;

        private void Awake()
        {
            _currentStepIndex = 0;
            ApplySpeed();
        }

        private void OnDestroy()
        {
            // Restore normal speed so leaving this scene/object never
            // leaves the rest of the game stuck fast-forwarded.
            Time.timeScale = 1f;
        }

        /// <summary>Advances to the next step (1x -> 2x -> 4x -> 1x). Call this from a UI button.</summary>
        public void CycleSpeed()
        {
            _currentStepIndex = (_currentStepIndex + 1) % SpeedSteps.Length;
            ApplySpeed();
        }

        private void ApplySpeed()
        {
            Time.timeScale = CurrentSpeedMultiplier;
            OnSpeedChanged(CurrentSpeedMultiplier);
        }

        private void OnSpeedChanged(float multiplier)
        {
            SpeedChanged?.Invoke(multiplier);
        }
    }
}
