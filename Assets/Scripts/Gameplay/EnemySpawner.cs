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
        WaveConfig _config;
        bool _isSpawning;

        public event Action<WaveEnemy> EnemySpawned;

        public void BeginWave(WaveConfig config)
        {
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

        void Update()
        {
            if (!_isSpawning || session == null || !session.IsPlaying || lanes == null || enemyPrefab == null)
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

            EnemySpawned?.Invoke(instance.GetComponent<WaveEnemy>());
        }
    }
}
