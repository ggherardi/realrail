using System;
using NUnit.Framework;

namespace RealRail.Tests
{
    public sealed class RunConfigurationTests
    {
        [Test]
        public void RuntimePlan_PreservesEveryConfiguredWaveParameter()
        {
            var source = new[]
            {
                new WaveConfig(20, 0.35f, 3.6f, new[] { 8 }),
                new WaveConfig(40, 0.22f, 4f, new[] { 14, 28 }, 0.10f, 5),
                new WaveConfig(70, 0.14f, 4.4f, new[] { 21, 46 }, 0.15f)
            };

            var run = new RunConfiguration(source);
            source[0].KillGoal = 999;
            source[1].UpgradeTriggerKillCounts[0] = 999;

            Assert.IsTrue(run.IsValid);
            Assert.AreEqual(3, run.WaveCount);
            AssertWave(run.GetWave(0), 20, 0.35f, 3.6f, 0f, 0, new[] { 8 });
            AssertWave(run.GetWave(1), 40, 0.22f, 4f, 0.10f, 5, new[] { 14, 28 });
            AssertWave(run.GetWave(2), 70, 0.14f, 4.4f, 0.15f, 0, new[] { 21, 46 });
        }

        [Test]
        public void RuntimePlan_HandlesEmptyAndInvalidWaveIndexesSafely()
        {
            var run = new RunConfiguration(null);

            Assert.IsFalse(run.IsValid);
            Assert.AreEqual(0, run.WaveCount);
            Assert.Throws<ArgumentOutOfRangeException>(() => run.GetWave(0));
        }

        static void AssertWave(
            WaveConfig wave,
            int killGoal,
            float spawnInterval,
            float speed,
            float heavyChance,
            int maxConcurrentEnemies,
            int[] triggers)
        {
            Assert.AreEqual(killGoal, wave.KillGoal);
            Assert.AreEqual(spawnInterval, wave.SpawnInterval);
            Assert.AreEqual(speed, wave.MoveSpeed);
            Assert.AreEqual(heavyChance, wave.HeavySpawnChance);
            Assert.AreEqual(maxConcurrentEnemies, wave.MaxConcurrentEnemies);
            CollectionAssert.AreEqual(triggers, wave.UpgradeTriggerKillCounts);
        }
    }
}
