using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RealRail.Tests
{
    public sealed class UpgradeSystemTests
    {
        sealed class FixedRandom : IUpgradeRandom
        {
            readonly int _value;
            public FixedRandom(int value) => _value = value;
            public int Next(int exclusiveMax) => _value;
        }

        [Test]
        public void State_StartsAtZeroAndHonorsAllCaps()
        {
            var state = new UpgradeState();
            Assert.AreEqual(1, state.GetMaxLevel(UpgradeId.DoubleShot));
            Assert.AreEqual(3, state.GetMaxLevel(UpgradeId.RapidFire));
            Assert.AreEqual(2, state.GetMaxLevel(UpgradeId.PiercingShot));
            Assert.AreEqual(2, state.GetMaxLevel(UpgradeId.PowerShot));

            foreach (UpgradeId upgrade in System.Enum.GetValues(typeof(UpgradeId)))
            {
                Assert.AreEqual(0, state.GetLevel(upgrade));
                while (state.TryApplyLevel(upgrade, out _)) { }
                Assert.AreEqual(state.GetMaxLevel(upgrade), state.GetLevel(upgrade));
                Assert.IsFalse(state.CanBeOffered(upgrade));
            }
        }

        [TestCase(0, 0.35f)]
        [TestCase(1, 0.30f)]
        [TestCase(2, 0.25f)]
        [TestCase(3, 0.20f)]
        public void RapidFire_UsesExactDerivedInterval(int level, float expected)
        {
            var state = new UpgradeState();
            for (var index = 0; index < level; index++) state.TryApplyLevel(UpgradeId.RapidFire, out _);
            Assert.AreEqual(expected, state.DeriveShotConfiguration().FireInterval, 0.0001f);
            while (state.TryApplyLevel(UpgradeId.RapidFire, out _)) { }
            Assert.AreEqual(0.20f, state.DeriveShotConfiguration().FireInterval, 0.0001f);
        }

        [Test]
        public void DerivedShotConfiguration_ComposesUpgradeLevels()
        {
            var state = new UpgradeState();
            state.TryApplyLevel(UpgradeId.DoubleShot, out _);
            state.TryApplyLevel(UpgradeId.RapidFire, out _);
            state.TryApplyLevel(UpgradeId.RapidFire, out _);
            state.TryApplyLevel(UpgradeId.PiercingShot, out _);
            state.TryApplyLevel(UpgradeId.PiercingShot, out _);
            state.TryApplyLevel(UpgradeId.PowerShot, out _);

            var shot = state.DeriveShotConfiguration();
            Assert.AreEqual(2, shot.ProjectileCount);
            Assert.AreEqual(0.25f, shot.FireInterval);
            Assert.AreEqual(2, shot.Damage);
            Assert.AreEqual(3, shot.DistinctHitCapacity);
        }

        [Test]
        public void EligibleRewards_ExcludeCappedUpgradesAndDeterministicSelectionAppliesOne()
        {
            var state = new UpgradeState();
            state.TryApplyLevel(UpgradeId.DoubleShot, out _);
            var eligible = UpgradeRewardGenerator.GetEligible(state);
            CollectionAssert.AreEquivalent(new[] { UpgradeId.RapidFire, UpgradeId.PiercingShot, UpgradeId.PowerShot }, eligible);
            Assert.IsTrue(UpgradeRewardGenerator.TrySelectAutomatic(eligible, new FixedRandom(1), out var selection));
            Assert.AreEqual(UpgradeId.PiercingShot, selection);
        }

        [Test]
        public void RunPool_DefaultsToCurrentFourAndNeverMutatesWhenAnUpgradeCaps()
        {
            var pool = RunUpgradePool.CreateCurrentGameplayPool();
            CollectionAssert.AreEquivalent(new[] { UpgradeId.DoubleShot, UpgradeId.RapidFire, UpgradeId.PiercingShot, UpgradeId.PowerShot }, pool.Upgrades);
            var state = new UpgradeState();
            while (state.TryApplyLevel(UpgradeId.DoubleShot, out _)) { }

            CollectionAssert.DoesNotContain(UpgradeRewardGenerator.GetEligible(pool, state), UpgradeId.DoubleShot);
            CollectionAssert.Contains(pool.Upgrades, UpgradeId.DoubleShot);
        }

        [Test]
        public void CandidateGeneration_UsesPoolMembershipCountsAndDistinctRandomChoices()
        {
            var pool = RunUpgradePool.CreateCurrentGameplayPool();
            var state = new UpgradeState();
            var four = UpgradeRewardGenerator.GenerateCandidates(pool, state, new FixedRandom(0));
            Assert.AreEqual(3, four.Count);
            Assert.AreEqual(3, new HashSet<UpgradeId>(four).Count);
            CollectionAssert.IsSubsetOf(four, pool.Upgrades);

            state.TryApplyLevel(UpgradeId.DoubleShot, out _);
            Assert.AreEqual(3, UpgradeRewardGenerator.GenerateCandidates(pool, state, new FixedRandom(0)).Count);
            while (state.TryApplyLevel(UpgradeId.RapidFire, out _)) { }
            Assert.AreEqual(2, UpgradeRewardGenerator.GenerateCandidates(pool, state, new FixedRandom(0)).Count);
            while (state.TryApplyLevel(UpgradeId.PiercingShot, out _)) { }
            Assert.AreEqual(1, UpgradeRewardGenerator.GenerateCandidates(pool, state, new FixedRandom(0)).Count);
            while (state.TryApplyLevel(UpgradeId.PowerShot, out _)) { }
            Assert.AreEqual(0, UpgradeRewardGenerator.GenerateCandidates(pool, state, new FixedRandom(0)).Count);
        }

        [Test]
        public void CandidateGeneration_CanUseReducedFutureDraftLikePool()
        {
            var pool = new RunUpgradePool(new[] { UpgradeId.DoubleShot, UpgradeId.PiercingShot, UpgradeId.PiercingShot });
            var choices = UpgradeRewardGenerator.GenerateCandidates(pool, new UpgradeState(), new FixedRandom(0));
            CollectionAssert.AreEquivalent(new[] { UpgradeId.DoubleShot, UpgradeId.PiercingShot }, choices);
        }

        [Test]
        public void Selection_AppliesOnceQueuesSimultaneousRewardsAndSkipsZeroEligible()
        {
            var owner = new GameObject("Upgrade reward selection");
            var system = owner.AddComponent<UpgradeSystem>();
            system.SetRewardRandomForTests(new FixedRandom(0));
            var selection = owner.AddComponent<UpgradeRewardSelection>();
            selection.ConfigureForTests(system);

            selection.RequestReward();
            selection.RequestReward();
            Assert.IsTrue(selection.IsSelecting);
            Assert.AreEqual(1, selection.PendingRewardCount);
            Assert.IsTrue(selection.Select(UpgradeId.DoubleShot));
            Assert.AreEqual(1, system.State.GetLevel(UpgradeId.DoubleShot));
            Assert.IsFalse(selection.Select(UpgradeId.DoubleShot));
            Assert.IsTrue(selection.IsSelecting, "The queued reward opens only after the first has resolved.");

            foreach (UpgradeId upgrade in System.Enum.GetValues(typeof(UpgradeId))) while (system.State.TryApplyLevel(upgrade, out _)) { }
            selection.Select(UpgradeId.RapidFire);
            Assert.IsFalse(selection.IsSelecting);
            selection.RequestReward();
            Assert.IsFalse(selection.IsSelecting);
            Assert.AreEqual(0, selection.PendingRewardCount);
            Object.DestroyImmediate(owner);
        }

        [Test]
        public void RewardSystem_AllCappedIsSafeAndDoesNotEmitReward()
        {
            var owner = new UnityEngine.GameObject("Upgrades");
            var system = owner.AddComponent<UpgradeSystem>();
            foreach (UpgradeId upgrade in System.Enum.GetValues(typeof(UpgradeId)))
                while (system.State.TryApplyLevel(upgrade, out _)) { }

            Assert.IsFalse(system.TryApplyAutomaticReward(out _));
            UnityEngine.Object.DestroyImmediate(owner);
        }

        [Test]
        public void DebugApplication_UsesAuthoritativeStateAndRespectsCaps()
        {
            var owner = new GameObject("Upgrades");
            var system = owner.AddComponent<UpgradeSystem>();

            foreach (UpgradeId upgrade in System.Enum.GetValues(typeof(UpgradeId)))
            {
                Assert.IsTrue(system.TryApplyLevel(upgrade, out var application));
                Assert.AreEqual(upgrade, application.Upgrade);
                Assert.AreEqual(1, application.Level);
                while (system.TryApplyLevel(upgrade, out _)) { }
                Assert.IsFalse(system.TryApplyLevel(upgrade, out _));
                Assert.AreEqual(system.State.GetMaxLevel(upgrade), system.State.GetLevel(upgrade));
            }

            Object.DestroyImmediate(owner);
        }

        [Test]
        public void ResetUpgrades_ReturnsMixedBuildAndConfigurationToBaseline()
        {
            var owner = new GameObject("Upgrades");
            var system = owner.AddComponent<UpgradeSystem>();
            system.TryApplyLevel(UpgradeId.DoubleShot, out _);
            system.TryApplyLevel(UpgradeId.RapidFire, out _);
            system.TryApplyLevel(UpgradeId.RapidFire, out _);
            system.TryApplyLevel(UpgradeId.PiercingShot, out _);
            system.TryApplyLevel(UpgradeId.PiercingShot, out _);
            system.TryApplyLevel(UpgradeId.PowerShot, out _);

            system.ResetUpgrades();

            foreach (UpgradeId upgrade in System.Enum.GetValues(typeof(UpgradeId))) Assert.AreEqual(0, system.State.GetLevel(upgrade));
            var shot = system.GetShotConfiguration();
            Assert.AreEqual(1, shot.ProjectileCount);
            Assert.AreEqual(0.35f, shot.FireInterval, 0.0001f);
            Assert.AreEqual(1, shot.Damage);
            Assert.AreEqual(1, shot.DistinctHitCapacity);
            Object.DestroyImmediate(owner);
        }

        [Test]
        public void WaveProgress_ConsumesEachDistinctConfiguredOpportunityOnce()
        {
            var progress = new WaveProgress(50);
            for (var index = 0; index < 28; index++)
            {
                progress.RegisterSpawned();
                progress.RegisterResolved(WaveEnemyResolution.Killed);
            }

            Assert.IsTrue(progress.TryConsumeUpgradeTrigger(14));
            Assert.IsTrue(progress.TryConsumeUpgradeTrigger(28));
            Assert.IsFalse(progress.TryConsumeUpgradeTrigger(14));
            Assert.IsFalse(progress.TryConsumeUpgradeTrigger(46));
        }

        [Test]
        public void PiercingProjectile_HitsDistinctTargetsOnlyAndResolvesAtCapacity()
        {
            var sessionOwner = new UnityEngine.GameObject("Session");
            var session = sessionOwner.AddComponent<GameSession>();
            var projectileOwner = new UnityEngine.GameObject("Projectile");
            var projectile = projectileOwner.AddComponent<Projectile>();
            projectile.Initialize(session, 2, 2);
            var firstOwner = new UnityEngine.GameObject("First");
            var first = firstOwner.AddComponent<Health>();
            first.SetMaxHealth(4);
            var secondOwner = new UnityEngine.GameObject("Second");
            var second = secondOwner.AddComponent<Health>();
            second.SetMaxHealth(4);

            Assert.IsTrue(projectile.TryApplyHit(first));
            Assert.IsFalse(projectile.TryApplyHit(first));
            Assert.AreEqual(2, first.Current, "Repeated callbacks must not re-damage a target.");
            Assert.IsFalse(projectile.IsResolved);
            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("Destroy may not be called from edit mode"));
            Assert.IsTrue(projectile.TryApplyHit(second));
            Assert.AreEqual(2, second.Current);
            Assert.AreEqual(2, projectile.DistinctHitCount);
            Assert.IsTrue(projectile.IsResolved);

            UnityEngine.Object.DestroyImmediate(secondOwner);
            UnityEngine.Object.DestroyImmediate(firstOwner);
            UnityEngine.Object.DestroyImmediate(projectileOwner);
            UnityEngine.Object.DestroyImmediate(sessionOwner);
        }
    }
}
