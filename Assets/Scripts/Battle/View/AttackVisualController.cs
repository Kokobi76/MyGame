using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Match3.Utilities;
using Match3.Battle.Model;
using Match3.View;

namespace Match3.Battle.View
{
    /// <summary>
    /// Plays the visual for one resolved attack — melee lunge, ranged
    /// Slash volley, Sword rain, or a Tiles Skill's board-destroy objects
    /// — and invokes a callback exactly when each object connects, so
    /// <see cref="Match3.Battle.Gameplay.BattleController"/> can time
    /// damage application to match ("an object must actually reach the
    /// opponent before HP is deducted", per design).
    ///
    /// Owns one pooled-projectile system per DISTINCT prefab actually
    /// used (see <see cref="GetPool"/>): <see cref="_projectilePrefab"/>
    /// is the shared default (used for ranged Slash, and as the ultimate
    /// fallback for Sword/board-destroy if nothing more specific is
    /// assigned); <see cref="_swordProjectilePrefab"/> and
    /// <see cref="_boardDestroyDefaultPrefab"/> are optional overrides so
    /// Sword rain and Tiles Skill board-destroy objects can each have
    /// their own look/speed (a prefab's own
    /// <see cref="AttackProjectileView"/> Flight Duration field is what
    /// controls its speed) independently of ranged Slash; a Tiles Skill
    /// can go one step further and specify its OWN prefab via
    /// <see cref="Match3.Battle.Skills.SkillDefinition.BoardObjectPrefab"/>,
    /// taking priority over this controller's own default.
    /// </summary>
    public sealed class AttackVisualController : MonoBehaviour
    {
        [Header("Characters")]
        [SerializeField] private CharacterView _playerView;
        [SerializeField] private CharacterView _enemyView;

        [Header("Board (Tiles Skill board-destroy visuals)")]
        [Tooltip("Required for Tiles Skill's board-destroy objects to have a flight visual at all — drag in the same Board View your board uses. Leave empty only if this battle never uses Tiles Skill; if it does and this is empty, a warning is logged and the objects simply won't be shown (tiles will still be destroyed correctly, just with no object flying in first).")]
        [SerializeField] private BoardView _boardView;

        [Header("Projectile Prefabs")]
        [Tooltip("Shared default — used for ranged Slash volleys, and as the fallback for Sword/board-destroy below if their own override is left empty.")]
        [SerializeField] private AttackProjectileView _projectilePrefab;
        [Tooltip("Optional override for Sword rain specifically, so its speed/look can differ from ranged Slash. Leave empty to keep using the default above.")]
        [SerializeField] private AttackProjectileView _swordProjectilePrefab;
        [Tooltip("Optional default for Tiles Skill board-destroy objects when the casting skill doesn't specify its own prefab (SkillDefinition.BoardObjectPrefab takes priority over this when set). Leave empty to fall back to the shared default above.")]
        [SerializeField] private AttackProjectileView _boardDestroyDefaultPrefab;

        [Header("Pool Settings")]
        [SerializeField] private Transform _projectileContainer;
        [SerializeField] private int _poolDefaultCapacity = 16;
        [SerializeField] private int _poolMaxSize = 64;

        [Header("Volley Pacing")]
        [Tooltip("Delay between launching each object in a multi-object volley, purely for readability.")]
        [SerializeField] private float _volleyLaunchStaggerSeconds = 0.05f;

        private AttackProjectilePool _defaultPool;
        private readonly Dictionary<AttackProjectileView, AttackProjectilePool> _poolsByPrefab = new Dictionary<AttackProjectileView, AttackProjectilePool>();

        private void Awake()
        {
            EnsureProjectileContainer();
            _defaultPool = new AttackProjectilePool(_projectilePrefab, _projectileContainer, _poolDefaultCapacity, _poolMaxSize);
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
            yield return PlayVolley(GetPool(null), attacker.ProjectileOrigin, defender.TargetPoint, objectCount, onEachImpact);
        }

