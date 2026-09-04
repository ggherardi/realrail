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

    /// <summary>Identities that are allowed to appear as rewards during one run.</summary>
    public sealed class RunUpgradePool
    {
        readonly List<UpgradeId> _upgrades;

        public RunUpgradePool(IEnumerable<UpgradeId> upgrades)
        {
            _upgrades = new List<UpgradeId>();
            if (upgrades == null) return;

            foreach (var upgrade in upgrades)
            {
                if (!_upgrades.Contains(upgrade)) _upgrades.Add(upgrade);
            }
        }

        public IReadOnlyList<UpgradeId> Upgrades => _upgrades;

        public static RunUpgradePool CreateCurrentGameplayPool() => new RunUpgradePool(new[]
        {
            UpgradeId.DoubleShot,
            UpgradeId.RapidFire,
            UpgradeId.PiercingShot,
            UpgradeId.PowerShot
        });
    }

    public static class UpgradeRewardGenerator
    {
        public static List<UpgradeId> GetEligible(RunUpgradePool pool, UpgradeState state)
        {
            var eligible = new List<UpgradeId>();
            if (pool == null || state == null) return eligible;

            foreach (var upgrade in pool.Upgrades)
            {
                if (state.CanBeOffered(upgrade))
                {
                    eligible.Add(upgrade);
                }
            }
            return eligible;
        }

        public static List<UpgradeId> GetEligible(UpgradeState state) => GetEligible(RunUpgradePool.CreateCurrentGameplayPool(), state);

        public static List<UpgradeId> GenerateCandidates(RunUpgradePool pool, UpgradeState state, IUpgradeRandom random, int maximumChoices = 3)
        {
            var candidates = GetEligible(pool, state);
            if (random == null || maximumChoices <= 0) return new List<UpgradeId>();

            var count = Mathf.Min(maximumChoices, candidates.Count);
            for (var index = 0; index < count; index++)
            {
                var selectedIndex = index + random.Next(candidates.Count - index);
                (candidates[index], candidates[selectedIndex]) = (candidates[selectedIndex], candidates[index]);
            }
            if (candidates.Count > count) candidates.RemoveRange(count, candidates.Count - count);
            return candidates;
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
        RunUpgradePool _runPool;

        public UpgradeState State => _state;
        public RunUpgradePool RunPool => _runPool ??= RunUpgradePool.CreateCurrentGameplayPool();
        public event Action<UpgradeApplication> UpgradeApplied;
        public event Action UpgradesChanged;

        public ShotConfiguration GetShotConfiguration() => _state.DeriveShotConfiguration();

        public bool TryApplyAutomaticReward(out UpgradeApplication application)
        {
            var eligible = UpgradeRewardGenerator.GetEligible(RunPool, _state);
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

        public void SetRunPoolForTests(RunUpgradePool pool)
        {
            _runPool = pool ?? new RunUpgradePool(Array.Empty<UpgradeId>());
        }

        public List<UpgradeId> GenerateRewardCandidates(int maximumChoices = 3)
        {
            return UpgradeRewardGenerator.GenerateCandidates(RunPool, _state, _random, maximumChoices);
        }

        void NotifyApplied(UpgradeApplication application)
        {
            UpgradeApplied?.Invoke(application);
            UpgradesChanged?.Invoke();
        }
    }
}
