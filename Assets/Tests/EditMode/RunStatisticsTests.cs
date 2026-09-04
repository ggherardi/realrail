using NUnit.Framework;

namespace RealRail.Tests
{
    public sealed class RunStatisticsTests
    {
        [Test]
        public void Aggregate_EmptyResultsIsSafeAndReadable()
        {
            var statistics = RunStatisticsAggregator.Aggregate(BotProfile.FromId(BotProfileId.Average), null);

            Assert.AreEqual(0, statistics.RunCount);
            Assert.AreEqual(0f, statistics.WinRate);
            Assert.AreEqual(0f, statistics.MedianDurationSeconds);
            Assert.AreEqual(0, statistics.MostCommonDefeatWave);
            StringAssert.Contains("Profile: Average", statistics.ToReport());
        }

        [Test]
        public void Aggregate_ReportsOutcomeMediansDistributionsAndBuilds()
        {
            var results = new[]
            {
                Result(SessionState.Victory, 8f, 4, 12, 1, 2, new AcquiredUpgrade(UpgradeId.PowerShot, 1)),
                Result(SessionState.Lost, 4f, 2, 3, 2, 3, new AcquiredUpgrade(UpgradeId.RapidFire, 2)),
                Result(SessionState.Lost, 6f, 2, 6, 4, 1, new AcquiredUpgrade(UpgradeId.PowerShot, 2))
            };

            var statistics = RunStatisticsAggregator.Aggregate(BotProfile.FromId(BotProfileId.Strong), results);

            Assert.AreEqual(3, statistics.RunCount);
            Assert.AreEqual(1, statistics.Victories);
            Assert.AreEqual(2, statistics.Defeats);
            Assert.AreEqual(1f / 3f, statistics.WinRate);
            Assert.AreEqual(6f, statistics.MedianDurationSeconds);
            Assert.AreEqual(2f, statistics.MedianFinalWave);
            Assert.AreEqual(2, statistics.MostCommonDefeatWave);
            Assert.AreEqual(2, statistics.WaveReachedDistribution[2]);
            Assert.AreEqual(2, statistics.DefeatWaveDistribution[2]);
            Assert.AreEqual(3, statistics.UpgradeLevelTotals[UpgradeId.PowerShot]);
            Assert.AreEqual(3, statistics.BuildDistribution.Count);
            StringAssert.Contains("Win rate: 33.3", statistics.ToReport());
        }

        static RunResult Result(SessionState outcome, float duration, int wave, int kills, int leaks, int damage, params AcquiredUpgrade[] upgrades)
        {
            return new RunResult(outcome, duration, wave, kills, leaks, damage, upgrades);
        }
    }
}
