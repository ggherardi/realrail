using NUnit.Framework;
using UnityEngine;
using RealRail;

namespace RealRail.Tests
{
    public sealed class HealthTests
    {
        Health _health;
        GameObject _owner;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("HealthHost");
            _health = _owner.AddComponent<Health>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
        }

        [Test]
        public void TakeDamage_ReducesCurrentHealth()
        {
            _health.SetMaxHealth(5);
            _health.TakeDamage(2);
            Assert.AreEqual(3, _health.Current);
            Assert.AreEqual(5, _health.Max);
        }

        [Test]
        public void TakeDamage_DoesNotDropBelowZero()
        {
            _health.SetMaxHealth(2);
            _health.TakeDamage(10);
            Assert.AreEqual(0, _health.Current);
        }

        [Test]
        public void TakeDamage_FiresDiedOnce()
        {
            _health.SetMaxHealth(2);
            var died = 0;
            _health.Died += () => died++;

            _health.TakeDamage(2);
            _health.TakeDamage(1);

            Assert.AreEqual(1, died);
            Assert.AreEqual(0, _health.Current);
        }

        [Test]
        public void TakeDamage_IgnoresNonPositiveAmounts()
        {
            _health.SetMaxHealth(4);
            _health.TakeDamage(0);
            _health.TakeDamage(-3);
            Assert.AreEqual(4, _health.Current);
        }
    }
}
