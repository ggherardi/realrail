using System;
using System.Collections.Generic;

namespace RealRail
{
    /// <summary>One acquired upgrade level included in a simulation-ready run result.</summary>
    public readonly struct AcquiredUpgrade
    {
        public AcquiredUpgrade(UpgradeId upgrade, int level)
        {
            Upgrade = upgrade;
            Level = level;
        }

        public UpgradeId Upgrade { get; }
        public int Level { get; }
    }

    /// <summary>Compact immutable summary of one completed or in-progress gameplay run.</summary>
    public sealed class RunResult
    {
        public RunResult(SessionState outcome, float durationSeconds, int finalWaveReached, int enemiesKilled, int enemiesLeaked, int playerDamageTaken, IReadOnlyList<AcquiredUpgrade> upgrades, int? runSeed = null, IReadOnlyList<string> upgradeOffers = null, IReadOnlyList<UpgradeId> upgradeSelections = null)
        {
            Outcome = outcome;
            DurationSeconds = durationSeconds;
            FinalWaveReached = finalWaveReached;
            EnemiesKilled = enemiesKilled;
            EnemiesLeaked = enemiesLeaked;
            PlayerDamageTaken = playerDamageTaken;
            Upgrades = upgrades ?? Array.Empty<AcquiredUpgrade>();
            RunSeed = runSeed;
            UpgradeOffers = upgradeOffers ?? Array.Empty<string>();
            UpgradeSelections = upgradeSelections ?? Array.Empty<UpgradeId>();
        }

        public SessionState Outcome { get; }
        public bool IsVictory => Outcome == SessionState.Victory;
        public bool IsDefeat => Outcome == SessionState.Lost;
        public float DurationSeconds { get; }
        public int FinalWaveReached { get; }
        public int EnemiesKilled { get; }
        public int EnemiesLeaked { get; }
        public int PlayerDamageTaken { get; }
        public IReadOnlyList<AcquiredUpgrade> Upgrades { get; }
        /// <summary>Seed used for this run, when it was executed through deterministic simulation.</summary>
        public int? RunSeed { get; }
        /// <summary>Ordered offer sets observed during a simulation run, encoded as stable upgrade labels.</summary>
        public IReadOnlyList<string> UpgradeOffers { get; }
        /// <summary>Ordered authoritative reward applications observed during a simulation run.</summary>
        public IReadOnlyList<UpgradeId> UpgradeSelections { get; }
    }
}
