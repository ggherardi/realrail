using UnityEngine;

namespace RealRail
{
    [RequireComponent(typeof(EnemyMover))]
    public sealed class EnemyDefenseLine : MonoBehaviour
    {
        GameSession _session;
        EnemyMover _mover;
        WaveEnemy _waveEnemy;
        bool _applied;

        public void Initialize(GameSession session)
        {
            _session = session;
            _applied = false;

            if (_mover == null)
            {
                _mover = GetComponent<EnemyMover>();
            }

            if (_waveEnemy == null)
            {
                _waveEnemy = GetComponent<WaveEnemy>();
            }

            _mover.DestinationReached -= OnDestinationReached;
            _mover.DestinationReached += OnDestinationReached;
        }

        void OnDestroy()
        {
            if (_mover != null)
            {
                _mover.DestinationReached -= OnDestinationReached;
            }
        }

        void OnDestinationReached()
        {
            if (_applied || _session == null || !_session.IsPlaying)
            {
                return;
            }

            _applied = true;
            _session.ApplyPlayerDamage(1);
            if (_waveEnemy == null)
            {
                _waveEnemy = GetComponent<WaveEnemy>();
            }
            _waveEnemy?.ResolveRemoved();
            if (Application.isPlaying)
            {
                Destroy(gameObject);
                return;
            }

            DestroyImmediate(gameObject);
        }
    }
}
