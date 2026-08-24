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

        public void Bind(GameSession gameSession, LaneLayout laneLayout, GameObject prefab, int playerLayer)
        {
            session = gameSession;
            lanes = laneLayout;
            enemyPrefab = prefab;
            _playerLayer = playerLayer;
            _cooldown = spawnInterval;
        }

        void Update()
        {
            if (session == null || !session.IsPlaying || lanes == null || enemyPrefab == null)
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
            var laneIndex = Random.Range(0, lanes.LaneCount);
            var position = lanes.GetSpawnPosition(laneIndex);
            var instance = Instantiate(enemyPrefab, position, Quaternion.identity);
            instance.SetActive(true);

            var mover = instance.GetComponent<EnemyMover>();
            mover.Initialize(session, lanes.GetLaneX(laneIndex), lanes.PlayerZ, lanes.ActorY);

            var contact = instance.GetComponent<EnemyContactDamage>();
            contact.Initialize(session, _playerLayer);
        }
    }
}
