using NUnit.Framework;
using UnityEngine;

namespace RealRail.Tests
{
    public sealed class VolcanoPrototypeTests
    {
        GameObject _obstacleOwner;
        GameObject _enemyOwner;

        [TearDown]
        public void TearDown()
        {
            if (_enemyOwner != null) Object.DestroyImmediate(_enemyOwner);
            if (_obstacleOwner != null) Object.DestroyImmediate(_obstacleOwner);
        }

        [Test]
        public void PartialObstacle_OffersAStableLaneLocalBypass()
        {
            var obstacle = CreateObstacle(10);
            _obstacleOwner.transform.position = new Vector3(-2.8f, 1f, 18f);

            Assert.IsTrue(obstacle.BlocksForwardPath(new Vector3(-3.2f, 1f, 25f), 0f));
            Assert.IsTrue(obstacle.TryGetBypassX(new Vector3(-3.2f, 1f, 25f), -5.5f, -1f, out var bypass));
            Assert.That(bypass, Is.InRange(-5.5f, -1f));
            Assert.IsFalse(obstacle.BlocksForwardPath(new Vector3(-3.2f, 1f, 12f), 0f));
        }

        [Test]
        public void Engager_DamagesObstacle_AndVolcanicDamageUsesNormalHealth()
        {
            var obstacle = CreateObstacle(3);
            _enemyOwner = new GameObject("Volcano engager");
            var health = _enemyOwner.AddComponent<Health>();
            health.SetMaxHealth(2);
            var mover = _enemyOwner.AddComponent<EnemyMover>();
            _enemyOwner.AddComponent<VolcanoEnemyAttack>();
            obstacle.RegisterEngager(mover);

            obstacle.ProcessEngagers();

            Assert.AreEqual(2, obstacle.Health.Current);
            Assert.AreEqual(1, health.Current);
        }

        [Test]
        public void VolcanoLethalDamage_ResolvesWaveEnemyAsRemoved_NotAProjectileKill()
        {
            var obstacle = CreateObstacle(10);
            _enemyOwner = new GameObject("Volcano wave engager");
            var health = _enemyOwner.AddComponent<Health>();
            health.SetMaxHealth(1);
            var waveEnemy = _enemyOwner.AddComponent<WaveEnemy>();
            _enemyOwner.AddComponent<EnemyMover>();
            _enemyOwner.AddComponent<VolcanoEnemyAttack>();
            WaveEnemyResolution? resolution = null;
            waveEnemy.Resolved += (_, value) => resolution = value;
            obstacle.RegisterEngager(_enemyOwner.GetComponent<EnemyMover>());

            obstacle.ProcessEngagers();

            Assert.AreEqual(WaveEnemyResolution.Removed, resolution);
            Assert.AreEqual(0, health.Current);
        }

        [Test]
        public void HeavyAttackComponent_IsExplicitlyTunable()
        {
            _enemyOwner = new GameObject("Heavy");
            var attack = _enemyOwner.AddComponent<VolcanoEnemyAttack>();
            Assert.AreEqual(1, attack.DamagePerHit);
        }

        VolcanoObstacle CreateObstacle(int hitPoints)
        {
            _obstacleOwner = new GameObject("Volcano");
            _obstacleOwner.AddComponent<BoxCollider>();
            _obstacleOwner.AddComponent<Health>();
            var obstacle = _obstacleOwner.AddComponent<VolcanoObstacle>();
            obstacle.Configure(3.1f, 1.8f, -3.25f, -5.5f, -1f, hitPoints, 1, 1f);
            return obstacle;
        }
    }
}
