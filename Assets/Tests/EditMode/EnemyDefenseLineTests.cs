using NUnit.Framework;
using UnityEngine;

namespace RealRail.Tests
{
    public sealed class EnemyDefenseLineTests
    {
        GameObject _sessionOwner;
        GameObject _player;
        GameObject _enemy;
        GameSession _session;
        Health _playerHealth;
        EnemyMover _mover;
        EnemyDefenseLine _defenseLine;

        [SetUp]
        public void SetUp()
        {
            _sessionOwner = new GameObject("Session");
            _session = _sessionOwner.AddComponent<GameSession>();
            _player = new GameObject("Player");
            _player.transform.position = new Vector3(4f, 1f, 0f);
            _playerHealth = _player.AddComponent<Health>();
            _playerHealth.SetMaxHealth(3);
            _session.BindPlayer(_playerHealth);

            _enemy = new GameObject("Enemy");
            _enemy.transform.position = new Vector3(-2.5f, 1f, 0f);
            _enemy.AddComponent<Health>().SetMaxHealth(1);
            _mover = _enemy.AddComponent<EnemyMover>();
            _defenseLine = _enemy.AddComponent<EnemyDefenseLine>();
            _mover.Initialize(_session, -2.5f, 0f, 1f, 3f);
            _defenseLine.Initialize(_session);
        }

        [TearDown]
        public void TearDown()
        {
            if (_enemy != null)
            {
                Object.DestroyImmediate(_enemy);
            }

            Object.DestroyImmediate(_player);
            Object.DestroyImmediate(_sessionOwner);
        }

        [Test]
        public void DestinationReached_DamagesPlayerOnceRegardlessOfX()
        {
            _mover.Advance(0f);

            Assert.AreEqual(2, _playerHealth.Current);
            Assert.IsTrue(_enemy == null);
        }

        [Test]
        public void HeavyDestinationReached_DamagesPlayerOnceAndRemovesTheEnemy()
        {
            _enemy.GetComponent<Health>().SetMaxHealth(4);

            _mover.Advance(0f);

            Assert.AreEqual(2, _playerHealth.Current);
            Assert.IsTrue(_enemy == null);
        }

        [Test]
        public void DestinationReached_ResolvesWaveEnemyAsRemoved()
        {
            var waveEnemy = _enemy.AddComponent<WaveEnemy>();
            var progress = new WaveProgress(1);
            progress.RegisterSpawned();
            WaveEnemyResolution? resolution = null;
            waveEnemy.Resolved += (_, value) =>
            {
                resolution = value;
                progress.RegisterResolved(value);
            };

            _mover.Advance(0f);

            Assert.AreEqual(WaveEnemyResolution.Removed, resolution);
            Assert.AreEqual(0, progress.KillCount);
            Assert.AreEqual(0, progress.ActiveEnemyCount);
        }
    }
}
