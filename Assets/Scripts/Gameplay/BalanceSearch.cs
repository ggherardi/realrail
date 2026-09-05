using System;
using System.Collections.Generic;

namespace RealRail
{
    /// <summary>Desired outcome ranges for one bot policy. Scores are penalties: zero is inside every requested range.</summary>
    public sealed class BalanceProfileObjective
    {
        public BalanceProfileObjective(BotProfileId profile, float minimumWinRate, float maximumWinRate,
            float minimumAverageDurationSeconds = 0f, float maximumAverageDurationSeconds = float.PositiveInfinity,
            float weight = 1f)
        {
            if (minimumWinRate < 0f || maximumWinRate > 1f || minimumWinRate > maximumWinRate)
                throw new ArgumentOutOfRangeException(nameof(minimumWinRate));
            if (minimumAverageDurationSeconds < 0f || maximumAverageDurationSeconds < minimumAverageDurationSeconds)
                throw new ArgumentOutOfRangeException(nameof(minimumAverageDurationSeconds));
            if (weight < 0f) throw new ArgumentOutOfRangeException(nameof(weight));

            Profile = profile;
            MinimumWinRate = minimumWinRate;
            MaximumWinRate = maximumWinRate;
            MinimumAverageDurationSeconds = minimumAverageDurationSeconds;
            MaximumAverageDurationSeconds = maximumAverageDurationSeconds;
            Weight = weight;
        }

        public BotProfileId Profile { get; }
        public float MinimumWinRate { get; }
        public float MaximumWinRate { get; }
        public float MinimumAverageDurationSeconds { get; }
        public float MaximumAverageDurationSeconds { get; }
        public float Weight { get; }
    }

    /// <summary>Explicit, profile-separated description of a balance experiment; it is not a production difficulty setting.</summary>
    public sealed class BalanceObjective
    {
        readonly BalanceProfileObjective[] _profiles;

        public BalanceObjective(IReadOnlyList<BalanceProfileObjective> profiles)
        {
            if (profiles == null || profiles.Count == 0) throw new ArgumentException("At least one profile objective is required.", nameof(profiles));
            _profiles = new BalanceProfileObjective[profiles.Count];
            var seen = new HashSet<BotProfileId>();
            for (var index = 0; index < profiles.Count; index++)
            {
                if (profiles[index] == null) throw new ArgumentException("Profile objectives cannot contain null.", nameof(profiles));
                if (!seen.Add(profiles[index].Profile)) throw new ArgumentException("Each bot profile may have only one objective.", nameof(profiles));
                _profiles[index] = profiles[index];
            }
        }

        public IReadOnlyList<BalanceProfileObjective> Profiles => _profiles;
    }

    /// <summary>Bounds for the dimensions currently exposed by <see cref="WaveConfig"/>. Future dimensions belong in a new constraint type.</summary>
    public sealed class BalanceCandidateConstraints
    {
        public BalanceCandidateConstraints(int minimumKillGoal = 1, int maximumKillGoal = 200,
            float minimumSpawnInterval = 0.01f, float maximumSpawnInterval = 3f,
            float minimumMoveSpeed = 0f, float maximumMoveSpeed = 12f,
            float minimumHeavySpawnChance = 0f, float maximumHeavySpawnChance = 1f,
            int minimumMaxConcurrentEnemies = 0, int maximumMaxConcurrentEnemies = 30)
        {
            if (minimumKillGoal < 1 || maximumKillGoal < minimumKillGoal || minimumSpawnInterval < 0.01f || maximumSpawnInterval < minimumSpawnInterval ||
                minimumMoveSpeed < 0f || maximumMoveSpeed < minimumMoveSpeed || minimumHeavySpawnChance < 0f || maximumHeavySpawnChance > 1f ||
                minimumHeavySpawnChance > maximumHeavySpawnChance || minimumMaxConcurrentEnemies < 0 || maximumMaxConcurrentEnemies < minimumMaxConcurrentEnemies)
                throw new ArgumentOutOfRangeException(nameof(minimumKillGoal));
            MinimumKillGoal = minimumKillGoal; MaximumKillGoal = maximumKillGoal;
            MinimumSpawnInterval = minimumSpawnInterval; MaximumSpawnInterval = maximumSpawnInterval;
            MinimumMoveSpeed = minimumMoveSpeed; MaximumMoveSpeed = maximumMoveSpeed;
            MinimumHeavySpawnChance = minimumHeavySpawnChance; MaximumHeavySpawnChance = maximumHeavySpawnChance;
            MinimumMaxConcurrentEnemies = minimumMaxConcurrentEnemies; MaximumMaxConcurrentEnemies = maximumMaxConcurrentEnemies;
        }

