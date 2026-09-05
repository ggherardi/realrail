using System.Collections.Generic;
using NUnit.Framework;

namespace RealRail.Tests
{
    public sealed class BalanceSearchTests
    {
        [Test]
        public void Generator_ClampsAllCurrentlyConfigurableDimensions_AndDoesNotMutateSource()
        {
            var source = new RunConfiguration(new[] { new WaveConfig(5, .2f, 2f, new[] { 3 }, .1f, 1) });
            var constraints = new BalanceCandidateConstraints(10, 10, .5f, .5f, 3f, 3f, .4f, .4f, 4, 4);

            var candidate = new BalanceCandidateGenerator(constraints, 12).Generate("generated", source);
            var wave = candidate.Configuration.GetWave(0);

            Assert.AreEqual(10, wave.KillGoal);
            Assert.AreEqual(.5f, wave.SpawnInterval);
            Assert.AreEqual(3f, wave.MoveSpeed);
            Assert.AreEqual(.4f, wave.HeavySpawnChance);
            Assert.AreEqual(4, wave.MaxConcurrentEnemies);
            Assert.AreEqual(5, source.GetWave(0).KillGoal);
            Assert.AreEqual(3, source.GetWave(0).UpgradeTriggerKillCounts[0]);
        }

        [Test]
        public void Search_PreservesSeparateProfileStatisticsAndScoresRanges()
        {
            var definition = Definition(candidateCount: 0, iterations: 0,
                objective: new BalanceObjective(new[]
                {
                    new BalanceProfileObjective(BotProfileId.Average, .4f, .6f, 10f, 20f),
                    new BalanceProfileObjective(BotProfileId.PerfectIsh, .8f, 1f, 30f, 40f)
                }));

            var result = new BalanceSearch().Execute(definition, new FixtureEvaluator());

            Assert.AreEqual(2, result.Baseline.Profiles.Count);
            Assert.AreEqual(0f, result.Baseline.Profiles[0].WinRatePenalty);
            Assert.AreEqual(0f, result.Baseline.Profiles[0].DurationPenalty);
            Assert.AreEqual(.3f, result.Baseline.Profiles[1].WinRatePenalty);
            Assert.AreEqual(10f, result.Baseline.Profiles[1].DurationPenalty);
            Assert.AreEqual(10.3f, result.Baseline.Score);
        }

        [Test]
        public void Search_IsBoundedReproduciblyRanked_AndKeepsBaselineUnchanged()
        {
            var baseline = new RunConfiguration(new[] { new WaveConfig(20, .4f, 4f, new[] { 7 }, .1f, 2) });
            var objective = new BalanceObjective(new[] { new BalanceProfileObjective(BotProfileId.Average, .5f, .5f) });
            var definition = new BalanceExperimentDefinition(baseline, objective, new BalanceCandidateConstraints(1, 40, .01f, 1f, 0f, 8f, 0f, 1f, 0, 10), new[] { 3, 8 }, 3, 2, 46);

            var first = new BalanceSearch().Execute(definition, new CandidateSensitiveEvaluator());
            var second = new BalanceSearch().Execute(definition, new CandidateSensitiveEvaluator());

            Assert.AreEqual(6, first.RankedCandidates.Count);
            for (var index = 0; index < first.RankedCandidates.Count; index++)
            {
                Assert.AreEqual(first.RankedCandidates[index].Candidate.Id, second.RankedCandidates[index].Candidate.Id);
                Assert.AreEqual(first.RankedCandidates[index].Score, second.RankedCandidates[index].Score);
            }
            Assert.AreEqual(20, baseline.GetWave(0).KillGoal);
            Assert.AreEqual(.4f, baseline.GetWave(0).SpawnInterval);
            Assert.AreEqual(7, baseline.GetWave(0).UpgradeTriggerKillCounts[0]);
        }

        [Test]
        public void Search_EvaluatesEveryCandidateProfileAndSeedExactlyOnce()
        {
            var evaluator = new CountingEvaluator();
            var objective = new BalanceObjective(new[]
            {
                new BalanceProfileObjective(BotProfileId.Average, 0f, 1f),
                new BalanceProfileObjective(BotProfileId.Strong, 0f, 1f)
            });

            new BalanceSearch().Execute(Definition(2, 1, objective), evaluator);

            // Baseline plus two candidates, two profiles, and two explicit seed jobs.
            Assert.AreEqual(12, evaluator.Requests.Count);
            CollectionAssert.AreEquivalent(new[] { 10, 20 }, evaluator.Seeds);
        }

        static BalanceExperimentDefinition Definition(int candidateCount, int iterations, BalanceObjective objective)
        {
            return new BalanceExperimentDefinition(new RunConfiguration(new[] { new WaveConfig(20, .3f, 4f) }), objective,
                new BalanceCandidateConstraints(), new[] { 10, 20 }, candidateCount, iterations, 99);
        }

        sealed class FixtureEvaluator : IRunConfigurationEvaluator
        {
            public RunResult Evaluate(BalanceEvaluationRequest request)
            {
                var perfect = request.Profile == BotProfileId.PerfectIsh;
                // The fixed seed set produces a controlled 50% win rate for both profiles.
                return new RunResult(request.Seed == 10 ? SessionState.Victory : SessionState.Lost, perfect ? 20f : 15f, 1, 1, 0, 0, null);
            }
        }

        sealed class CandidateSensitiveEvaluator : IRunConfigurationEvaluator
        {
            public RunResult Evaluate(BalanceEvaluationRequest request)
            {
                var goal = request.Candidate.Configuration.GetWave(0).KillGoal;
                return new RunResult(goal % 2 == 0 ? SessionState.Victory : SessionState.Lost, 1f, 1, goal, 0, 0, null);
            }
        }

        sealed class CountingEvaluator : IRunConfigurationEvaluator
        {
            public readonly List<BalanceEvaluationRequest> Requests = new List<BalanceEvaluationRequest>();
            public readonly HashSet<int> Seeds = new HashSet<int>();
            public RunResult Evaluate(BalanceEvaluationRequest request)
            {
                Requests.Add(request); Seeds.Add(request.Seed);
                return new RunResult(SessionState.Victory, 1f, 1, 1, 0, 0, null);
            }
        }
    }
}
