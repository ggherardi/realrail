using NUnit.Framework;
using UnityEngine;

namespace RealRail.Tests
{
    public sealed class VolcanoImpactEruptionTests
    {
        GameObject _eruptionOwner;
        GameObject _nearEnemy;
        GameObject _farEnemy;

        [TearDown]
        public void TearDown()
        {
            if (_nearEnemy != null) Object.DestroyImmediate(_nearEnemy);
            if (_farEnemy != null) Object.DestroyImmediate(_farEnemy);
            if (_eruptionOwner != null) Object.DestroyImmediate(_eruptionOwner);
        }

        [Test]
        public void Burst_DamagesOnlyUniqueEnemiesInsideItsRadius()
        {
            _eruptionOwner = new GameObject("Eruption");
            var eruption = _eruptionOwner.AddComponent<VolcanoEruption>();
            _nearEnemy = CreateEnemy(new Vector3(0.5f, 0f, 0f), 4);
            _farEnemy = CreateEnemy(new Vector3(4f, 0f, 0f), 4);
            Physics.SyncTransforms();

            var hits = eruption.Burst(2, 1.5f, Vector3.zero);

            Assert.AreEqual(1, hits);
            Assert.AreEqual(2, _nearEnemy.GetComponent<Health>().Current);
            Assert.AreEqual(4, _farEnemy.GetComponent<Health>().Current);
        }

        [Test]
        public void VolcanoWeaponDamage_UsesNormalKilledResolution()
        {
            _eruptionOwner = new GameObject("Eruption");
            var eruption = _eruptionOwner.AddComponent<VolcanoEruption>();
            _nearEnemy = CreateEnemy(Vector3.zero, 1);
            var waveEnemy = _nearEnemy.AddComponent<WaveEnemy>();
            WaveEnemyResolution? resolution = null;
            waveEnemy.Resolved += (_, value) => resolution = value;
            Physics.SyncTransforms();

            eruption.Burst(1, 1f, Vector3.zero);

            Assert.AreEqual(WaveEnemyResolution.Killed, resolution);
        }

        static GameObject CreateEnemy(Vector3 position, int hitPoints)
        {
            var enemy = new GameObject("Enemy") { layer = LayerMask.NameToLayer(GameplayLayers.Enemy) };
            enemy.transform.position = position;
            enemy.AddComponent<SphereCollider>().isTrigger = true;
            var health = enemy.AddComponent<Health>();
            health.SetMaxHealth(hitPoints);
            return enemy;
        }
    }
}
