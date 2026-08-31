using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using Match3.Utilities;
using Match3.Battle.Model;

namespace Match3.Battle.View
{
    /// <summary>
    /// Plays the visual for one resolved attack — melee lunge, ranged
    /// Slash volley, or Sword rain — and invokes a callback exactly when
    /// each object connects, so <see cref="Match3.Battle.Gameplay.BattleController"/>
    /// can time damage application to match ("an object must actually
    /// reach the opponent before HP is deducted", per design). Owns the
    /// pooled projectile objects used for ranged/Sword attacks.
    /// </summary>
    public sealed class AttackVisualController : MonoBehaviour
    {
        [Header("Characters")]
        [SerializeField] private CharacterView _playerView;
        [SerializeField] private CharacterView _enemyView;

        [Header("Projectile Pool")]
        [SerializeField] private AttackProjectileView _projectilePrefab;
        [SerializeField] private Transform _projectileContainer;
        [SerializeField] private int _poolDefaultCapacity = 16;
        [SerializeField] private int _poolMaxSize = 64;

        [Header("Volley Pacing")]
        [Tooltip("Delay between launching each object in a multi-object volley, purely for readability.")]
        [SerializeField] private float _volleyLaunchStaggerSeconds = 0.05f;

        private AttackProjectilePool _projectilePool;

        private void Awake()
        {
            EnsureProjectileContainer();
            _projectilePool = new AttackProjectilePool(_projectilePrefab, _projectileContainer, _poolDefaultCapacity, _poolMaxSize);
        }

        public CharacterView GetView(BattleSide side)
        {
            return side == BattleSide.Player ? _playerView : _enemyView;
        }

        /// <summary>Melee: the attacker lunges to the defender and back, invoking <paramref name="onImpact"/> once, at the moment of contact.</summary>
        public IEnumerator PlayMeleeAttack(BattleSide attackerSide, Action onImpact)
        {
            CharacterView attacker = GetView(attackerSide);
            CharacterView defender = GetView(attackerSide.GetOpposite());
            yield return attacker.PlayMeleeAttack(defender.TargetPoint, onImpact);
        }

        /// <summary>Ranged Slash: fires <paramref name="objectCount"/> objects from the attacker toward the defender, invoking <paramref name="onEachImpact"/> once per object as it lands.</summary>
        public IEnumerator PlayRangedSlashAttack(BattleSide attackerSide, int objectCount, Action onEachImpact)
        {
            CharacterView attacker = GetView(attackerSide);
            CharacterView defender = GetView(attackerSide.GetOpposite());
            yield return PlayVolley(attacker.ProjectileOrigin, defender.TargetPoint, objectCount, onEachImpact);
        }

        /// <summary>Sword: fires <paramref name="objectCount"/> objects from the attacker's designated Sword spawn point toward the defender, invoking <paramref name="onEachImpact"/> once per object as it lands.</summary>
        public IEnumerator PlaySwordAttack(BattleSide attackerSide, int objectCount, Action onEachImpact)
        {
            CharacterView attacker = GetView(attackerSide);
            CharacterView defender = GetView(attackerSide.GetOpposite());
            yield return PlayVolley(attacker.SwordSpawnPosition, defender.TargetPoint, objectCount, onEachImpact);
        }

        /// <summary>
        /// Launches <paramref name="objectCount"/> pooled projectiles from
        /// <paramref name="from"/> to <paramref name="to"/>, staggered so a
        /// multi-object volley reads visually. Returns each projectile to
        /// the pool the instant its own flight completes, and only
        /// finishes once every projectile has actually landed.
        /// </summary>
        private IEnumerator PlayVolley(Vector3 from, Vector3 to, int objectCount, Action onEachImpact)
        {
            int count = Mathf.Max(objectCount, 1);
            Coroutine lastImpactRoutine = null;

            for (int i = 0; i < count; i++)
            {
                AttackProjectileView projectile = _projectilePool.Get();
                Tween flight = projectile.PlayFlight(from, to);
                lastImpactRoutine = StartCoroutine(WaitForProjectileImpact(flight, projectile, onEachImpact));

                bool isLastObject = i == count - 1;
                if (!isLastObject)
                {
                    yield return new WaitForSeconds(_volleyLaunchStaggerSeconds);
                }
            }

            yield return lastImpactRoutine;
        }

        /// <summary>
        /// Waits for one projectile's flight, then applies its impact and
        /// releases it back to the pool. Deliberately routed through a
        /// coroutine rather than <c>Tween.OnComplete</c>: DOTween's
        /// OnComplete *replaces* any previously registered callback on the
        /// same tween instead of chaining them, so a projectile whose
        /// flight is also awaited via <see cref="TweenCoroutineExtensions.AsCoroutine"/>
        /// (which sets its own OnComplete internally) would silently lose
        /// this impact/release callback — which is exactly why projectiles
        /// were staying stuck on screen instead of disappearing.
        /// </summary>
        private IEnumerator WaitForProjectileImpact(Tween flight, AttackProjectileView projectile, Action onImpact)
        {
            yield return flight.AsCoroutine();
            onImpact?.Invoke();
            _projectilePool.Release(projectile);
        }

        private void EnsureProjectileContainer()
        {
            if (_projectileContainer != null)
            {
                return;
            }

            GameObject containerObject = new GameObject("Projectiles");
            containerObject.transform.SetParent(transform);
            containerObject.transform.localPosition = Vector3.zero;
            _projectileContainer = containerObject.transform;
        }
    }
}
