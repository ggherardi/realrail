using NUnit.Framework;
using UnityEngine;

namespace RealRail.Tests
{
    public sealed class GameSessionTests
    {
        GameObject _owner;
        GameObject _player;
        GameSession _session;
        Health _health;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("SessionHost");
            _session = _owner.AddComponent<GameSession>();
            _player = new GameObject("Player");
            _health = _player.AddComponent<Health>();
            _health.SetMaxHealth(1);
            _session.BindPlayer(_health);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_player);
            Object.DestroyImmediate(_owner);
        }

        [Test]
        public void PlayerDeath_SetsLostAndRaisesEventOnce()
        {
            var lost = 0;
            _session.Lost += () => lost++;

            _health.TakeDamage(1);
            _health.TakeDamage(1);

            Assert.AreEqual(SessionState.Lost, _session.State);
            Assert.IsFalse(_session.IsPlaying);
            Assert.AreEqual(1, lost);
        }

        [Test]
        public void Win_SetsVictoryAndRaisesEventOnce()
        {
            var victories = 0;
            _session.Victory += () => victories++;

            _session.Win();
            _session.Win();

            Assert.AreEqual(SessionState.Victory, _session.State);
            Assert.IsFalse(_session.IsPlaying);
            Assert.AreEqual(1, victories);
        }

        [Test]
        public void LostSession_CannotBecomeVictory()
        {
            _health.TakeDamage(1);

            _session.Win();

            Assert.AreEqual(SessionState.Lost, _session.State);
        }

        [Test]
        public void ApplyPlayerDamage_UsesBoundPlayerHealthOnlyWhilePlaying()
        {
            _health.SetMaxHealth(3);

            _session.ApplyPlayerDamage(1);
            _session.ApplyPlayerDamage(0);

            Assert.AreEqual(2, _health.Current);

            _session.Win();
            _session.ApplyPlayerDamage(1);

            Assert.AreEqual(2, _health.Current);
        }

        [Test]
        public void GodMode_InterceptsOnlyPlayerDamage()
        {
            _health.SetMaxHealth(3);
            var upgrades = _owner.AddComponent<UpgradeSystem>();
            var progress = new WaveProgress(2);
            progress.RegisterSpawned();
            progress.RegisterResolved(WaveEnemyResolution.Killed);
            var enemy = new GameObject("Enemy").AddComponent<Health>();
            enemy.SetMaxHealth(4);

            _session.ApplyPlayerDamage(1);
            Assert.AreEqual(2, _health.Current);

            _session.SetGodMode(true);
            _session.ApplyPlayerDamage(1);
            Assert.AreEqual(2, _health.Current);
            Assert.AreEqual(4, enemy.Current);
            Assert.AreEqual(0, upgrades.State.GetLevel(UpgradeId.PowerShot));
            Assert.AreEqual(1, progress.KillCount);

            _session.SetGodMode(false);
            _session.ApplyPlayerDamage(1);
            Assert.AreEqual(1, _health.Current);
            Object.DestroyImmediate(enemy.gameObject);
        }
    }
}