        public int MinimumKillGoal { get; } public int MaximumKillGoal { get; }
        public float MinimumSpawnInterval { get; } public float MaximumSpawnInterval { get; }
        public float MinimumMoveSpeed { get; } public float MaximumMoveSpeed { get; }
        public float MinimumHeavySpawnChance { get; } public float MaximumHeavySpawnChance { get; }
        public int MinimumMaxConcurrentEnemies { get; } public int MaximumMaxConcurrentEnemies { get; }

        public WaveConfig Clamp(WaveConfig wave)
        {
            return new WaveConfig(Math.Max(MinimumKillGoal, Math.Min(MaximumKillGoal, wave.KillGoal)),
                Math.Max(MinimumSpawnInterval, Math.Min(MaximumSpawnInterval, wave.SpawnInterval)),
                Math.Max(MinimumMoveSpeed, Math.Min(MaximumMoveSpeed, wave.MoveSpeed)),
                wave.UpgradeTriggerKillCounts == null ? Array.Empty<int>() : (int[])wave.UpgradeTriggerKillCounts.Clone(),
                Math.Max(MinimumHeavySpawnChance, Math.Min(MaximumHeavySpawnChance, wave.HeavySpawnChance)),
                Math.Max(MinimumMaxConcurrentEnemies, Math.Min(MaximumMaxConcurrentEnemies, wave.MaxConcurrentEnemies)));
        }
    }

