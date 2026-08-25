using System;
using UnityEngine;

namespace Match3.Battle.Turn
{
    /// <summary>
    /// A restartable countdown, ticked externally (from
    /// <see cref="Match3.Battle.Gameplay.BattleController.Update"/> using
    /// <c>Time.unscaledDeltaTime</c>) so the player's real thinking time
    /// stays constant regardless of any game-speed multiplier applied to
    /// animations via <see cref="Match3.Battle.Gameplay.GameSpeedController"/>.
    /// </summary>
    public sealed class MoveTimer
    {
        public float RemainingSeconds { get; private set; }
        public bool IsRunning { get; private set; }

        public event Action Expired;
        public event Action<float> RemainingSecondsChanged;

        public void Start(float durationSeconds)
        {
            RemainingSeconds = durationSeconds;
            IsRunning = true;
            OnRemainingSecondsChanged(RemainingSeconds);
        }

        public void Stop()
        {
            IsRunning = false;
            RemainingSeconds = 0f;
            OnRemainingSecondsChanged(RemainingSeconds);
        }

        public void Tick(float deltaSeconds)
        {
            if (!IsRunning)
            {
                return;
            }

            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - deltaSeconds);
            OnRemainingSecondsChanged(RemainingSeconds);

            if (RemainingSeconds <= 0f)
            {
                IsRunning = false;
                OnExpired();
            }
        }

        private void OnExpired()
        {
            Expired?.Invoke();
        }

        private void OnRemainingSecondsChanged(float remainingSeconds)
        {
            RemainingSecondsChanged?.Invoke(remainingSeconds);
        }
    }
}
