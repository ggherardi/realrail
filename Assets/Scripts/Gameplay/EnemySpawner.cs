using System;
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

        public event Action<WaveEnemy> EnemySpawned;
        public bool IsSpawning => _isSpawning;
        public int ActiveEnemyCount => _activeEnemyCount;

        public void BeginWave(WaveConfig config)
        {
            _config = config;
            spawnInterval = config.SpawnInterval;
            _moveSpeed = config.MoveSpeed;
            var gruntMover = enemyPrefab != null ? enemyPrefab.GetComponent<EnemyMover>() : null;
            _gruntBaseSpeed = gruntMover != null ? gruntMover.BaseSpeed : 0f;
            _cooldown = spawnInterval;
            _activeEnemyCount = 0;
            _isSpawning = true;
        }

        public void StopSpawning()
        {
            _isSpawning = false;
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
            var laneIndex = UnityEngine.Random.Range(0, lanes.LaneCount);
            var position = lanes.GetSpawnPosition(laneIndex);
            var prefab = _config.ShouldSpawnHeavy(UnityEngine.Random.value) && heavyEnemyPrefab != null
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
        }
    }
}
