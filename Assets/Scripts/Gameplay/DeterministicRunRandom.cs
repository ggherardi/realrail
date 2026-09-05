using System;
using UnityEngine;

namespace RealRail
{
    /// <summary>Small RNG contract used by run-scoped gameplay decisions.</summary>
    public interface IRunRandom : IUpgradeRandom
    {
        float NextFloat(float minimumInclusive, float maximumInclusive);
    }

    /// <summary>
    /// Stable, platform-independent PRNG. Do not replace it with UnityEngine.Random: that global
    /// state would make one simulation job affect another.
    /// </summary>
    public sealed class DeterministicRunRandom : IRunRandom
    {
        uint _state;

        public DeterministicRunRandom(int seed)
        {
            Seed = seed;
            _state = unchecked((uint)seed);
            if (_state == 0) _state = 0x6D2B79F5u;
        }

        public int Seed { get; }

        uint NextUInt()
        {
            var value = _state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            _state = value;
            return value;
        }

        public int Next(int exclusiveMax)
        {
            if (exclusiveMax <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            return (int)(NextUInt() % (uint)exclusiveMax);
        }

        public float NextFloat(float minimumInclusive, float maximumInclusive)
        {
            if (maximumInclusive < minimumInclusive) throw new ArgumentOutOfRangeException(nameof(maximumInclusive));
            var unit = (NextUInt() >> 8) * (1f / 16777216f);
            return minimumInclusive + (maximumInclusive - minimumInclusive) * unit;
        }
    }

    sealed class UnityRunRandom : IRunRandom
    {
        public static readonly UnityRunRandom Shared = new UnityRunRandom();
        public int Next(int exclusiveMax) => UnityEngine.Random.Range(0, exclusiveMax);
        public float NextFloat(float minimumInclusive, float maximumInclusive) => UnityEngine.Random.Range(minimumInclusive, maximumInclusive);
    }

    /// <summary>
    /// Owns one seed and derives isolated streams so adding reward randomness cannot perturb
    /// enemy composition. Instances are per run and have no Unity global state.
    /// </summary>
    public sealed class RunRandomContext
    {
        public RunRandomContext(int seed) => Seed = seed;
        public int Seed { get; }

        public IRunRandom CreateStream(string name)
        {
            unchecked
            {
                var hash = 23;
                foreach (var character in name ?? string.Empty) hash = hash * 31 + character;
                return new DeterministicRunRandom(Seed ^ hash);
            }
        }

        public static int SeedForRun(int batchSeed, int runIndex)
        {
            unchecked { return batchSeed + runIndex * (int)0x9E3779B9u; }
        }
    }
}
