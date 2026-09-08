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
                _health.DiedWithSource += OnDiedWithSource;
            }
        }

        void OnDestroy()
        {
            if (_health != null)
            {
                _health.Died -= OnDied;
                _health.DiedWithSource -= OnDiedWithSource;
            }

            Resolve(WaveEnemyResolution.Removed);
        }

        void OnDied()
        {
            // DiedWithSource is invoked first for normal Health deaths. This fallback
            // keeps legacy/direct callers safe if they do not provide a source.
            if (!_resolved) Resolve(WaveEnemyResolution.Killed);
        }

        void OnDiedWithSource(DamageSource source)
        {
            Resolve(source == DamageSource.Projectile ? WaveEnemyResolution.Killed : WaveEnemyResolution.Removed);
        }

        public void ResolveRemoved()
        {
            Resolve(WaveEnemyResolution.Removed);
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
