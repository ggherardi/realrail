using System;
using UnityEngine;

namespace RealRail
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField] LaneLayout lanes;
        [SerializeField] GameObject enemyPrefab;
        [SerializeField] float spawnInterval = 1.6f;

        int _playerLayer;
        float _cooldown;
        float _moveSpeed;
        bool _isSpawning;

        public event Action<WaveEnemy> EnemySpawned;

        public void Bind(GameSession gameSession, LaneLayout laneLayout, GameObject prefab, int playerLayer)
        {
            session = gameSession;
            lanes = laneLayout;
            enemyPrefab = prefab;
            _playerLayer = playerLayer;
            StopSpawning();
        }

        public void BeginWave(WaveConfig config)
        {
            spawnInterval = config.SpawnInterval;
            _moveSpeed = config.MoveSpeed;
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
            var instance = Instantiate(enemyPrefab, position, Quaternion.identity);
            instance.SetActive(true);

            var mover = instance.GetComponent<EnemyMover>();
            mover.Initialize(session, position.x, lanes.PlayerZ, lanes.ActorY, _moveSpeed);

            var contact = instance.GetComponent<EnemyContactDamage>();
            contact.Initialize(session, _playerLayer);

            EnemySpawned?.Invoke(instance.GetComponent<WaveEnemy>());
        }
    }
}
