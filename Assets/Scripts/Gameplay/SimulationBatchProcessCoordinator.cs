using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace RealRail
{
    public sealed class UnityBatchWorkerCommand
    {
        public string FileName { get; set; }
        public string ProjectPath { get; set; }
        public string RequestPath { get; set; }
        public string ExecuteMethod { get; set; }
        public string LogPath { get; set; }
        public string ResultPath { get; set; }
        // The worker calls EditorApplication.Exit after atomically producing its terminal result; -quit would end it before the editor update loop can run.
        public string Arguments => "-batchmode -nographics -projectPath " + Quote(ProjectPath) + " -executeMethod " + ExecuteMethod + " -simulationRequest " + Quote(RequestPath) + " -simulationResult " + Quote(ResultPath) + " -logFile " + Quote(LogPath);
        static string Quote(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
    }

    public interface IUnityBatchProcess
    {
        int Id { get; }
        bool HasExited { get; }
        int ExitCode { get; }
        void Kill();
    }

    public interface IUnityBatchProcessFactory { IUnityBatchProcess Start(UnityBatchWorkerCommand command); }

    public sealed class SystemUnityBatchProcessFactory : IUnityBatchProcessFactory
    {
        public IUnityBatchProcess Start(UnityBatchWorkerCommand command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.FileName)) throw new ArgumentException("Unity executable is required.", nameof(command));
            var process = Process.Start(new ProcessStartInfo(command.FileName, command.Arguments) { UseShellExecute = false, CreateNoWindow = true });
            if (process == null) throw new InvalidOperationException("Unity batch process did not start.");
            return new SystemUnityBatchProcess(process);
        }

        sealed class SystemUnityBatchProcess : IUnityBatchProcess
        {
            readonly Process _process;
            public SystemUnityBatchProcess(Process process) => _process = process;
            public int Id => _process.Id;
            public bool HasExited => _process.HasExited;
            public int ExitCode => _process.ExitCode;
            public void Kill() { if (!_process.HasExited) _process.Kill(); }
        }
    }

    /// <summary>Creates independently importable project copies. It intentionally copies only authored project inputs, never Library or generated output.</summary>
    public sealed class SimulationWorkerProjectPreparer
    {
        public string Prepare(string sourceProjectPath, string workersRoot, int slot)
        {
            if (string.IsNullOrWhiteSpace(sourceProjectPath) || string.IsNullOrWhiteSpace(workersRoot)) throw new ArgumentException("Source and worker roots are required.");
            var source = Path.GetFullPath(sourceProjectPath);
            var root = Path.GetFullPath(workersRoot);
            if (root.StartsWith(source + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new InvalidOperationException("Worker root must be outside the source project.");
            var destination = Path.Combine(root, "worker-" + Math.Max(0, slot));
            CopyRequiredDirectory(source, destination, "Assets");
            CopyRequiredDirectory(source, destination, "Packages");
            CopyRequiredDirectory(source, destination, "ProjectSettings");
            return destination;
        }

        static void CopyRequiredDirectory(string sourceRoot, string destinationRoot, string name)
        {
            var source = Path.Combine(sourceRoot, name);
            if (!Directory.Exists(source)) throw new DirectoryNotFoundException("Missing project directory: " + source);
            CopyDirectory(source, Path.Combine(destinationRoot, name));
        }

        static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            foreach (var directory in Directory.GetDirectories(source)) CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }

    public enum SimulationBatchProcessState { Queued, Running, Succeeded, Failed, TimedOut, Cancelled }

    public sealed class SimulationBatchProcessExecution
    {
        internal SimulationBatchProcessExecution(SimulationBatchRequest request, string resultPath) { Request = request ?? throw new ArgumentNullException(nameof(request)); ResultPath = resultPath ?? throw new ArgumentNullException(nameof(resultPath)); State = SimulationBatchProcessState.Queued; }
        public SimulationBatchRequest Request { get; }
        public string ResultPath { get; }
        public SimulationBatchProcessState State { get; internal set; }
        public SimulationBatchResult Result { get; internal set; }
        public string Failure { get; internal set; }
        public int ProcessId { get; internal set; }
        public DateTime StartedUtc { get; internal set; }
        public DateTime EndedUtc { get; internal set; }
        internal IUnityBatchProcess Process { get; set; }
    }

    /// <summary>Bounded, main-thread-polled process pool. It never runs Unity gameplay on a .NET worker thread.</summary>
    public sealed class SimulationBatchProcessCoordinator
    {
        readonly Queue<SimulationBatchProcessExecution> _pending = new Queue<SimulationBatchProcessExecution>();
        readonly List<SimulationBatchProcessExecution> _all = new List<SimulationBatchProcessExecution>();
        readonly IUnityBatchProcessFactory _factory;
        readonly Func<DateTime> _utcNow;
        readonly string _requestsRoot;
        readonly Func<int, string> _projectForSlot;
        readonly Func<SimulationBatchProcessExecution, UnityBatchWorkerCommand> _commandFor;
        bool _cancelled;

        public SimulationBatchProcessCoordinator(int workerCount, string requestsRoot, Func<int, string> projectForSlot, Func<SimulationBatchProcessExecution, UnityBatchWorkerCommand> commandFor, IUnityBatchProcessFactory factory = null, Func<DateTime> utcNow = null)
        {
            WorkerCount = Math.Max(1, Math.Min(8, workerCount));
            _requestsRoot = Path.GetFullPath(requestsRoot ?? throw new ArgumentNullException(nameof(requestsRoot)));
            _projectForSlot = projectForSlot ?? throw new ArgumentNullException(nameof(projectForSlot));
            _commandFor = commandFor ?? throw new ArgumentNullException(nameof(commandFor));
            _factory = factory ?? new SystemUnityBatchProcessFactory(); _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public int WorkerCount { get; }
        public IReadOnlyList<SimulationBatchProcessExecution> Executions => _all;
        public bool IsComplete => _pending.Count == 0 && ActiveCount == 0;
        public int ActiveCount { get { var count = 0; foreach (var execution in _all) if (execution.State == SimulationBatchProcessState.Running) count++; return count; } }
        public bool ObservedOverlap { get { for (var a = 0; a < _all.Count; a++) for (var b = a + 1; b < _all.Count; b++) if (_all[a].StartedUtc < _all[b].EndedUtc && _all[b].StartedUtc < _all[a].EndedUtc) return true; return false; } }

        public SimulationBatchProcessExecution Enqueue(SimulationBatchRequest request, string resultPath) { var execution = new SimulationBatchProcessExecution(request, resultPath); _pending.Enqueue(execution); _all.Add(execution); return execution; }

        public void Tick()
        {
            foreach (var execution in _all) if (execution.State == SimulationBatchProcessState.Running) Inspect(execution);
            while (!_cancelled && ActiveCount < WorkerCount && _pending.Count > 0) Launch(_pending.Dequeue(), ActiveCount);
        }

        public void Cancel()
        {
            _cancelled = true;
            while (_pending.Count > 0) { var queued = _pending.Dequeue(); queued.State = SimulationBatchProcessState.Cancelled; queued.EndedUtc = _utcNow(); }
            foreach (var execution in _all) if (execution.State == SimulationBatchProcessState.Running) { execution.Process.Kill(); execution.State = SimulationBatchProcessState.Cancelled; execution.EndedUtc = _utcNow(); execution.Failure = "Cancelled by coordinator."; }
        }

        void Launch(SimulationBatchProcessExecution execution, int slot)
        {
            try
            {
                Directory.CreateDirectory(_requestsRoot);
                var requestPath = Path.Combine(_requestsRoot, execution.Request.jobId + ".request.json");
                File.WriteAllText(requestPath, execution.Request.ToJson(true));
                var command = _commandFor(execution); command.ProjectPath = _projectForSlot(slot); command.RequestPath = requestPath; command.ResultPath = execution.ResultPath;
                execution.Process = _factory.Start(command); execution.ProcessId = execution.Process.Id; execution.StartedUtc = _utcNow(); execution.State = SimulationBatchProcessState.Running;
            }
            catch (Exception exception) { Fail(execution, "Could not launch worker: " + exception.Message); }
        }

        void Inspect(SimulationBatchProcessExecution execution)
        {
            var timeout = execution.Request.timeoutSeconds;
            if (timeout > 0f && (_utcNow() - execution.StartedUtc).TotalSeconds > timeout) { execution.Process.Kill(); execution.State = SimulationBatchProcessState.TimedOut; execution.Failure = "Worker exceeded timeout of " + timeout + " seconds."; execution.EndedUtc = _utcNow(); return; }
            if (!execution.Process.HasExited) return;
            if (execution.Process.ExitCode != 0) { Fail(execution, "Worker exited with code " + execution.Process.ExitCode + "."); return; }
            try
            {
                if (string.IsNullOrWhiteSpace(execution.ResultPath) || !File.Exists(execution.ResultPath)) { Fail(execution, "Worker exited successfully but did not write its result file."); return; }
                var result = SimulationBatchResult.FromJson(File.ReadAllText(execution.ResultPath));
                if (result == null || result.state != SimulationJobState.Succeeded.ToString()) { Fail(execution, result != null ? result.failure : "Malformed worker result JSON."); return; }
                if (result.experimentId != execution.Request.experimentId || result.jobId != execution.Request.jobId || result.candidateId != execution.Request.candidateId || result.seed != execution.Request.seed) { Fail(execution, "Worker result does not match its immutable request."); return; }
                execution.Result = result; execution.State = SimulationBatchProcessState.Succeeded; execution.EndedUtc = _utcNow();
            }
            catch (Exception exception) { Fail(execution, "Could not read worker result: " + exception.Message); }
        }

        void Fail(SimulationBatchProcessExecution execution, string failure) { execution.State = SimulationBatchProcessState.Failed; execution.Failure = failure ?? "Unknown coordinator failure."; execution.EndedUtc = _utcNow(); }
    }
}
