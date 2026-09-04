using NUnit.Framework;
using UnityEngine;

namespace RealRail.Tests
{
    public sealed class PlayerBotTelemetryTests
    {
        readonly System.Collections.Generic.List<Object> _owners = new System.Collections.Generic.List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var owner in _owners)
            {
                if (owner != null) Object.DestroyImmediate(owner);
            }
            _owners.Clear();
        }

        [Test]
        public void Bot_DrivesPlayerMotorTowardClosestUpgradeTargetThroughMotorSeam()
        {
            var lanesOwner = Track(new GameObject("Lanes"));
            var lanes = lanesOwner.AddComponent<LaneLayout>();
            var session = CreateSession();
            var player = Track(new GameObject("Player"));
            var motor = player.AddComponent<PlayerMotor>();
            motor.ConfigureForTests(lanes, session, 10f);
            var bot = player.AddComponent<PlayerBot>();
            bot.ConfigureForTests(motor, session);
            bot.SetBotEnabled(true);

            var threat = Track(new GameObject("Threat"));
            threat.transform.position = new Vector3(-3.25f, 1f, 10f);
            threat.AddComponent<EnemyMover>();
            var target = Track(new GameObject("Upgrade Target"));
            target.transform.position = new Vector3(3.25f, 1f, 20f);
            target.AddComponent<Health>();
            target.AddComponent<EnemyMover>();
            target.AddComponent<UpgradeTarget>();

            bot.Tick();
            motor.Move(motor.AutomatedInput, 0.1f);

            Assert.IsTrue(motor.HasAutomatedInput);
            Assert.Greater(motor.AutomatedInput.x, 0f, "The bot should direct the normal motor toward the prioritized target.");
            Assert.Greater(player.transform.position.x, 0f, "The upgrade target has priority even when a normal threat is closer.");

            bot.SetBotEnabled(false);
            Assert.IsFalse(motor.HasAutomatedInput, "Disabling the bot restores the human input path.");
        }

        [Test]
        public void Bot_SelectsRewardThroughUpgradeRewardSelection()
        {
            var session = CreateSession();
            var owner = Track(new GameObject("Upgrade Systems"));
            var upgrades = owner.AddComponent<UpgradeSystem>();
            var selection = owner.AddComponent<UpgradeRewardSelection>();
            selection.ConfigureForTests(upgrades);
            var bot = owner.AddComponent<PlayerBot>();
            bot.ConfigureForTests(null, session, selection);
            bot.SetBotEnabled(true);

            selection.RequestReward();

            Assert.IsFalse(selection.IsSelecting);
            Assert.AreEqual(1, TotalLevels(upgrades.State));
        }

        [Test]
        public void Bot_ProfilesOnlyChangeDecisionPolicyAndUseOfferedUpgradeChoices()
        {
            var owner = Track(new GameObject("Bot"));
            var bot = owner.AddComponent<PlayerBot>();
            var choices = new[] { UpgradeId.PiercingShot, UpgradeId.RapidFire, UpgradeId.PowerShot };

            bot.SetProfile(BotProfileId.Average);
            Assert.AreEqual("Average", bot.Profile.Label);
            Assert.AreEqual(UpgradeId.RapidFire, bot.ChooseUpgradeForCurrentProfile(choices));

            bot.SetProfile(BotProfileId.Strong);
            Assert.Less(bot.Profile.ReactionIntervalSeconds, BotProfile.FromId(BotProfileId.Average).ReactionIntervalSeconds);
            Assert.AreEqual(UpgradeId.PowerShot, bot.ChooseUpgradeForCurrentProfile(choices));

            bot.SetProfile(BotProfileId.PerfectIsh);
            Assert.Less(bot.Profile.ReactionIntervalSeconds, BotProfile.FromId(BotProfileId.Strong).ReactionIntervalSeconds);
            CollectionAssert.Contains(choices, bot.ChooseUpgradeForCurrentProfile(choices));
        }

        [Test]
        public void Telemetry_RecordsSessionFactsAndAuthoritativeUpgradeBuild()
        {
            var session = CreateSession();
            var owner = Track(new GameObject("Telemetry"));
            var upgrades = owner.AddComponent<UpgradeSystem>();
            var telemetry = owner.AddComponent<RunTelemetry>();
            telemetry.ConfigureForTests(session, upgrades);

            session.ReportWaveStarted(2);
            session.ReportEnemyResolved(WaveEnemyResolution.Killed);
            session.ReportEnemyResolved(WaveEnemyResolution.Removed);
            session.ApplyPlayerDamage(1);
            Assert.IsTrue(upgrades.TryApplyLevel(UpgradeId.PowerShot, out _));
            session.Win();

            var result = telemetry.CurrentResult;
            Assert.IsTrue(result.IsVictory);
            Assert.AreEqual(2, result.FinalWaveReached);
            Assert.AreEqual(1, result.EnemiesKilled);
            Assert.AreEqual(1, result.EnemiesLeaked);
            Assert.AreEqual(1, result.PlayerDamageTaken);
            Assert.AreEqual(1, result.Upgrades.Count);
            Assert.AreEqual(UpgradeId.PowerShot, result.Upgrades[0].Upgrade);
            Assert.AreEqual(1, result.Upgrades[0].Level);
        }

        GameSession CreateSession()
        {
            var owner = Track(new GameObject("Session"));
            var session = owner.AddComponent<GameSession>();
            var player = Track(new GameObject("Player Health"));
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

        static int TotalLevels(UpgradeState state)
        {
            var total = 0;
            foreach (UpgradeId upgrade in System.Enum.GetValues(typeof(UpgradeId))) total += state.GetLevel(upgrade);
            return total;
        }
    }
}
