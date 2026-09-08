using System;
using UnityEngine;

namespace RealRail
{
    /// <summary>
    /// Development-only tuning and activation seam for the Railgun visual prototype.
    /// This is deliberately independent from run-upgrade acquisition.
    /// </summary>
    public sealed class RailgunPrototype : MonoBehaviour
    {
        [SerializeField] bool enabledForDebug;
        [SerializeField, Min(0.25f)] float cadenceSeconds = 3f;
        [SerializeField, Min(1)] int damage = 6;
        [SerializeField, Min(1)] int distinctHitCapacity = 24;
        [SerializeField, Min(1f)] float projectileSpeed = 80f;

        float _nextScheduledShotTime;

        public bool IsEnabled => enabledForDebug;
        public float CadenceSeconds => cadenceSeconds;
        public int Damage => damage;
        public int DistinctHitCapacity => distinctHitCapacity;
        public float ProjectileSpeed => projectileSpeed;
        public event Action<bool> Changed;

        public void SetEnabled(bool enabled)
        {
            if (enabledForDebug == enabled) return;
            enabledForDebug = enabled;
            _nextScheduledShotTime = Time.time + cadenceSeconds;
            Changed?.Invoke(enabledForDebug);
        }

        /// <summary>Returns one scheduled prototype shot while preserving normal auto-fire between events.</summary>
        public bool TryConsumeScheduledShot(float currentTime)
        {
            if (!enabledForDebug || currentTime < _nextScheduledShotTime) return false;
            _nextScheduledShotTime = currentTime + cadenceSeconds;
            return true;
        }

        public ShotConfiguration GetShotConfiguration() => new ShotConfiguration(1, 0f, damage, distinctHitCapacity, true, projectileSpeed);

        public void ConfigureForTests(float cadence, int prototypeDamage, int capacity, float prototypeSpeed = 80f)
        {
            cadenceSeconds = Mathf.Max(0.25f, cadence);
            damage = Mathf.Max(1, prototypeDamage);
            distinctHitCapacity = Mathf.Max(1, capacity);
            projectileSpeed = Mathf.Max(1f, prototypeSpeed);
        }
    }
}