        /// <summary>Sword: fires <paramref name="objectCount"/> objects from the attacker's designated Sword spawn point toward the defender, invoking <paramref name="onEachImpact"/> once per object as it lands. Uses <see cref="_swordProjectilePrefab"/> if assigned, so Sword's own flight speed/look can be tuned independently of ranged Slash.</summary>
        public IEnumerator PlaySwordAttack(BattleSide attackerSide, int objectCount, Action onEachImpact)
        {
            CharacterView attacker = GetView(attackerSide);
            CharacterView defender = GetView(attackerSide.GetOpposite());
            yield return PlayVolley(GetPool(_swordProjectilePrefab), attacker.SwordSpawnPosition, defender.TargetPoint, objectCount, onEachImpact);
        }

        /// <summary>
        /// Tiles Skill board-destroy: fires one pooled projectile from the
        /// attacker (offset by <paramref name="spawnOffset"/>) toward each
        /// of <paramref name="targetCells"/>'s world position (via the
        /// assigned Board View), staggered like any other volley, and
        /// waits for all of them to land. Purely visual — doesn't touch
        /// the board model itself; <see cref="Match3.Battle.Gameplay.BattleController"/>
        /// calls <see cref="Match3.Gameplay.BoardController.DestroyPositionsRoutine"/>
        /// right after this returns to actually clear the tiles, so what
        /// the player sees is objects flying in and landing, then the
        /// target tiles clearing shortly after.
        /// <paramref name="targetsAreEvenBlockCenters"/> (from the casting
        /// skill's own <c>NeedsEvenBlockCenterOffset</c>) nudges every
        /// target half a cell up-and-right, to the true geometric center
        /// of an even-sized Block — see
        /// <see cref="Match3.Battle.Skills.DestroyAreaResolver"/>'s class
        /// remarks for why an even-sized block has no single center tile.
        /// <paramref name="prefabOverride"/> (from the casting skill's own
        /// <c>BoardObjectPrefab</c>) takes priority over
        /// <see cref="_boardDestroyDefaultPrefab"/> when provided; pass
        /// null to just use this controller's own default/fallback.
        /// </summary>
        public IEnumerator PlayBoardDestroyAttack(BattleSide attackerSide, IReadOnlyList<Vector2Int> targetCells, bool targetsAreEvenBlockCenters, AttackProjectileView prefabOverride, Vector3 spawnOffset)
        {
            if (targetCells == null || targetCells.Count == 0)
            {
                yield break;
            }
            if (_boardView == null)
            {
                Debug.LogWarning("AttackVisualController has no Board View assigned, so Tiles Skill board-destroy objects have no flight visual (tiles still get destroyed correctly). Assign Board View in the Inspector to fix this.", this);
                yield break;
            }

            AttackProjectileView effectivePrefab = prefabOverride != null ? prefabOverride : _boardDestroyDefaultPrefab;
            AttackProjectilePool pool = GetPool(effectivePrefab);
            Vector3 origin = GetView(attackerSide).ProjectileOrigin + spawnOffset;
            Coroutine lastLandingRoutine = null;

            for (int i = 0; i < targetCells.Count; i++)
            {
                Vector3 targetWorldPosition = targetsAreEvenBlockCenters
                    ? GetEvenBlockCenterWorldPosition(targetCells[i])
                    : _boardView.GetWorldPosition(targetCells[i]);
                Debug.Log($"[TilesSkill] Object {i + 1}/{targetCells.Count} firing from {origin} to cell {targetCells[i]} (world {targetWorldPosition}){(targetsAreEvenBlockCenters ? " [even-block center, nudged between tiles]" : string.Empty)}", this);
                AttackProjectileView projectile = pool.Get();
                Tween flight = projectile.PlayFlight(origin, targetWorldPosition);
                lastLandingRoutine = StartCoroutine(WaitForBoardProjectileLanding(pool, flight, projectile));

                bool isLastObject = i == targetCells.Count - 1;
                if (!isLastObject)
                {
                    yield return new WaitForSeconds(_volleyLaunchStaggerSeconds);
                }
            }

            yield return lastLandingRoutine;
        }

