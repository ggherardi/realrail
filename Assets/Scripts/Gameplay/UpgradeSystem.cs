using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealRail
{
    public enum UpgradeId
    {
        DoubleShot,
        RapidFire,
        PiercingShot,
        PowerShot
    }

    public readonly struct ShotConfiguration
    {
        public ShotConfiguration(int projectileCount, float fireInterval, int damage, int distinctHitCapacity)
        {
            ProjectileCount = projectileCount;
            FireInterval = fireInterval;
            Damage = damage;
            DistinctHitCapacity = distinctHitCapacity;
        }

        public int ProjectileCount { get; }
        public float FireInterval { get; }
        public int Damage { get; }
        public int DistinctHitCapacity { get; }
    }

    public readonly struct UpgradeApplication
    {
        public UpgradeApplication(UpgradeId upgrade, int level)
        {
            Upgrade = upgrade;
            Level = level;
        }

        public UpgradeId Upgrade { get; }
        public int Level { get; }
    }

    public sealed class UpgradeState
    {
        static readonly int[] MaxLevels = { 1, 3, 2, 2 };
        readonly int[] _levels = new int[MaxLevels.Length];

        public int GetLevel(UpgradeId upgrade) => _levels[(int)upgrade];
        public int GetMaxLevel(UpgradeId upgrade) => MaxLevels[(int)upgrade];
        public bool IsCapped(UpgradeId upgrade) => GetLevel(upgrade) >= GetMaxLevel(upgrade);
        public bool CanBeOffered(UpgradeId upgrade) => !IsCapped(upgrade);

        public bool TryApplyLevel(UpgradeId upgrade, out int level)
        {
            if (IsCapped(upgrade))
            {
                level = GetLevel(upgrade);
                return false;
            }

            level = ++_levels[(int)upgrade];
            return true;
        }

        public void Reset()
        {
            Array.Clear(_levels, 0, _levels.Length);
        }

        public ShotConfiguration DeriveShotConfiguration()
        {
            return new ShotConfiguration(
                1 + GetLevel(UpgradeId.DoubleShot),
                0.35f - (0.05f * GetLevel(UpgradeId.RapidFire)),
                1 + GetLevel(UpgradeId.PowerShot),
                1 + GetLevel(UpgradeId.PiercingShot));
        }
    }

    public interface IUpgradeRandom
    {
        int Next(int exclusiveMax);
    }

    sealed class UnityUpgradeRandom : IUpgradeRandom
    {
        public int Next(int exclusiveMax) => UnityEngine.Random.Range(0, exclusiveMax);
    }

    public static class UpgradeRewardGenerator
    {
        public static List<UpgradeId> GetEligible(UpgradeState state)
        {
            var eligible = new List<UpgradeId>();
            foreach (UpgradeId upgrade in Enum.GetValues(typeof(UpgradeId)))
            {
                if (state.CanBeOffered(upgrade))
                {
                    eligible.Add(upgrade);
                }
            }
            return eligible;
        }

        public static bool TrySelectAutomatic(IReadOnlyList<UpgradeId> eligible, IUpgradeRandom random, out UpgradeId selected)
        {
            if (eligible == null || eligible.Count == 0)
            {
                selected = default;
                return false;
            }

            selected = eligible[random.Next(eligible.Count)];
            return true;
        }
    }

    /// <summary>Single runtime authority for acquired upgrade levels and reward application.</summary>
    public sealed class UpgradeSystem : MonoBehaviour
    {
        readonly UpgradeState _state = new UpgradeState();
        IUpgradeRandom _random = new UnityUpgradeRandom();

        public UpgradeState State => _state;
        public event Action<UpgradeApplication> UpgradeApplied;
        public event Action UpgradesChanged;

        public ShotConfiguration GetShotConfiguration() => _state.DeriveShotConfiguration();

        public bool TryApplyAutomaticReward(out UpgradeApplication application)
        {
            var eligible = UpgradeRewardGenerator.GetEligible(_state);
            if (!UpgradeRewardGenerator.TrySelectAutomatic(eligible, _random, out var selected) ||
                !_state.TryApplyLevel(selected, out var level))
            {
                application = default;
                return false;
            }

            application = new UpgradeApplication(selected, level);
            NotifyApplied(application);
            return true;
        }

        /// <summary>Applies exactly one level through the same runtime state used by rewards.</summary>
        public bool TryApplyLevel(UpgradeId upgrade, out UpgradeApplication application)
        {
            if (!_state.TryApplyLevel(upgrade, out var level))
            {
                application = default;
                return false;
            }

            application = new UpgradeApplication(upgrade, level);
            NotifyApplied(application);
            return true;
        }

        /// <summary>Development seam for returning the current run build to its baseline.</summary>
        public void ResetUpgrades()
        {
            _state.Reset();
            UpgradesChanged?.Invoke();
        }

        public void SetRewardRandomForTests(IUpgradeRandom random)
        {
            _random = random ?? new UnityUpgradeRandom();
        }

        void NotifyApplied(UpgradeApplication application)
        {
            UpgradeApplied?.Invoke(application);
            UpgradesChanged?.Invoke();
        }
    }
}
