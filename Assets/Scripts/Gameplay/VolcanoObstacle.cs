using System.Collections.Generic;
using UnityEngine;

namespace RealRail
{
    /// <summary>Temporary prototype world object. Its owner handles creation and cleanup; it never participates in wave accounting.</summary>
    [RequireComponent(typeof(Health))]
    public sealed class VolcanoObstacle : MonoBehaviour
    {
        readonly HashSet<EnemyMover> _engagers = new HashSet<EnemyMover>();
        readonly List<EnemyMover> _staleEngagers = new List<EnemyMover>();
        Health _health;
        float _nextTickTime;
        float _halfWidth;
        float _halfDepth;
        float _laneCenter;
        float _laneMin;
        float _laneMax;
        int _enemyDamage;
        float _damageInterval;

        public Health Health => _health;
        public int EngagerCount => _engagers.Count;

        public void Configure(float width, float depth, float laneCenter, float laneMin, float laneMax, int hitPoints,
            int enemyDamage, float damageInterval)
        {
            _halfWidth = width * 0.5f;
            _halfDepth = depth * 0.5f;
            _laneCenter = laneCenter;
            _laneMin = laneMin;
            _laneMax = laneMax;
            _enemyDamage = Mathf.Max(1, enemyDamage);
            _damageInterval = Mathf.Max(0.1f, damageInterval);
            _health = GetComponent<Health>();
            _health.SetMaxHealth(hitPoints);
        }

        void Awake() => _health = GetComponent<Health>();

        void Update()
        {
            if (Time.time < _nextTickTime || _engagers.Count == 0) return;
            _nextTickTime = Time.time + _damageInterval;
            ProcessEngagers();
        }

        /// <summary>Applies one bounded prototype interaction tick; exposed for deterministic EditMode coverage.</summary>
        public void ProcessEngagers()
        {
            _staleEngagers.Clear();
            foreach (var engager in _engagers)
            {
                if (engager == null) { _staleEngagers.Add(engager); continue; }
                var health = engager.GetComponent<Health>();
                if (health != null && health.Current <= _enemyDamage)
                {
                    // Preserve the wave contract before Health emits its normal death
                    // event: environmental Volcano deaths are leaks/removals, never
                    // player projectile kills or upgrade-trigger progress.
                    engager.GetComponent<WaveEnemy>()?.ResolveRemoved();
                }
                health?.TakeDamage(_enemyDamage, DamageSource.Volcano);
                _health?.TakeDamage(engager.GetComponent<VolcanoEnemyAttack>()?.DamagePerHit ?? 1, DamageSource.Generic);
            }
            foreach (var mover in _staleEngagers) _engagers.Remove(mover);
        }

        public bool BlocksForwardPath(Vector3 position, float targetZ) =>
            position.z > transform.position.z - _halfDepth && targetZ < transform.position.z + _halfDepth &&
            Mathf.Abs(position.x - transform.position.x) < _halfWidth + 0.5f;

        public bool TryGetBypassX(Vector3 position, float laneMin, float laneMax, out float bypassX)
        {
            var left = transform.position.x - _halfWidth - 0.55f;
            var right = transform.position.x + _halfWidth + 0.55f;
            var preferLeft = position.x <= transform.position.x;
            var preferred = preferLeft ? left : right;
            var alternate = preferLeft ? right : left;
            if (preferred >= laneMin && preferred <= laneMax) { bypassX = preferred; return true; }
            if (alternate >= laneMin && alternate <= laneMax) { bypassX = alternate; return true; }
            bypassX = _laneCenter;
            return false;
        }

        public bool IsInEngagementRange(Vector3 position) =>
            Mathf.Abs(position.z - transform.position.z) <= _halfDepth + 0.75f && Mathf.Abs(position.x - transform.position.x) <= _halfWidth + 0.75f;

        public void RegisterEngager(EnemyMover engager) => _engagers.Add(engager);
        public void UnregisterEngager(EnemyMover engager) => _engagers.Remove(engager);
    }
}
