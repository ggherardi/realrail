using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace RealRail.Editor
{
    /// <summary>Editor-side bridge from bounded candidate jobs to isolated Unity batch workers.</summary>
    public sealed class HeadlessSimulationExperiment
    {
        readonly BalanceExperimentDefinition _definition;
        readonly float _speed;
        readonly string _root;
        readonly SimulationBatchProcessCoordinator _coordinator;
        readonly List<BalanceCandidate> _candidates = new List<BalanceCandidate>();
        readonly Stopwatch _wallClock;
        public event Action<BalanceSearchResult, string> Completed;
        public event Action<string> Failed;
        public bool IsRunning { get; private set; } = true;
        public int CompletedJobs => _coordinator.Executions.Count;
        public int WorkerCount => _coordinator.WorkerCount;
        public int SucceededJobs { get { var count = 0; foreach (var execution in _coordinator.Executions) if (execution.State == SimulationBatchProcessState.Succeeded) count++; return count; } }
        public double WallClockMilliseconds => _wallClock.Elapsed.TotalMilliseconds;
        public float TotalSimulatedSeconds { get; private set; }
        public double RunsPerMinute => WallClockMilliseconds <= 0d ? 0d : SucceededJobs * 60000d / WallClockMilliseconds;
        public bool ObservedOverlap => _coordinator.ObservedOverlap;
        public IReadOnlyList<SimulationBatchProcessExecution> Executions => _coordinator.Executions;

        public HeadlessSimulationExperiment(BalanceExperimentDefinition definition, float speed, int workers)
        {
            _definition = definition; _speed = speed;
            _root = Path.Combine(Application.persistentDataPath, "RealRail", "BatchWorkers", Guid.NewGuid().ToString("N"));
            var source = Directory.GetParent(Application.dataPath).FullName;
            var projects = new string[Mathf.Clamp(workers, 1, 8)];
            var preparer = new SimulationWorkerProjectPreparer();
            for (var i = 0; i < projects.Length; i++) projects[i] = preparer.Prepare(source, Path.Combine(_root, "projects"), i);
            _coordinator = new SimulationBatchProcessCoordinator(projects.Length, Path.Combine(_root, "requests"), i => projects[i], execution => new UnityBatchWorkerCommand { FileName = EditorApplication.applicationPath, ExecuteMethod = "RealRail.Editor.SimulationBatchWorker.Execute", LogPath = Path.Combine(_root, "logs", execution.Request.jobId + ".log") });
            BuildAndEnqueue();
            _wallClock = Stopwatch.StartNew();
            EditorApplication.update += Tick;
        }

        public void Cancel() { _coordinator.Cancel(); FinishFailure("Batch experiment cancelled."); }
        void Tick()
        {
            _coordinator.Tick();
            if (!_coordinator.IsComplete) return;
            var failures = new List<string>(); foreach (var e in _coordinator.Executions) if (e.State != SimulationBatchProcessState.Succeeded) failures.Add(e.Failure);
            if (failures.Count > 0) { FinishFailure(string.Join("\n", failures)); return; }
            try { FinishSuccess(); } catch (Exception e) { FinishFailure(e.Message); }
        }
        void BuildAndEnqueue()
        {
            _candidates.Add(new BalanceCandidate("baseline", _definition.Baseline)); var generator = new BalanceCandidateGenerator(_definition.Constraints, _definition.CandidateSeed);
            for (var i = 0; i < _definition.CandidateCount * _definition.Iterations; i++) _candidates.Add(generator.Generate("candidate-" + i, _definition.Baseline));
            foreach (var c in _candidates) foreach (var p in _definition.Objective.Profiles) foreach (var seed in _definition.Seeds)
            { var id = Guid.NewGuid().ToString("N"); _coordinator.Enqueue(new SimulationBatchRequest(id, seed, p.Profile, _speed, 300f, c.Configuration, "batch-experiment", c.Id), Path.Combine(_root, "results", id + ".json")); }
        }
        void FinishSuccess()
        {
            var map = new Dictionary<string, RunResult>(); foreach (var e in _coordinator.Executions) { map[e.Request.candidateId + "|" + (int)e.Request.profile + "|" + e.Request.seed] = ToRunResult(e.Result); TotalSimulatedSeconds += e.Result.durationSeconds; }
            var evaluator = new MapEvaluator(map); var search = new BalanceSearch(); var baseline = search.EvaluateCandidate(_candidates[0], _definition.Objective, _definition.Seeds, evaluator); var ranked = new List<BalanceCandidateEvaluation>(); for (var i = 1; i < _candidates.Count; i++) ranked.Add(search.EvaluateCandidate(_candidates[i], _definition.Objective, _definition.Seeds, evaluator)); ranked.Sort((a,b) => a.Score != b.Score ? a.Score.CompareTo(b.Score) : string.CompareOrdinal(a.Candidate.Id,b.Candidate.Id));
            var result = new BalanceSearchResult(baseline, ranked); var json = BalanceExperimentJsonReport.Serialize(_definition, result, SucceededJobs, WallClockMilliseconds, TotalSimulatedSeconds, _speed, SimulationExecutionMode.ExternalBatchHeadless, _coordinator.WorkerCount); var path = ExperimentResultStorage.Save(json, "headless-batch-experiment"); Finish(); CleanUpSuccessfulWorkerArtifacts(); Completed?.Invoke(result, path);
        }
        void FinishFailure(string message) { Finish(); Failed?.Invoke(message); }
        void Finish() { if (!IsRunning) return; IsRunning = false; _wallClock.Stop(); EditorApplication.update -= Tick; }
        void CleanUpSuccessfulWorkerArtifacts()
        {
            try
            {
                if (Directory.Exists(_root)) Directory.Delete(_root, true);
            }
            catch (Exception exception) { UnityEngine.Debug.LogWarning("Could not clean generated batch worker artifacts: " + exception.Message); }
        }
        static RunResult ToRunResult(SimulationBatchResult r) => new RunResult(r.victory ? SessionState.Victory : SessionState.Lost, r.durationSeconds, r.finalWaveReached, r.enemiesKilled, r.enemiesLeaked, r.playerDamageTaken, Array.Empty<AcquiredUpgrade>(), r.seed, r.upgradeOffers, Array.Empty<UpgradeId>());
        sealed class MapEvaluator : IRunConfigurationEvaluator { readonly IReadOnlyDictionary<string,RunResult> _map; public MapEvaluator(IReadOnlyDictionary<string,RunResult> map) { _map=map; } public RunResult Evaluate(BalanceEvaluationRequest r) => _map[r.Candidate.Id + "|" + (int)r.Profile + "|" + r.Seed]; }
    }
}
