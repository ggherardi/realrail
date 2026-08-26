using System;
using UnityEngine;

namespace RealRail
{
    public sealed class UpgradeTarget : MonoBehaviour
    {
        [SerializeField] int startingHealth = 3;

        Health _health;
        EnemyMover _mover;
        float _playerZ;
        bool _resolved;

        public event Action<UpgradeTarget, bool> Resolved;

        void Awake()
        {
            _health = GetComponent<Health>();
            _mover = GetComponent<EnemyMover>();
            if (_health != null)
            {
                _health.Died += OnDied;
            }
        }

        void OnDestroy()
        {
            if (_health != null)
            {
                _health.Died -= OnDied;
            }
        }

        public void Initialize(GameSession session, float laneX, float playerZ, float y, float speed)
        {
            _playerZ = playerZ;
            _health.SetMaxHealth(startingHealth);
            _mover.Initialize(session, laneX, playerZ, y, speed);
        }

        void Update()
        {
            if (!_resolved && transform.position.z <= _playerZ)
            {
                Resolve(false);
            }
        }

        void OnDied()
        {
            Resolve(true);
        }

        void Resolve(bool collected)
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;
            Resolved?.Invoke(this, collected);
            Destroy(gameObject);
        }
    }
}
