using System;
using UnityEngine;

namespace RealRail
{
    public enum DamageSource
    {
        Generic,
        Projectile,
        Volcano
    }

    public sealed class Health : MonoBehaviour
    {
        [SerializeField] int maxHealth = 3;

        bool _initialized;

        public int Max => maxHealth;
        public int Current { get; private set; }

        public event Action<int, int> Changed;
        public event Action Died;
        public event Action<DamageSource> DiedWithSource;

        void Awake()
        {
            EnsureInitialized();
        }

        public void SetMaxHealth(int max)
        {
            maxHealth = Mathf.Max(1, max);
            Current = maxHealth;
            _initialized = true;
            Changed?.Invoke(Current, maxHealth);
        }

        public void TakeDamage(int amount, DamageSource source = DamageSource.Generic)
        {
            EnsureInitialized();
            if (amount <= 0 || Current <= 0)
            {
                return;
            }

            Current = Mathf.Max(0, Current - amount);
            Changed?.Invoke(Current, maxHealth);
            if (Current == 0)
            {
                DiedWithSource?.Invoke(source);
                Died?.Invoke();
            }
        }

        void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            Current = Mathf.Max(1, maxHealth);
            _initialized = true;
        }
    }
}
