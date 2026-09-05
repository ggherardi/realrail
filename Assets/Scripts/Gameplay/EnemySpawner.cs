using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealRail
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField] LaneLayout lanes;
        [SerializeField] GameObject enemyPrefab;
        [SerializeField] GameObject heavyEnemyPrefab;
        [SerializeField] float spawnInterval = 1.6f;

        float _cooldown;
        float _moveSpeed;
        float _gruntBaseSpeed;
        int _activeEnemyCount;
        WaveConfig _config;
        bool _isSpawning;
        IRunRandom _random = UnityRunRandom.Shared;
        readonly HashSet<WaveEnemy> _activeEnemies = new HashSet<WaveEnemy>();

        public event Action<WaveEnemy> EnemySpawned;
        public bool IsSpawning => _isSpawning;
        public int ActiveEnemyCount => _activeEnemyCount;

        public void SetRunRandom(IRunRandom random)
        {
            _random = random ?? UnityRunRandom.Shared;
        }

        public void BeginWave(WaveConfig config)
        {
            ResetSpawnerState();
            _config = config;
            spawnInterval = config.SpawnInterval;
            _moveSpeed = config.MoveSpeed;
            var gruntMover = enemyPrefab != null ? enemyPrefab.GetComponent<EnemyMover>() : null;
            _gruntBaseSpeed = gruntMover != null ? gruntMover.BaseSpeed : 0f;
            _cooldown = spawnInterval;
            _isSpawning = true;
        }

        public void StopSpawning()
        {
            _isSpawning = false;
        }

        /// <summary>Stops the current plan and forgets actors that a run owner is about to remove.</summary>
        public void ResetSpawnerState()
        {
            _isSpawning = false;
            _cooldown = 0f;
            _activeEnemyCount = 0;
            foreach (var enemy in _activeEnemies)
            {
                if (enemy != null) enemy.Resolved -= OnEnemyResolved;
            }
            _activeEnemies.Clear();
        }

        void Update()
        {
            if (!_isSpawning || session == null || !session.IsPlaying || lanes == null || enemyPrefab == null ||
                (_config.MaxConcurrentEnemies > 0 && _activeEnemyCount >= _config.MaxConcurrentEnemies))
            {
                return;
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f)
            {
                return;
            }

            _cooldown = spawnInterval;
            Spawn();
        }

        void Spawn()
        {
            var laneIndex = _random.Next(lanes.LaneCount);
            var position = lanes.GetSpawnPosition(laneIndex, _random);
            var prefab = _config.ShouldSpawnHeavy(_random.NextFloat(0f, 1f)) && heavyEnemyPrefab != null
                ? heavyEnemyPrefab
                : enemyPrefab;
            var instance = Instantiate(prefab, position, Quaternion.identity);
            instance.SetActive(true);

            var mover = instance.GetComponent<EnemyMover>();
            var speedScale = _gruntBaseSpeed > 0f ? mover.BaseSpeed / _gruntBaseSpeed : 1f;
            mover.Initialize(session, position.x, lanes.DefenseLineZ, lanes.ActorY, _moveSpeed * speedScale);

            var defenseLine = instance.GetComponent<EnemyDefenseLine>();
            defenseLine.Initialize(session);

            var waveEnemy = instance.GetComponent<WaveEnemy>();
            if (waveEnemy != null)
            {
                _activeEnemyCount++;
                _activeEnemies.Add(waveEnemy);
                waveEnemy.Resolved += OnEnemyResolved;
            }

            EnemySpawned?.Invoke(waveEnemy);
        }

        void OnEnemyResolved(WaveEnemy enemy, WaveEnemyResolution resolution)
        {
            if (enemy != null)
            {
                enemy.Resolved -= OnEnemyResolved;
            }

            _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
            _activeEnemies.Remove(enemy);
        }
    }
}
