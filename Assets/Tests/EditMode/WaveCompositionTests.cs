using NUnit.Framework;

namespace RealRail.Tests
{
    public sealed class WaveCompositionTests
    {
        [Test]
        public void HeavySelection_UsesAnInclusiveZeroAndExclusiveChanceThreshold()
        {
            var config = new WaveConfig(1, 1f, 4f, heavySpawnChance: 0.10f);

            Assert.IsTrue(config.ShouldSpawnHeavy(0f));
            Assert.IsTrue(config.ShouldSpawnHeavy(0.0999f));
            Assert.IsFalse(config.ShouldSpawnHeavy(0.10f));
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
