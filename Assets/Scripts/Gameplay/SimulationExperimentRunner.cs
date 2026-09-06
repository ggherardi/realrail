using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RealRail
{
    /// <summary>
    /// Runs a deliberately small, bounded balance experiment through <see cref="SimulationRunner"/>.
    /// Every sample remains ordinary scene gameplay; Unity objects are never simulated from a worker thread.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationExperimentRunner : MonoBehaviour
    {
        [SerializeField] SimulationRunner simulationRunner;
        [SerializeField] PlayerBot playerBot;

        readonly List<ExperimentJob> _jobs = new List<ExperimentJob>();
        readonly Dictionary<string, RunResult> _results = new Dictionary<string, RunResult>();
        Coroutine _routine;
        Stopwatch _wallClock;

        public bool IsRunning => _routine != null;
        public double WallClockMilliseconds => _wallClock != null ? _wallClock.Elapsed.TotalMilliseconds : 0d;
        public BalanceSearchResult LatestResult { get; private set; }
        public string LatestJson { get; private set; }
        public string LatestResultPath { get; private set; }
        public int CompletedJobCount => _results.Count;
        public float TotalSimulatedSeconds { get; private set; }
        public int WorkerCount => 1;
        public SimulationExecutionMode ExecutionMode => SimulationExecutionMode.AuthoredSceneMainThreadAccelerated;
        public double RunsPerMinute => WallClockMilliseconds <= 0d ? 0d : CompletedJobCount * 60000d / WallClockMilliseconds;
        public string Failure { get; private set; }
        public event Action<BalanceSearchResult> ExperimentCompleted;

        public void ConfigureForTests(SimulationRunner runner, PlayerBot bot)
        {
            simulationRunner = runner;
            playerBot = bot;
        }

        public bool StartExperiment(BalanceExperimentDefinition definition, float speed)
        {
            if (IsRunning || definition == null || simulationRunner == null || playerBot == null || speed <= 0f)
                return false;

            LatestResult = null;
            LatestJson = null;
            LatestResultPath = null;
            TotalSimulatedSeconds = 0f;
            Failure = null;
            _jobs.Clear();
            _results.Clear();
            BuildJobs(definition);
            _wallClock = Stopwatch.StartNew();
            _routine = StartCoroutine(Execute(definition, speed));
            return true;
        }

        public void StopExperiment()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            simulationRunner?.StopSimulation();
            _wallClock?.Stop();
        }

        void OnDisable() => StopExperiment();

        void BuildJobs(BalanceExperimentDefinition definition)
        {
            var candidates = new List<BalanceCandidate> { new BalanceCandidate("baseline", definition.Baseline) };
            var generator = new BalanceCandidateGenerator(definition.Constraints, definition.CandidateSeed);
            var count = definition.CandidateCount * definition.Iterations;
            for (var index = 0; index < count; index++)
                candidates.Add(generator.Generate("candidate-" + index, definition.Baseline));

            foreach (var candidate in candidates)
            foreach (var profile in definition.Objective.Profiles)
            foreach (var seed in definition.Seeds)
                _jobs.Add(new ExperimentJob(candidate, profile.Profile, seed));
        }

        IEnumerator Execute(BalanceExperimentDefinition definition, float speed)
        {
            foreach (var job in _jobs)
            {
                playerBot.SetProfile(job.Profile);
                simulationRunner.SetSimulationSeed(job.Seed);
                simulationRunner.StartSimulation(1, speed, job.Candidate.Configuration);
                while (simulationRunner.IsRunningBatch) yield return null;

                if (simulationRunner.Results.Count != 1)
                {
                    Failure = "A simulation job completed without exactly one result.";
                    StopExperiment();
                    yield break;
                }
                _results[Key(job.Candidate.Id, job.Profile, job.Seed)] = simulationRunner.Results[0];
                TotalSimulatedSeconds += simulationRunner.Results[0].DurationSeconds;
            }

            _wallClock.Stop();
            var evaluator = new CollectedResultEvaluator(_results);
            var search = new BalanceSearch();
            var baseline = new BalanceCandidate("baseline", definition.Baseline);
            var ranked = new List<BalanceCandidateEvaluation>();
            var seen = new HashSet<string>();
            foreach (var job in _jobs)
            {
                if (job.Candidate.Id != baseline.Id && seen.Add(job.Candidate.Id))
                    ranked.Add(search.EvaluateCandidate(job.Candidate, definition.Objective, definition.Seeds, evaluator));
            }
            ranked.Sort((left, right) => left.Score != right.Score ? left.Score.CompareTo(right.Score) : string.CompareOrdinal(left.Candidate.Id, right.Candidate.Id));
            LatestResult = new BalanceSearchResult(search.EvaluateCandidate(baseline, definition.Objective, definition.Seeds, evaluator), ranked);
            LatestJson = BalanceExperimentJsonReport.Serialize(definition, LatestResult, _jobs.Count, WallClockMilliseconds, TotalSimulatedSeconds, speed, ExecutionMode, WorkerCount);
            try { LatestResultPath = ExperimentResultStorage.Save(LatestJson, "balance-experiment"); }
            catch (Exception exception) { Failure = "Could not save experiment JSON: " + exception.Message; }
            _routine = null;
            ExperimentCompleted?.Invoke(LatestResult);
        }

        static string Key(string candidateId, BotProfileId profile, int seed) => candidateId + "|" + (int)profile + "|" + seed;

        readonly struct ExperimentJob
        {
            public ExperimentJob(BalanceCandidate candidate, BotProfileId profile, int seed) { Candidate = candidate; Profile = profile; Seed = seed; }
            public BalanceCandidate Candidate { get; }
            public BotProfileId Profile { get; }
            public int Seed { get; }
        }

        sealed class CollectedResultEvaluator : IRunConfigurationEvaluator
        {
            readonly IReadOnlyDictionary<string, RunResult> _results;
            public CollectedResultEvaluator(IReadOnlyDictionary<string, RunResult> results) => _results = results;
            public RunResult Evaluate(BalanceEvaluationRequest request)
            {
                if (!_results.TryGetValue(Key(request.Candidate.Id, request.Profile, request.Seed), out var result))
                    throw new InvalidOperationException("Missing simulation result for " + request.Candidate.Id + ".");
                return result;
            }
        }
    }

    /// <summary>Stable, dependency-free JSON for an entire completed balance experiment.</summary>
    public static class BalanceExperimentJsonReport
    {
        [Serializable] sealed class ProfileDto { public string profile; public int runs; public float winRate; public float averageDurationSeconds; public float winRatePenalty; public float durationPenalty; public float score; }
        [Serializable] sealed class WaveDto { public int killGoal; public float spawnInterval; public float moveSpeed; public float heavySpawnChance; public int maxConcurrentEnemies; }
        [Serializable] sealed class CandidateDto { public string id; public float score; public WaveDto[] waves; public ProfileDto[] profiles; }
        [Serializable] sealed class ObjectiveProfileDto { public string profile; public float minimumWinRate; public float maximumWinRate; public float minimumDurationSeconds; public float maximumDurationSeconds; public float weight; }
        [Serializable] sealed class ExperimentDto { public string experimentId; public string createdUtc; public int[] seeds; public int candidateCount; public int iterations; public float simulationSpeed; public string executionMode; public int workerCount; public int jobs; public double wallClockMilliseconds; public float totalSimulatedSeconds; public double runsPerMinute; public ObjectiveProfileDto[] objective; public CandidateDto baseline; public CandidateDto[] candidates; }

        public static string Serialize(BalanceExperimentDefinition definition, BalanceSearchResult result, int jobs, double wallClockMilliseconds, float totalSimulatedSeconds, float speed, SimulationExecutionMode mode, int workers)
        {
            if (definition == null || result == null) throw new ArgumentNullException(definition == null ? nameof(definition) : nameof(result));
            var candidates = new CandidateDto[result.RankedCandidates.Count];
            for (var index = 0; index < candidates.Length; index++) candidates[index] = Convert(result.RankedCandidates[index]);
            var objective = new ObjectiveProfileDto[definition.Objective.Profiles.Count];
            for (var index = 0; index < objective.Length; index++) { var p = definition.Objective.Profiles[index]; objective[index] = new ObjectiveProfileDto { profile = p.Profile.ToString(), minimumWinRate = p.MinimumWinRate, maximumWinRate = p.MaximumWinRate, minimumDurationSeconds = p.MinimumAverageDurationSeconds, maximumDurationSeconds = p.MaximumAverageDurationSeconds, weight = p.Weight }; }
            var seeds = new int[definition.Seeds.Count]; for (var index = 0; index < seeds.Length; index++) seeds[index] = definition.Seeds[index];
            return JsonUtility.ToJson(new ExperimentDto { experimentId = "balance-experiment", createdUtc = DateTime.UtcNow.ToString("O"), seeds = seeds, candidateCount = definition.CandidateCount, iterations = definition.Iterations, simulationSpeed = speed, executionMode = mode.ToString(), workerCount = workers, jobs = jobs, wallClockMilliseconds = wallClockMilliseconds, totalSimulatedSeconds = totalSimulatedSeconds, runsPerMinute = wallClockMilliseconds <= 0d ? 0d : jobs * 60000d / wallClockMilliseconds, objective = objective, baseline = Convert(result.Baseline), candidates = candidates }, true);
        }

        static CandidateDto Convert(BalanceCandidateEvaluation evaluation)
        {
            var profiles = new ProfileDto[evaluation.Profiles.Count];
            for (var index = 0; index < profiles.Length; index++)
            {
                var profile = evaluation.Profiles[index];
                profiles[index] = new ProfileDto { profile = profile.Profile.ToString(), runs = profile.Statistics.RunCount, winRate = profile.Statistics.WinRate, averageDurationSeconds = profile.Statistics.AverageDurationSeconds, winRatePenalty = profile.WinRatePenalty, durationPenalty = profile.DurationPenalty, score = profile.WeightedScore };
            }
            var waves = new WaveDto[evaluation.Candidate.Configuration.WaveCount];
            for (var index = 0; index < waves.Length; index++) { var wave = evaluation.Candidate.Configuration.GetWave(index); waves[index] = new WaveDto { killGoal = wave.KillGoal, spawnInterval = wave.SpawnInterval, moveSpeed = wave.MoveSpeed, heavySpawnChance = wave.HeavySpawnChance, maxConcurrentEnemies = wave.MaxConcurrentEnemies }; }
            return new CandidateDto { id = evaluation.Candidate.Id, score = evaluation.Score, waves = waves, profiles = profiles };
        }
    }
}
