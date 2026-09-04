using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RealRail
{
    /// <summary>Structured, zero-safe summary suitable for both batch tooling and readable reports.</summary>
    public sealed class RunStatistics
    {
        internal RunStatistics(BotProfile profile, int runCount, int victories, int defeats, float averageDurationSeconds,
            float medianDurationSeconds, float averageFinalWave, float medianFinalWave, float averageEnemiesKilled,
            float averageEnemiesLeaked, float averagePlayerDamageTaken, int mostCommonDefeatWave,
            IReadOnlyDictionary<int, int> waveReachedDistribution, IReadOnlyDictionary<int, int> defeatWaveDistribution,
            IReadOnlyDictionary<UpgradeId, int> upgradeLevelTotals, IReadOnlyDictionary<string, int> buildDistribution)
        {
            Profile = profile;
            RunCount = runCount;
            Victories = victories;
            Defeats = defeats;
            AverageDurationSeconds = averageDurationSeconds;
            MedianDurationSeconds = medianDurationSeconds;
            AverageFinalWave = averageFinalWave;
            MedianFinalWave = medianFinalWave;
            AverageEnemiesKilled = averageEnemiesKilled;
            AverageEnemiesLeaked = averageEnemiesLeaked;
            AveragePlayerDamageTaken = averagePlayerDamageTaken;
            MostCommonDefeatWave = mostCommonDefeatWave;
            WaveReachedDistribution = waveReachedDistribution;
            DefeatWaveDistribution = defeatWaveDistribution;
            UpgradeLevelTotals = upgradeLevelTotals;
            BuildDistribution = buildDistribution;
        }

        public BotProfile Profile { get; }
        public int RunCount { get; }
        public int Victories { get; }
        public int Defeats { get; }
        public float WinRate => RunCount == 0 ? 0f : (float)Victories / RunCount;
        public float AverageDurationSeconds { get; }
        public float MedianDurationSeconds { get; }
        public float AverageFinalWave { get; }
        public float MedianFinalWave { get; }
        public float AverageEnemiesKilled { get; }
        public float AverageEnemiesLeaked { get; }
        public float AveragePlayerDamageTaken { get; }
        /// <summary>Zero when there are no defeats.</summary>
        public int MostCommonDefeatWave { get; }
        public IReadOnlyDictionary<int, int> WaveReachedDistribution { get; }
        public IReadOnlyDictionary<int, int> DefeatWaveDistribution { get; }
        /// <summary>Sum of acquired levels across all runs, by upgrade.</summary>
        public IReadOnlyDictionary<UpgradeId, int> UpgradeLevelTotals { get; }
        /// <summary>Canonical build labels and the number of runs which produced each build.</summary>
        public IReadOnlyDictionary<string, int> BuildDistribution { get; }

        public string ToReport()
        {
            var report = new StringBuilder();
            report.AppendLine("Profile: " + Profile.Label);
            report.AppendLine("Runs: " + RunCount);
            report.AppendLine("Victories: " + Victories + " | Defeats: " + Defeats);
            report.AppendLine("Win rate: " + WinRate.ToString("P1", CultureInfo.InvariantCulture));
            report.AppendLine("Duration (avg / median): " + AverageDurationSeconds.ToString("0.##", CultureInfo.InvariantCulture) + "s / " + MedianDurationSeconds.ToString("0.##", CultureInfo.InvariantCulture) + "s");
            report.AppendLine("Final wave (avg / median): " + AverageFinalWave.ToString("0.##", CultureInfo.InvariantCulture) + " / " + MedianFinalWave.ToString("0.##", CultureInfo.InvariantCulture));
            report.AppendLine("Average kills / leaks / damage: " + AverageEnemiesKilled.ToString("0.##", CultureInfo.InvariantCulture) + " / " + AverageEnemiesLeaked.ToString("0.##", CultureInfo.InvariantCulture) + " / " + AveragePlayerDamageTaken.ToString("0.##", CultureInfo.InvariantCulture));
            report.AppendLine("Most common defeat wave: " + (MostCommonDefeatWave == 0 ? "n/a" : MostCommonDefeatWave.ToString(CultureInfo.InvariantCulture)));
            return report.ToString().TrimEnd();
        }
    }

    public static class RunStatisticsAggregator
    {
        public static RunStatistics Aggregate(BotProfile profile, IReadOnlyList<RunResult> results)
        {
            var durations = new List<float>();
            var waves = new List<int>();
            var reached = new Dictionary<int, int>();
            var defeats = new Dictionary<int, int>();
            var upgradeTotals = new Dictionary<UpgradeId, int>();
            var builds = new Dictionary<string, int>();
            var victories = 0;
            var defeatCount = 0;
            var kills = 0f;
            var leaks = 0f;
            var damage = 0f;

            if (results != null)
            {
                foreach (var result in results)
                {
                    if (result == null) continue;
                    durations.Add(result.DurationSeconds);
                    waves.Add(result.FinalWaveReached);
                    Add(reached, result.FinalWaveReached);
                    if (result.IsVictory) victories++;
                    if (result.IsDefeat)
                    {
                        defeatCount++;
                        Add(defeats, result.FinalWaveReached);
                    }
                    kills += result.EnemiesKilled;
                    leaks += result.EnemiesLeaked;
                    damage += result.PlayerDamageTaken;
                    AddBuild(result.Upgrades, upgradeTotals, builds);
                }
            }

            var count = durations.Count;
            return new RunStatistics(profile, count, victories, defeatCount,
                Average(durations), Median(durations), Average(waves), Median(waves),
                count == 0 ? 0f : kills / count, count == 0 ? 0f : leaks / count, count == 0 ? 0f : damage / count,
                MostCommonKey(defeats), reached, defeats, upgradeTotals, builds);
        }

        static void Add(Dictionary<int, int> distribution, int value)
        {
            distribution.TryGetValue(value, out var count);
            distribution[value] = count + 1;
        }

        static void AddBuild(IReadOnlyList<AcquiredUpgrade> upgrades, Dictionary<UpgradeId, int> totals, Dictionary<string, int> builds)
        {
            var parts = new List<string>();
            if (upgrades != null)
            {
                foreach (var upgrade in upgrades)
                {
                    if (upgrade.Level <= 0) continue;
                    totals.TryGetValue(upgrade.Upgrade, out var total);
                    totals[upgrade.Upgrade] = total + upgrade.Level;
                    parts.Add(upgrade.Upgrade + " " + upgrade.Level);
                }
            }
            parts.Sort(StringComparer.Ordinal);
            var build = parts.Count == 0 ? "No upgrades" : string.Join(", ", parts);
            builds.TryGetValue(build, out var count);
            builds[build] = count + 1;
        }

        static float Average(List<float> values)
        {
            if (values.Count == 0) return 0f;
            var sum = 0f;
            foreach (var value in values) sum += value;
            return sum / values.Count;
        }

        static float Average(List<int> values)
        {
            if (values.Count == 0) return 0f;
            var sum = 0;
            foreach (var value in values) sum += value;
            return (float)sum / values.Count;
        }

        static float Median(List<float> values)
        {
            if (values.Count == 0) return 0f;
            values.Sort();
            var middle = values.Count / 2;
            return values.Count % 2 == 0 ? (values[middle - 1] + values[middle]) * 0.5f : values[middle];
        }

        static float Median(List<int> values)
        {
            if (values.Count == 0) return 0f;
            values.Sort();
            var middle = values.Count / 2;
            return values.Count % 2 == 0 ? (values[middle - 1] + values[middle]) * 0.5f : values[middle];
        }

        static int MostCommonKey(Dictionary<int, int> distribution)
        {
            var key = 0;
            var count = 0;
            foreach (var pair in distribution)
            {
                if (pair.Value > count || (pair.Value == count && pair.Key < key))
                {
                    key = pair.Key;
                    count = pair.Value;
                }
            }
            return key;
        }
    }
}