        /// <summary>The true geometric center of an even-sized Block anchored at this cell — halfway between the anchor and its up-right neighbor (see <see cref="Match3.Battle.Skills.DestroyAreaResolver"/>'s centering convention), computed from two GetWorldPosition calls rather than needing this controller to know the board's own Cell Size directly.</summary>
        private Vector3 GetEvenBlockCenterWorldPosition(Vector2Int anchor)
        {
            Vector3 anchorWorldPosition = _boardView.GetWorldPosition(anchor);
            Vector3 upRightNeighborWorldPosition = _boardView.GetWorldPosition(anchor + Vector2Int.one);
            return (anchorWorldPosition + upRightNeighborWorldPosition) * 0.5f;
        }

        /// <summary>Resolves which pool to use for a prefab reference: the shared default pool if null or literally the same prefab as the default, otherwise a lazily-created pool dedicated to that exact prefab (cached for reuse — each distinct prefab gets exactly one pool for this controller's lifetime).</summary>
        private AttackProjectilePool GetPool(AttackProjectileView prefab)
        {
            if (prefab == null || prefab == _projectilePrefab)
            {
                return _defaultPool;
            }

            if (!_poolsByPrefab.TryGetValue(prefab, out AttackProjectilePool pool))
            {
                pool = new AttackProjectilePool(prefab, _projectileContainer, _poolDefaultCapacity, _poolMaxSize);
                _poolsByPrefab[prefab] = pool;
            }
            return pool;
        }

        /// <summary>
        /// Launches <paramref name="objectCount"/> pooled projectiles from
        /// <paramref name="from"/> to <paramref name="to"/>, staggered so a
        /// multi-object volley reads visually. Returns each projectile to
        /// the pool the instant its own flight completes, and only
        /// finishes once every projectile has actually landed.
        /// </summary>
        private IEnumerator PlayVolley(AttackProjectilePool pool, Vector3 from, Vector3 to, int objectCount, Action onEachImpact)
        {
            int count = Mathf.Max(objectCount, 1);
            Coroutine lastImpactRoutine = null;

            for (int i = 0; i < count; i++)
            {
                AttackProjectileView projectile = pool.Get();
                Tween flight = projectile.PlayFlight(from, to);
                lastImpactRoutine = StartCoroutine(WaitForProjectileImpact(pool, flight, projectile, onEachImpact));

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
        /// releases it back to the pool it came from. Deliberately routed
        /// through a coroutine rather than <c>Tween.OnComplete</c>:
        /// DOTween's OnComplete *replaces* any previously registered
        /// callback on the same tween instead of chaining them, so a
        /// projectile whose flight is also awaited via
        /// <see cref="TweenCoroutineExtensions.AsCoroutine"/> (which sets
        /// its own OnComplete internally) would silently lose this
        /// impact/release callback — which is exactly why projectiles
        /// were staying stuck on screen instead of disappearing.
        /// </summary>
        private IEnumerator WaitForProjectileImpact(AttackProjectilePool pool, Tween flight, AttackProjectileView projectile, Action onImpact)
        {
            yield return flight.AsCoroutine();
            onImpact?.Invoke();
            pool.Release(projectile);
        }

        /// <summary>Same reasoning as <see cref="WaitForProjectileImpact"/> (a dedicated coroutine rather than Tween.OnComplete). A board-destroy object has no per-object damage callback of its own — the board's own Tile Resolve pipeline handles the actual effect once BoardController.DestroyPositionsRoutine runs — so this only needs to release the projectile, to the pool it came from, once its flight ends.</summary>
        private IEnumerator WaitForBoardProjectileLanding(AttackProjectilePool pool, Tween flight, AttackProjectileView projectile)
        {
            yield return flight.AsCoroutine();
            pool.Release(projectile);
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
