using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealRail
{
    /// <summary>Development-only bounded propagation and Frost prototype; deliberately independent from upgrades and Fusion acquisition.</summary>
    public sealed class CryoStormPrototype : MonoBehaviour
    {
        const int CandidateBufferSize = 48;

        [SerializeField] GameSession session;
        [SerializeField] EnemySpawner enemySpawner;
        [SerializeField] bool enabledForDebug;
        [SerializeField, Min(0.5f)] float cadenceSeconds = 4f;
        [SerializeField, Min(2)] int maximumTargets = 10;
        [SerializeField, Min(0.5f)] float chainRange = 4.5f;
        [SerializeField, Min(0.02f)] float stepDelaySeconds = 0.11f;
        [SerializeField, Range(0.05f, 1f)] float frostSpeedMultiplier = 0.55f;
        [SerializeField, Min(0.25f)] float frostDurationSeconds = 3.5f;
        [SerializeField] LayerMask enemyLayers;

        readonly Collider[] _candidates = new Collider[CandidateBufferSize];
        readonly HashSet<WaveEnemy> _visited = new HashSet<WaveEnemy>();
        float _nextActivationTime;
        bool _isPropagating;

        public bool IsEnabled => enabledForDebug;
        public bool IsPropagating => _isPropagating;
        public float CadenceSeconds => cadenceSeconds;
        public int MaximumTargets => maximumTargets;
        public float ChainRange => chainRange;
        public float StepDelaySeconds => stepDelaySeconds;
        public float FrostSpeedMultiplier => frostSpeedMultiplier;
        public float FrostDurationSeconds => frostDurationSeconds;
        public event Action<bool> Changed;
        public event Action<WaveEnemy> FrostApplied;

        void Update()
        {
            if (!enabledForDebug || _isPropagating || session == null || !session.IsPlaying || Time.time < _nextActivationTime) return;
            TryActivate();
        }

        public void SetEnabled(bool enabled)
        {
            if (enabledForDebug == enabled) return;
            enabledForDebug = enabled;
            _nextActivationTime = Time.time + cadenceSeconds;
            Changed?.Invoke(enabledForDebug);
        }

        /// <summary>Starts one chain from the nearest active real wave enemy, if available.</summary>
        public bool TryActivate()
        {
            if (_isPropagating || session == null || !session.IsPlaying || enemySpawner == null ||
                !enemySpawner.TryGetNearestActiveEnemy(transform.position, out var source)) return false;
            StartCoroutine(Propagate(source));
            _nextActivationTime = Time.time + cadenceSeconds;
            return true;
        }

        public void ConfigureForTests(int targetLimit, float range, float stepDelay, float slowMultiplier, float slowDuration)
        {
            maximumTargets = Mathf.Max(2, targetLimit);
            chainRange = Mathf.Max(0.5f, range);
            stepDelaySeconds = Mathf.Max(0.02f, stepDelay);
            frostSpeedMultiplier = Mathf.Clamp(slowMultiplier, 0.05f, 1f);
            frostDurationSeconds = Mathf.Max(0.25f, slowDuration);
            enemyLayers = 1 << LayerMask.NameToLayer(GameplayLayers.Enemy);
        }

        IEnumerator Propagate(WaveEnemy source)
        {
            _isPropagating = true;
            _visited.Clear();
            var current = source;
            while (current != null && _visited.Count < maximumTargets)
            {
                _visited.Add(current);
                ApplyFrost(current);
                var next = FindNearestUnvisited(current.transform.position);
                if (next == null) break;
                CryoStormArc.Create(current.transform.position, next.transform.position);
                yield return new WaitForSeconds(stepDelaySeconds);
                current = next;
            }
            _isPropagating = false;
        }

        void ApplyFrost(WaveEnemy enemy)
        {
            if (enemy == null) return;
            var mover = enemy.GetComponent<EnemyMover>();
            var frost = enemy.GetComponent<FrostStatus>();
            if (mover == null || frost == null) return;
            mover.ApplyTemporarySpeedMultiplier(frostSpeedMultiplier, frostDurationSeconds);
            frost.Apply(frostDurationSeconds);
            FrostApplied?.Invoke(enemy);
        }

        WaveEnemy FindNearestUnvisited(Vector3 origin)
        {
            var count = Physics.OverlapSphereNonAlloc(origin, chainRange, _candidates, enemyLayers, QueryTriggerInteraction.Collide);
            WaveEnemy nearest = null;
            var nearestDistance = float.MaxValue;
            for (var index = 0; index < count; index++)
            {
                var candidate = _candidates[index] != null ? _candidates[index].GetComponentInParent<WaveEnemy>() : null;
                if (candidate == null || _visited.Contains(candidate)) continue;
                var distance = (candidate.transform.position - origin).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }
    }
}
