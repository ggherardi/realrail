using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RealRail
{
    public enum SimulationJobState { Queued, Running, Succeeded, Failed, Cancelled, TimedOut }
    public enum SimulationExecutionMode
    {
        AuthoredSceneMainThreadAccelerated,
        ExternalBatchHeadless
    }

    /// <summary>Describes process-level fan-out. A worker never shares a Unity scene with another worker.</summary>
    public sealed class SimulationWorkerPlan
    {
        public SimulationWorkerPlan(int requestedWorkerCount)
        {
            RequestedWorkerCount = Mathf.Max(1, requestedWorkerCount);
        }
        public int RequestedWorkerCount { get; }
        /// <summary>One is authoritative for this process because Unity objects are main-thread only.</summary>
        public int InProcessWorkerCount => 1;
        public bool RequiresSeparateProcesses => RequestedWorkerCount > 1;
        public bool IsValid => RequestedWorkerCount > 0;
    }

    /// <summary>Immutable request that may safely be queued, serialized, and replayed by seed.</summary>
    public sealed class SimulationJob
    {
        public SimulationJob(string id, int seed, float timeoutSeconds = 0f, SimulationExecutionMode mode = SimulationExecutionMode.AuthoredSceneMainThreadAccelerated)
        {
            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
            Seed = seed;
            TimeoutSeconds = Mathf.Max(0f, timeoutSeconds);
            Mode = mode;
        }
        public string Id { get; }
        public int Seed { get; }
        public float TimeoutSeconds { get; }
        public SimulationExecutionMode Mode { get; }
    }

    /// <summary>Result lifecycle owned by the main-thread scheduler; no Unity API is called off-thread.</summary>
    public sealed class SimulationJobExecution
    {
        readonly Stopwatch _stopwatch = new Stopwatch();
        public SimulationJobExecution(SimulationJob job) => Job = job ?? throw new ArgumentNullException(nameof(job));
        public SimulationJob Job { get; }
        public SimulationJobState State { get; private set; } = SimulationJobState.Queued;
        public RunResult Result { get; private set; }
        public string Failure { get; private set; }
        public double WallClockMilliseconds => _stopwatch.Elapsed.TotalMilliseconds;
        public bool IsTerminal => State == SimulationJobState.Succeeded || State == SimulationJobState.Failed || State == SimulationJobState.Cancelled || State == SimulationJobState.TimedOut;

        internal void Start() { State = SimulationJobState.Running; _stopwatch.Start(); }
        internal void Succeed(RunResult result) { if (!IsTerminal) { Result = result; State = SimulationJobState.Succeeded; _stopwatch.Stop(); } }
        internal void Fail(string failure) { if (!IsTerminal) { Failure = failure; State = SimulationJobState.Failed; _stopwatch.Stop(); } }
        internal void Cancel() { if (!IsTerminal) { State = SimulationJobState.Cancelled; _stopwatch.Stop(); } }
        internal void Timeout() { if (!IsTerminal) { State = SimulationJobState.TimedOut; _stopwatch.Stop(); } }
    }

    /// <summary>
    /// Queue for independent authored-scene jobs. Unity scene simulation remains single-threaded;
    /// callers can run many jobs at high time scale without pretending GameObjects are thread-safe.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationJobScheduler : MonoBehaviour
    {
        readonly Queue<SimulationJobExecution> _pending = new Queue<SimulationJobExecution>();
        [SerializeField, Min(1)] int requestedWorkerCount = 1;
        public SimulationJobExecution Active { get; private set; }
        public SimulationWorkerPlan WorkerPlan => new SimulationWorkerPlan(requestedWorkerCount);
        public event Action<SimulationJobExecution> JobStarted;
        public event Action<SimulationJobExecution> JobFinished;

        public SimulationJobExecution Enqueue(SimulationJob job)
        {
            var execution = new SimulationJobExecution(job);
            _pending.Enqueue(execution);
            return execution;
        }

        public void SetRequestedWorkerCount(int count) => requestedWorkerCount = Mathf.Max(1, count);

        public bool Cancel(string id)
        {
            if (Active != null && Active.Job.Id == id) { Active.Cancel(); FinishActive(); return true; }
            foreach (var queued in _pending) if (queued.Job.Id == id) { queued.Cancel(); return true; }
            return false;
        }

        public void CompleteActive(RunResult result) { if (Active == null) return; Active.Succeed(result); FinishActive(); }
        public void FailActive(string reason) { if (Active == null) return; Active.Fail(reason); FinishActive(); }

        void Update()
        {
            TickScheduler();
        }

        /// <summary>Advances the queue on Unity's main thread. Exposed for deterministic tooling tests.</summary>
        public void TickScheduler()
        {
            if (Active == null) StartNext();
            if (Active != null && Active.Job.TimeoutSeconds > 0f && Active.WallClockMilliseconds >= Active.Job.TimeoutSeconds * 1000d)
            {
                Active.Timeout();
                FinishActive();
            }
        }

        void StartNext()
        {
            while (_pending.Count > 0)
            {
                var next = _pending.Dequeue();
                if (next.IsTerminal) continue;
                Active = next;
                Active.Start();
                JobStarted?.Invoke(Active);
                break;
            }
        }

        void FinishActive()
        {
            var completed = Active;
            Active = null;
            if (completed != null) JobFinished?.Invoke(completed);
        }
    }

    /// <summary>Machine-readable interchange report. JsonUtility is used to avoid runtime dependencies.</summary>
    public static class SimulationJsonReport
    {
        [Serializable] class JobDto
        {
            public string id; public int seed; public string state; public double wallClockMilliseconds; public string failure;
            public float durationSeconds; public int finalWave; public bool victory;
            public string[] upgradeOffers; public string[] upgradeSelections;
        }

        public static string Serialize(SimulationJobExecution execution)
        {
            if (execution == null) throw new ArgumentNullException(nameof(execution));
            var result = execution.Result;
            return JsonUtility.ToJson(new JobDto
            {
                id = execution.Job.Id, seed = execution.Job.Seed, state = execution.State.ToString(),
                wallClockMilliseconds = execution.WallClockMilliseconds, failure = execution.Failure,
                durationSeconds = result != null ? result.DurationSeconds : 0f,
                finalWave = result != null ? result.FinalWaveReached : 0,
                victory = result != null && result.IsVictory,
                upgradeOffers = result != null ? ToArray(result.UpgradeOffers) : Array.Empty<string>(),
                upgradeSelections = result != null ? ToNames(result.UpgradeSelections) : Array.Empty<string>()
            });
        }

        static string[] ToArray(IReadOnlyList<string> values)
        {
            if (values == null) return Array.Empty<string>();
            var copy = new string[values.Count];
            for (var index = 0; index < copy.Length; index++) copy[index] = values[index];
            return copy;
        }

        static string[] ToNames(IReadOnlyList<UpgradeId> values)
        {
            if (values == null) return Array.Empty<string>();
            var copy = new string[values.Count];
            for (var index = 0; index < copy.Length; index++) copy[index] = values[index].ToString();
            return copy;
        }
    }
}
