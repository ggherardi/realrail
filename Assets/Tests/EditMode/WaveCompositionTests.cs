using NUnit.Framework;

namespace RealRail.Tests
{
    public sealed class WaveCompositionTests
    {
        [Test]
        public void HeavySelection_UsesAnInclusiveZeroAndExclusiveChanceThreshold()
        {
            var config = new WaveConfig(1, 1f, 4f, heavySpawnChance: 0.15f);

            Assert.IsTrue(config.ShouldSpawnHeavy(0f));
            Assert.IsTrue(config.ShouldSpawnHeavy(0.1499f));
            Assert.IsFalse(config.ShouldSpawnHeavy(0.15f));
            Assert.IsFalse(config.ShouldSpawnHeavy(-0.01f));
        }

        [Test]
        public void HeavySelection_ClampsConfiguredChance()
        {
            var none = new WaveConfig(1, 1f, 4f, heavySpawnChance: -1f);
            var all = new WaveConfig(1, 1f, 4f, heavySpawnChance: 2f);

            Assert.IsFalse(none.ShouldSpawnHeavy(0f));
            Assert.IsTrue(all.ShouldSpawnHeavy(0.9999f));
        }
    }
}
