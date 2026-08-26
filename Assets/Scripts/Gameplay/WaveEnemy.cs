using System;
using UnityEngine;

namespace RealRail
{
    public enum WaveEnemyResolution
    {
        Killed,
        Removed
    }

    public sealed class WaveEnemy : MonoBehaviour
    {
        Health _health;
        bool _resolved;

        public event Action<WaveEnemy, WaveEnemyResolution> Resolved;

        void Awake()
        {
            _health = GetComponent<Health>();
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

            Resolve(WaveEnemyResolution.Removed);
        }

        void OnDied()
        {
            Resolve(WaveEnemyResolution.Killed);
        }

        void Resolve(WaveEnemyResolution resolution)
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;
            Resolved?.Invoke(this, resolution);
        }
    }
}
