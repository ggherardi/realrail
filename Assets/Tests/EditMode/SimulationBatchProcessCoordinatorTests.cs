using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace RealRail.Tests
{
    public sealed class SimulationBatchProcessCoordinatorTests
    {
        string _root;

        [TearDown]
        public void TearDown() { if (!string.IsNullOrEmpty(_root) && Directory.Exists(_root)) Directory.Delete(_root, true); }

        [Test]
        public void BoundsPool_ValidatesResultsAndRecordsOverlappingWorkers()
        {
            var clock = new Clock(); var factory = new Factory(); var coordinator = Create(2, clock, factory);
            var first = Enqueue(coordinator, "one", 1); Enqueue(coordinator, "two", 2); var queued = Enqueue(coordinator, "three", 3);
            coordinator.Tick();
            Assert.AreEqual(2, coordinator.ActiveCount);
            Assert.AreEqual(0, first.WorkerSlot);
            Assert.AreEqual(1, coordinator.Executions[1].WorkerSlot);
            Assert.AreEqual(SimulationBatchProcessState.Queued, queued.State);
            clock.Advance(1); factory.Processes[0].Exit(0); WriteSuccess(first); coordinator.Tick();
            Assert.AreEqual(SimulationBatchProcessState.Succeeded, first.State);
            Assert.AreEqual(3, factory.Processes.Count);
            Assert.AreEqual(0, queued.WorkerSlot, "A freed worker-0 project must be reused; active worker-1 remains reserved.");
            clock.Advance(1); factory.Processes[1].Exit(0); WriteSuccess(coordinator.Executions[1]); coordinator.Tick();
            Assert.IsTrue(coordinator.ObservedOverlap);
            Assert.Greater(first.ProcessId, 0);
        }

        [Test]
        public void TimesOutMissingResultsAndCancelsPendingAndActiveProcesses()
        {
            var clock = new Clock(); var factory = new Factory(); var coordinator = Create(1, clock, factory);
            var slow = Enqueue(coordinator, "slow", 1, 1f); coordinator.Tick(); clock.Advance(2); coordinator.Tick();
            Assert.AreEqual(SimulationBatchProcessState.TimedOut, slow.State);
            Assert.IsTrue(factory.Processes[0].Killed);
            var missing = Enqueue(coordinator, "missing", 2); coordinator.Tick(); factory.Processes[1].Exit(0); coordinator.Tick();
            Assert.AreEqual(SimulationBatchProcessState.Failed, missing.State);
            StringAssert.Contains("did not write", missing.Failure);
            var active = Enqueue(coordinator, "active", 3); var queued = Enqueue(coordinator, "queued", 4); coordinator.Tick(); coordinator.Cancel();
            Assert.AreEqual(SimulationBatchProcessState.Cancelled, active.State);
            Assert.AreEqual(SimulationBatchProcessState.Cancelled, queued.State);
        }

        [Test]
        public void ProjectPreparerCopiesOnlyAuthoredInputs()
        {
            var source = Path.Combine(Root(), "source");
            foreach (var name in new[] { "Assets", "Packages", "ProjectSettings", "Library" }) Directory.CreateDirectory(Path.Combine(source, name));
            File.WriteAllText(Path.Combine(source, "Assets", "authored.txt"), "authored");
            var copy = new SimulationWorkerProjectPreparer().Prepare(source, Path.Combine(Root(), "workers"), 0);
            Assert.IsTrue(File.Exists(Path.Combine(copy, "Assets", "authored.txt")));
            Assert.IsFalse(Directory.Exists(Path.Combine(copy, "Library")));
        }

        [Test]
        public void WorkerProtocol_DeepCopiesWaveTriggersAndRunDiagnostics()
        {
            var source = new RunConfiguration(new[] { new WaveConfig(7, .25f, 3.5f, new[] { 2, 5 }, .2f, 3) });
            var request = new SimulationBatchRequest("job", 99, BotProfileId.PerfectIsh, 4f, 30f, source, "experiment", "candidate");
            var restored = SimulationBatchRequest.FromJson(request.ToJson());
            Assert.IsTrue(restored.IsValid(out var failure), failure);
            CollectionAssert.AreEqual(new[] { 2, 5 }, restored.CreateConfiguration().GetWave(0).UpgradeTriggerKillCounts);

            var run = new RunResult(SessionState.Victory, 2f, 1, 3, 0, 1, new[] { new AcquiredUpgrade(UpgradeId.PowerShot, 2) }, 99, new[] { "PowerShot" }, new[] { UpgradeId.PowerShot });
            var result = SimulationBatchResult.Succeeded("job", 99, run, 15d);
            result.experimentId = "experiment"; result.candidateId = "candidate";
            var resultJson = result.ToJson();
            StringAssert.Contains("\"upgradeOffers\":[\"PowerShot\"]", resultJson);
            StringAssert.Contains("\"finalBuild\":[{\"upgrade\":\"PowerShot\",\"level\":2}]", resultJson);
        }

        SimulationBatchProcessCoordinator Create(int workers, Clock clock, Factory factory) => new SimulationBatchProcessCoordinator(workers, Root(), index => "worker-" + index, job => new UnityBatchWorkerCommand { FileName = "Unity", ExecuteMethod = "RealRail.Editor.SimulationBatchWorker.Execute", LogPath = Path.Combine(Root(), job.Request.jobId + ".log") }, factory, () => clock.Utc);
        SimulationBatchProcessExecution Enqueue(SimulationBatchProcessCoordinator coordinator, string id, int seed, float timeout = 0f) => coordinator.Enqueue(new SimulationBatchRequest(id, seed, BotProfileId.Strong, 4f, timeout, new RunConfiguration(new[] { new WaveConfig(2, .2f, 3f) }), "experiment", "candidate"), Path.Combine(Root(), id + ".json"));
        void WriteSuccess(SimulationBatchProcessExecution execution) { var result = SimulationBatchResult.Succeeded(execution.Request.jobId, execution.Request.seed, new RunResult(SessionState.Victory, 1f, 1, 1, 0, 0, Array.Empty<AcquiredUpgrade>(), execution.Request.seed), 1d); result.experimentId = execution.Request.experimentId; result.candidateId = execution.Request.candidateId; result.profile = execution.Request.profile; File.WriteAllText(execution.ResultPath, result.ToJson()); }
        string Root() { if (_root == null) { _root = Path.Combine(Path.GetTempPath(), "RealRail-Coordinator-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_root); } return _root; }

        sealed class Clock { public DateTime Utc { get; private set; } = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc); public void Advance(double seconds) => Utc = Utc.AddSeconds(seconds); }
        sealed class Factory : IUnityBatchProcessFactory { int _next; public readonly List<FakeProcess> Processes = new List<FakeProcess>(); public IUnityBatchProcess Start(UnityBatchWorkerCommand _) { var process = new FakeProcess(++_next); Processes.Add(process); return process; } }
        sealed class FakeProcess : IUnityBatchProcess { public FakeProcess(int id) { Id = id; } public int Id { get; } public bool HasExited { get; private set; } public int ExitCode { get; private set; } public bool Killed { get; private set; } public void Exit(int code) { ExitCode = code; HasExited = true; } public void Kill() { Killed = true; HasExited = true; } }
    }
}
