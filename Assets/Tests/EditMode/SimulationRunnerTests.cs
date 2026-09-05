using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RealRail.Tests
{
    public sealed class SimulationRunnerTests
    {
        readonly System.Collections.Generic.List<Object> _owners = new System.Collections.Generic.List<Object>();
        float _timeScale;

        [SetUp]
        public void SetUp()
        {
            _timeScale = Time.timeScale;
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _timeScale;
            foreach (var owner in _owners)
            {
                if (owner != null) Object.DestroyImmediate(owner);
            }
            _owners.Clear();
        }

        [UnityTest]
        public IEnumerator TerminalRun_PublishesCompletedResultThenRestoresNormalTimeAtBatchLimit()
        {
            var session = CreateSession();
            var telemetryOwner = Track(new GameObject("Telemetry"));
            var telemetry = telemetryOwner.AddComponent<RunTelemetry>();
            telemetry.ConfigureForTests(session, null);
            session.ReportWaveStarted(2);

            var runnerOwner = Track(new GameObject("Runner"));
            var runner = runnerOwner.AddComponent<SimulationRunner>();
            var director = Track(new GameObject("Director")).AddComponent<WaveDirector>();
            runner.ConfigureForTests(session, director, telemetry);
            runner.SetSimulationSpeed(3f);
            SetPrivateField(runner, "maximumRuns", 1);
            RunResult completed = null;
            runner.RunCompleted += result => completed = result;

            runner.StartSimulation();
            yield return null;
            session.ReportWaveStarted(2);
            session.Win();

            Assert.NotNull(completed);
            Assert.IsTrue(completed.IsVictory);
            Assert.AreEqual(2, completed.FinalWaveReached);
            Assert.AreEqual(1, runner.CompletedRunCount);
            Assert.AreEqual(1, runner.Results.Count);
            Assert.NotNull(runner.LatestStatistics);
            Assert.AreEqual(1, runner.LatestStatistics.RunCount);
            Assert.IsFalse(runner.IsRunningBatch);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void RewardSelection_RestoresSimulationSpeedAfterTemporaryPause()
        {
            var owner = Track(new GameObject("Upgrades"));
            var upgrades = owner.AddComponent<UpgradeSystem>();
            var selection = owner.AddComponent<UpgradeRewardSelection>();
            selection.ConfigureForTests(upgrades);

            Time.timeScale = 5f;
            selection.RequestReward();
            Assert.IsTrue(selection.IsSelecting);
            Assert.AreEqual(0f, Time.timeScale);

            Assert.IsTrue(selection.Select(UpgradeId.PowerShot));
            Assert.IsFalse(selection.IsSelecting);
            Assert.AreEqual(5f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator ConfiguredBatch_UsesRequestedCountSpeedAndPlayerBotProfile()
        {
            var session = CreateSession();
            var telemetryOwner = Track(new GameObject("Telemetry"));
            var telemetry = telemetryOwner.AddComponent<RunTelemetry>();
            telemetry.ConfigureForTests(session, null);
            var player = Track(new GameObject("Bot Player"));
            var bot = player.AddComponent<PlayerBot>();
            bot.SetProfile(BotProfileId.PerfectIsh);
            var runner = Track(new GameObject("Runner")).AddComponent<SimulationRunner>();
            var director = Track(new GameObject("Director")).AddComponent<WaveDirector>();
            runner.ConfigureForTests(session, director, telemetry, bot: bot);

            runner.StartSimulation(2, 4f);
            yield return null;

            Assert.IsTrue(runner.IsRunningBatch);
            Assert.AreEqual(4f, runner.SimulationSpeed);
            Assert.AreEqual("Perfect-ish", bot.Profile.Label);
            runner.StopSimulation();
        }

        GameSession CreateSession()
        {
            var sessionOwner = Track(new GameObject("Session"));
            var session = sessionOwner.AddComponent<GameSession>();
            var player = Track(new GameObject("Player"));
            var health = player.AddComponent<Health>();
            health.SetMaxHealth(3);
            session.BindPlayer(health);
            return session;
        }

        T Track<T>(T owner) where T : Object
        {
            _owners.Add(owner);
            return owner;
        }

        static void SetPrivateField(object target, string name, object value)
        {
            target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(target, value);
        }
    }
}
