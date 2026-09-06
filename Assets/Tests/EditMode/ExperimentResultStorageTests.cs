using System;
using System.IO;
using NUnit.Framework;

namespace RealRail.Tests
{
    public sealed class ExperimentResultStorageTests
    {
        [Test]
        public void Json_PersistsConfiguredObjectiveAndMeasuredMetricsOutsideProject()
        {
            var baseline = new RunConfiguration(new[] { new WaveConfig(20, .3f, 4f) });
            var objective = new BalanceObjective(new[] { new BalanceProfileObjective(BotProfileId.Average, .2f, .4f, 10f, 30f) });
            var definition = new BalanceExperimentDefinition(baseline, objective, new BalanceCandidateConstraints(), new[] { 123 }, 1, 1, 123);
            var candidate = new BalanceCandidate("baseline", baseline);
            var statistics = RunStatisticsAggregator.Aggregate(BotProfile.FromId(BotProfileId.Average), new[] { new RunResult(SessionState.Lost, 40f, 1, 1, 1, 1, Array.Empty<AcquiredUpgrade>(), 123) });
            var evaluation = new BalanceCandidateEvaluation(candidate, new[] { new BalanceProfileEvaluation(BotProfileId.Average, statistics, .2f, 10f, 10.2f) }, 10.2f);
            var result = new BalanceSearchResult(evaluation, Array.Empty<BalanceCandidateEvaluation>());
            var json = BalanceExperimentJsonReport.Serialize(definition, result, 1, 2500d, 40f, 4f, SimulationExecutionMode.AuthoredSceneMainThreadAccelerated, 1);
            var directory = Path.Combine(Path.GetTempPath(), "realrail-experiment-result-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                var path = ExperimentResultStorage.Save(json, "fixture", directory);
                StringAssert.Contains("\"minimumWinRate\": 0.2", json);
                StringAssert.Contains("\"wallClockMilliseconds\": 2500.0", json);
                Assert.IsTrue(File.Exists(path));
                Assert.IsFalse(path.StartsWith(Directory.GetCurrentDirectory(), StringComparison.Ordinal));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
