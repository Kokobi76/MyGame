using System.Collections;
using DG.Tweening;

namespace Match3.Utilities
{
    /// <summary>
    /// Bridges DOTween tweens into Unity coroutines. Deliberately does
    /// NOT use DOTween's own <c>Tween.WaitForCompletion()</c>, which is a
    /// blocking/synchronous helper not meant for coroutine use. Instead
    /// this flips a flag from <c>OnComplete</c>/<c>OnKill</c> and polls it
    /// once per frame, which is safe regardless of DOTween version.
    /// </summary>
    public static class TweenCoroutineExtensions
    {
        public static IEnumerator AsCoroutine(this Tween tween)
        {
            if (tween == null)
            {
                yield break;
            }

            bool isDone = false;
            tween.OnComplete(() => isDone = true);
            tween.OnKill(() => isDone = true);

            while (!isDone)
            {
                yield return null;
            }
        }
    }
}