    public sealed class BalanceCandidate
    {
        public BalanceCandidate(string id, RunConfiguration configuration)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("A candidate id is required.", nameof(id));
            Id = id;
            Configuration = Copy(configuration ?? throw new ArgumentNullException(nameof(configuration)));
        }
        public string Id { get; }
        public RunConfiguration Configuration { get; }
        internal static RunConfiguration Copy(RunConfiguration configuration)
        {
            var waves = new WaveConfig[configuration.WaveCount];
            for (var index = 0; index < waves.Length; index++) waves[index] = configuration.GetWave(index);
            return new RunConfiguration(waves);
        }
    }

    /// <summary>Deterministic, constrained mutation generator. It never writes to the source configuration or authored assets.</summary>
    public sealed class BalanceCandidateGenerator
    {
        readonly BalanceCandidateConstraints _constraints;
        readonly Random _random;
        public BalanceCandidateGenerator(BalanceCandidateConstraints constraints, int seed)
        {
            _constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
            _random = new Random(seed);
        }
        public BalanceCandidate Generate(string id, RunConfiguration source)
        {
            if (source == null || !source.IsValid) throw new ArgumentException("A valid source run is required.", nameof(source));
            var waves = new WaveConfig[source.WaveCount];
            for (var index = 0; index < waves.Length; index++)
            {
                var wave = source.GetWave(index);
                waves[index] = _constraints.Clamp(new WaveConfig(
                    Vary(wave.KillGoal, Math.Max(1, wave.KillGoal / 3)),
                    Vary(wave.SpawnInterval, Math.Max(0.01f, wave.SpawnInterval * .35f)),
                    Vary(wave.MoveSpeed, Math.Max(.05f, wave.MoveSpeed * .25f)),
                    wave.UpgradeTriggerKillCounts,
                    Vary(wave.HeavySpawnChance, .2f),
                    Vary(wave.MaxConcurrentEnemies, Math.Max(1, wave.MaxConcurrentEnemies == 0 ? 2 : wave.MaxConcurrentEnemies / 2))));
            }
            return new BalanceCandidate(id, new RunConfiguration(waves));
        }
        int Vary(int value, int amount) => value + _random.Next(-amount, amount + 1);
        float Vary(float value, float amount) => value + ((float)_random.NextDouble() * 2f - 1f) * amount;
    }

    public readonly struct BalanceEvaluationRequest
    {
        public BalanceEvaluationRequest(BalanceCandidate candidate, BotProfileId profile, int seed)
        { Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate)); Profile = profile; Seed = seed; }
        public BalanceCandidate Candidate { get; } public BotProfileId Profile { get; } public int Seed { get; }
    }

    /// <summary>Bridge to the authoritative deterministic simulation runner. Search code does not invent combat results.</summary>
    public interface IRunConfigurationEvaluator { RunResult Evaluate(BalanceEvaluationRequest request); }

    public sealed class BalanceProfileEvaluation
    {
        public BalanceProfileEvaluation(BotProfileId profile, RunStatistics statistics, float winRatePenalty, float durationPenalty, float weightedScore)
        { Profile = profile; Statistics = statistics; WinRatePenalty = winRatePenalty; DurationPenalty = durationPenalty; WeightedScore = weightedScore; }
        public BotProfileId Profile { get; } public RunStatistics Statistics { get; }
        public float WinRatePenalty { get; } public float DurationPenalty { get; } public float WeightedScore { get; }
    }

    public sealed class BalanceCandidateEvaluation
    {
        public BalanceCandidateEvaluation(BalanceCandidate candidate, IReadOnlyList<BalanceProfileEvaluation> profiles, float score)
        { Candidate = candidate; Profiles = profiles; Score = score; }
        public BalanceCandidate Candidate { get; } public IReadOnlyList<BalanceProfileEvaluation> Profiles { get; }
        /// <summary>Lower is better; zero means all configured profile ranges were met.</summary>
        public float Score { get; }
    }

    public sealed class BalanceExperimentDefinition
    {
        readonly int[] _seeds;
        public BalanceExperimentDefinition(RunConfiguration baseline, BalanceObjective objective, BalanceCandidateConstraints constraints,
            IReadOnlyList<int> seeds, int candidateCount, int iterations, int candidateSeed)
        {
            Baseline = BalanceCandidate.Copy(baseline ?? throw new ArgumentNullException(nameof(baseline)));
            Objective = objective ?? throw new ArgumentNullException(nameof(objective)); Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
            if (seeds == null || seeds.Count == 0 || candidateCount < 0 || iterations < 0) throw new ArgumentOutOfRangeException(nameof(seeds));
            _seeds = new int[seeds.Count]; for (var index = 0; index < seeds.Count; index++) _seeds[index] = seeds[index];
            CandidateCount = candidateCount; Iterations = iterations; CandidateSeed = candidateSeed;
        }
        public RunConfiguration Baseline { get; } public BalanceObjective Objective { get; } public BalanceCandidateConstraints Constraints { get; }
        public IReadOnlyList<int> Seeds => _seeds; public int CandidateCount { get; } public int Iterations { get; } public int CandidateSeed { get; }
    }

    public sealed class BalanceSearchResult
    {
        public BalanceSearchResult(BalanceCandidateEvaluation baseline, IReadOnlyList<BalanceCandidateEvaluation> rankedCandidates)
        { Baseline = baseline; RankedCandidates = rankedCandidates; }
        public BalanceCandidateEvaluation Baseline { get; } public IReadOnlyList<BalanceCandidateEvaluation> RankedCandidates { get; }
        public BalanceCandidateEvaluation BestCandidate => RankedCandidates.Count == 0 ? null : RankedCandidates[0];
    }

    /// <summary>A bounded guided-mutation search. Candidate evaluation order and tie-breaking are stable for a deterministic evaluator.</summary>
    public sealed class BalanceSearch
    {
        public BalanceSearchResult Execute(BalanceExperimentDefinition definition, IRunConfigurationEvaluator evaluator)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition)); if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            var baseline = new BalanceCandidate("baseline", definition.Baseline);
            var evaluated = new List<BalanceCandidateEvaluation>();
            var baselineEvaluation = EvaluateCandidate(baseline, definition.Objective, definition.Seeds, evaluator);
            var generator = new BalanceCandidateGenerator(definition.Constraints, definition.CandidateSeed);
            var source = definition.Baseline;
            var bestScore = baselineEvaluation.Score;
            var ordinal = 0;
            for (var iteration = 0; iteration < definition.Iterations; iteration++)
            {
                BalanceCandidateEvaluation iterationBest = null;
                for (var candidateIndex = 0; candidateIndex < definition.CandidateCount; candidateIndex++)
                {
                    var candidate = generator.Generate("candidate-" + ordinal++, source);
                    var candidateEvaluation = EvaluateCandidate(candidate, definition.Objective, definition.Seeds, evaluator);
                    evaluated.Add(candidateEvaluation);
                    if (iterationBest == null || candidateEvaluation.Score < iterationBest.Score ||
                        (candidateEvaluation.Score == iterationBest.Score && string.CompareOrdinal(candidate.Id, iterationBest.Candidate.Id) < 0))
                        iterationBest = candidateEvaluation;
                }
                // A better candidate becomes the next generation's mutation source. The baseline is
                // only a read-only control: this changes no authored configuration or scene asset.
                if (iterationBest != null && iterationBest.Score < bestScore)
                {
                    bestScore = iterationBest.Score;
                    source = iterationBest.Candidate.Configuration;
                }
            }
            evaluated.Sort((left, right) => { var score = left.Score.CompareTo(right.Score); return score != 0 ? score : string.CompareOrdinal(left.Candidate.Id, right.Candidate.Id); });
            return new BalanceSearchResult(baselineEvaluation, evaluated);
        }

        /// <summary>Scores externally-selected candidates too, enabling authoritative asynchronous runners to feed the same objective model.</summary>
        public BalanceCandidateEvaluation EvaluateCandidate(BalanceCandidate candidate, BalanceObjective objective, IReadOnlyList<int> seeds, IRunConfigurationEvaluator evaluator)
        {
            var evaluations = new List<BalanceProfileEvaluation>(); var total = 0f;
            foreach (var profileObjective in objective.Profiles)
            {
                var results = new List<RunResult>();
                foreach (var seed in seeds)
                {
                    var result = evaluator.Evaluate(new BalanceEvaluationRequest(candidate, profileObjective.Profile, seed));
                    if (result == null) throw new InvalidOperationException("Evaluator returned no RunResult.");
                    results.Add(result);
                }
                var statistics = RunStatisticsAggregator.Aggregate(BotProfile.FromId(profileObjective.Profile), results);
                var winPenalty = RangePenalty(statistics.WinRate, profileObjective.MinimumWinRate, profileObjective.MaximumWinRate);
                var durationPenalty = RangePenalty(statistics.AverageDurationSeconds, profileObjective.MinimumAverageDurationSeconds, profileObjective.MaximumAverageDurationSeconds);
                var weighted = (winPenalty + durationPenalty) * profileObjective.Weight;
                total += weighted;
                evaluations.Add(new BalanceProfileEvaluation(profileObjective.Profile, statistics, winPenalty, durationPenalty, weighted));
            }
            return new BalanceCandidateEvaluation(candidate, evaluations, total);
        }
        static float RangePenalty(float value, float minimum, float maximum) => value < minimum ? minimum - value : value > maximum ? value - maximum : 0f;
    }
}
