using NUnit.Framework;

namespace RealRail.Tests
{
    public sealed class WaveProgressTests
    {
        [Test]
        public void ProjectileKill_CountsTowardGoalAndResolvesEnemy()
        {
            var progress = new WaveProgress(2);
            progress.RegisterSpawned();
            progress.RegisterSpawned();

            progress.RegisterResolved(WaveEnemyResolution.Killed);

            Assert.AreEqual(1, progress.KillCount);
            Assert.AreEqual(1, progress.ActiveEnemyCount);
            Assert.IsFalse(progress.KillGoalReached);
            Assert.IsFalse(progress.IsComplete);
        }

        [Test]
        public void ReachingPlayer_ResolvesEnemyWithoutCountingKill()
        {
            var progress = new WaveProgress(1);
            progress.RegisterSpawned();

            progress.RegisterResolved(WaveEnemyResolution.Removed);

            Assert.AreEqual(0, progress.KillCount);
            Assert.AreEqual(0, progress.ActiveEnemyCount);
            Assert.IsFalse(progress.KillGoalReached);
            Assert.IsFalse(progress.IsComplete);
        }

        [Test]
        public void GoalReached_WaitsForRemainingEnemiesToResolve()
        {
            var progress = new WaveProgress(1);
            progress.RegisterSpawned();
            progress.RegisterSpawned();

            progress.RegisterResolved(WaveEnemyResolution.Killed);

            Assert.IsTrue(progress.KillGoalReached);
            Assert.AreEqual(1, progress.ActiveEnemyCount);
            Assert.IsFalse(progress.IsComplete);

            progress.RegisterResolved(WaveEnemyResolution.Removed);

            Assert.IsTrue(progress.IsComplete);
        }

        [Test]
        public void KillsBeyondGoal_DoNotChangeKillCount()
        {
            var progress = new WaveProgress(1);
            progress.RegisterSpawned();
            progress.RegisterSpawned();

            progress.RegisterResolved(WaveEnemyResolution.Killed);
            progress.RegisterResolved(WaveEnemyResolution.Killed);

            Assert.AreEqual(1, progress.KillCount);
            Assert.AreEqual(0, progress.ActiveEnemyCount);
            Assert.IsTrue(progress.IsComplete);
        }

        [Test]
        public void UpgradeTrigger_IsConsumedOnceWhenKillThresholdIsReached()
        {
            var progress = new WaveProgress(20);
            for (var index = 0; index < 7; index++)
            {
                progress.RegisterSpawned();
                progress.RegisterResolved(WaveEnemyResolution.Killed);
            }

            Assert.IsFalse(progress.TryConsumeUpgradeTrigger(8));

            progress.RegisterSpawned();
            progress.RegisterResolved(WaveEnemyResolution.Killed);

            Assert.IsTrue(progress.TryConsumeUpgradeTrigger(8));
            Assert.IsFalse(progress.TryConsumeUpgradeTrigger(8));
        }

        [Test]
        public void UpgradeTrigger_DoesNotCountRemovedEnemies()
        {
            var progress = new WaveProgress(20);
            for (var index = 0; index < 8; index++)
            {
                progress.RegisterSpawned();
                progress.RegisterResolved(WaveEnemyResolution.Removed);
            }

            Assert.IsFalse(progress.TryConsumeUpgradeTrigger(8));
        }

        [Test]
        public void UpgradeTrigger_CanBeDisabled()
        {
            var progress = new WaveProgress(1);
            progress.RegisterSpawned();
            progress.RegisterResolved(WaveEnemyResolution.Killed);

            Assert.IsFalse(progress.TryConsumeUpgradeTrigger(0));
        }
    }
}
