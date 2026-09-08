using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RealRail.Tests
{
    public sealed class CryoStormPrototypeTests
    {
        GameObject _enemy;
        EnemyMover _mover;
        FrostStatus _frost;

        [SetUp]
        public void SetUp()
        {
            _enemy = new GameObject("Cryo target");
            _mover = _enemy.AddComponent<EnemyMover>();
            _frost = _enemy.AddComponent<FrostStatus>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemy);
        }

        [Test]
        public void Frost_UsesTemporaryMultiplierWithoutMutatingBaseSpeed_AndExpires()
        {
            _mover.ApplyTemporarySpeedMultiplier(0.55f, 3.5f);
            _frost.Apply(3.5f);

            Assert.AreEqual(4f, _mover.BaseSpeed);
            Assert.AreEqual(2.2f, _mover.EffectiveSpeed, 0.001f);
            Assert.IsTrue(_frost.IsFrosted);

            _mover.RefreshTemporaryEffects(float.MaxValue);

            Assert.AreEqual(4f, _mover.BaseSpeed);
            Assert.AreEqual(4f, _mover.EffectiveSpeed);
            Assert.IsFalse(_frost.IsFrosted);
        }

        [Test]
        public void CryoPrototype_UsesBoundedNonDamageTuning_WithoutUpgradeState()
        {
            var owner = new GameObject("Cryo Storm prototype");
            var prototype = owner.AddComponent<CryoStormPrototype>();
            var upgrades = new UpgradeState();

            prototype.ConfigureForTests(10, 4.5f, 0.11f, 0.55f, 3.5f);

            Assert.AreEqual(10, prototype.MaximumTargets);
            Assert.AreEqual(4.5f, prototype.ChainRange);
            Assert.AreEqual(0.11f, prototype.StepDelaySeconds);
            Assert.AreEqual(0.55f, prototype.FrostSpeedMultiplier);
            Assert.AreEqual(3.5f, prototype.FrostDurationSeconds);
            Assert.AreEqual(0, upgrades.GetLevel(UpgradeId.PowerShot));
            Assert.AreEqual(0, upgrades.GetLevel(UpgradeId.PiercingShot));
            Object.DestroyImmediate(owner);
        }

        [Test]
        public void FrostStatus_AloneDoesNotResolveWaveEnemyOrCreateProjectileKills()
        {
            var waveEnemy = _enemy.AddComponent<WaveEnemy>();
            var resolved = false;
            waveEnemy.Resolved += (_, _) => resolved = true;

            _mover.ApplyTemporarySpeedMultiplier(0.55f, 3.5f);
            _frost.Apply(3.5f);

            Assert.IsFalse(resolved);
            Assert.IsTrue(_frost.IsFrosted);
        }

        [Test]
        public void ChainSelection_AffectsDistinctNearbyEnemies_AndRespectsItsBound()
        {
            var prototypeOwner = new GameObject("Cryo chain");
            var prototype = prototypeOwner.AddComponent<CryoStormPrototype>();
            prototype.ConfigureForTests(3, 4.5f, 0.11f, 0.55f, 3.5f);
            var candidates = new List<GameObject>();
            for (var index = 0; index < 5; index++) candidates.Add(CreateWaveEnemy(index * 1.2f));
            Physics.SyncTransforms();

            var visited = (HashSet<WaveEnemy>)typeof(CryoStormPrototype)
                .GetField("_visited", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(prototype);
            var current = candidates[0].GetComponent<WaveEnemy>();
            for (var index = 0; index < prototype.MaximumTargets; index++)
            {
                Assert.IsTrue(visited.Add(current), "A chain target must not repeat.");
                current = InvokeNearest(prototype, current.transform.position);
                if (index < prototype.MaximumTargets - 1) Assert.NotNull(current);
            }

            Assert.AreEqual(3, visited.Count);
            Assert.IsFalse(visited.Contains(candidates[3].GetComponent<WaveEnemy>()), "The configured cap must leave later candidates untouched.");
            DestroyCandidates(candidates);
            Object.DestroyImmediate(prototypeOwner);
        }

        [Test]
        public void ChainSelection_SafelySkipsAnEnemyThatDisappearsBeforeTheNextStep()
        {
            var prototypeOwner = new GameObject("Cryo chain");
            var prototype = prototypeOwner.AddComponent<CryoStormPrototype>();
            prototype.ConfigureForTests(10, 4.5f, 0.11f, 0.55f, 3.5f);
            var candidates = new List<GameObject> { CreateWaveEnemy(0f), CreateWaveEnemy(1f), CreateWaveEnemy(2f) };
            Physics.SyncTransforms();

            var visited = (HashSet<WaveEnemy>)typeof(CryoStormPrototype)
                .GetField("_visited", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(prototype);
            var source = candidates[0].GetComponent<WaveEnemy>();
            visited.Add(source);
            var next = InvokeNearest(prototype, source.transform.position);
            Assert.NotNull(next);
            Object.DestroyImmediate(next.gameObject);
            Physics.SyncTransforms();

            WaveEnemy remaining = null;
            Assert.DoesNotThrow(() => remaining = InvokeNearest(prototype, source.transform.position));
            Assert.AreSame(candidates[2].GetComponent<WaveEnemy>(), remaining);
            Object.DestroyImmediate(candidates[0]);
            Object.DestroyImmediate(candidates[2]);
            Object.DestroyImmediate(prototypeOwner);
        }

        static WaveEnemy InvokeNearest(CryoStormPrototype prototype, Vector3 position) =>
            (WaveEnemy)typeof(CryoStormPrototype).GetMethod("FindNearestUnvisited", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(prototype, new object[] { position });

        static GameObject CreateWaveEnemy(float x)
        {
            var owner = new GameObject("Chain candidate") { layer = LayerMask.NameToLayer(GameplayLayers.Enemy) };
            owner.transform.position = new Vector3(x, 0f, 0f);
            owner.AddComponent<CapsuleCollider>().isTrigger = true;
            owner.AddComponent<WaveEnemy>();
            return owner;
        }

        static void DestroyCandidates(IEnumerable<GameObject> candidates)
        {
            foreach (var candidate in candidates) Object.DestroyImmediate(candidate);
        }
    }
}
