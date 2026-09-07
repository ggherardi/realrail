using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace RealRail.Editor
{
    /// <summary>
    /// Process-isolated Unity worker for one authoritative SampleScene simulation. Invoke with:
    /// -batchmode -nographics -executeMethod RealRail.Editor.SimulationBatchWorker.Execute
    /// -simulationRequest /absolute/request.json -simulationResult /absolute/result.json
    /// The caller owns process fan-out; this class deliberately runs one scene on Unity's main thread.
    /// </summary>
    public static class SimulationBatchWorker
    {
        const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";
        const string RequestArgument = "-simulationRequest";
        const string ResultArgument = "-simulationResult";

        static SimulationBatchRequest _request;
        static string _resultPath;
        static SimulationRunner _runner;
        static Stopwatch _stopwatch;
        static string _startedUtc;
        static bool _started;
        static bool _finished;

        /// <summary>Unity -executeMethod entry point. It exits only after a terminal result file is written.</summary>
        public static void Execute()
        {
            try
            {
                var requestPath = RequireArgument(RequestArgument);
                _resultPath = RequireArgument(ResultArgument);
                _request = SimulationBatchRequest.FromJson(File.ReadAllText(requestPath));
                string failure = null;
                if (_request == null || !_request.IsValid(out failure)) throw new InvalidOperationException(failure ?? "Could not deserialize simulation request.");
                if (!Path.IsPathRooted(_resultPath)) throw new InvalidOperationException("The simulation result path must be absolute.");

                _startedUtc = DateTime.UtcNow.ToString("O");
                _stopwatch = Stopwatch.StartNew();
                EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
                // This disposable worker project enters Play Mode to exercise the authored runtime. Retain
                // the immutable request and editor callbacks across that transition; production settings
                // are never touched because workers use isolated project copies.
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                EditorApplication.update += Tick;
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                FailBeforeStart(exception);
            }
        }

        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                // Tick starts only after the usual runtime Awake/Start cycle has had a frame.
                return;
            }

            if (!_finished && (change == PlayModeStateChange.ExitingPlayMode || change == PlayModeStateChange.EnteredEditMode))
                FinishFailure("Unity exited Play Mode before the simulation worker completed.");
        }

        static void Tick()
        {
            if (_finished || _request == null) return;
            try
            {
                if (!_started)
                {
                    if (!EditorApplication.isPlaying) return;
                    _runner = UnityEngine.Object.FindAnyObjectByType<SimulationRunner>();
                    var bot = UnityEngine.Object.FindAnyObjectByType<PlayerBot>();
                    if (_runner == null || bot == null) throw new InvalidOperationException("SampleScene must contain configured SimulationRunner and PlayerBot components.");

                    bot.SetProfile(_request.profile);
                    _runner.SetSimulationSeed(_request.seed);
                    _runner.StartSimulation(1, _request.simulationSpeed, _request.CreateConfiguration());
                    if (!_runner.IsRunningBatch) throw new InvalidOperationException("SimulationRunner did not start; verify its authored scene references.");
                    _started = true;
                    return;
                }

                if (_request.timeoutSeconds > 0f && _stopwatch.Elapsed.TotalSeconds > _request.timeoutSeconds)
                {
                    _runner?.StopSimulation();
                    FinishFailure("Simulation worker timed out after " + _request.timeoutSeconds + " seconds.");
                    return;
                }

                if (_runner.IsRunningBatch) return;
                if (_runner.Results.Count != 1) throw new InvalidOperationException("Simulation worker completed without exactly one RunResult.");
                Finish(SimulationBatchResult.Succeeded(_request.jobId, _request.seed, _runner.Results[0], _stopwatch.Elapsed.TotalMilliseconds), 0);
            }
            catch (Exception exception)
            {
                FinishFailure(exception.Message);
            }
        }

        static void FinishFailure(string failure)
        {
            var elapsed = _stopwatch != null ? _stopwatch.Elapsed.TotalMilliseconds : 0d;
            Finish(SimulationBatchResult.Failed(_request != null ? _request.jobId : null, _request != null ? _request.seed : 0, failure, elapsed), 1);
        }

        static void FailBeforeStart(Exception exception)
        {
            try
            {
                if (!string.IsNullOrEmpty(_resultPath)) FinishFailure(exception.Message);
                else UnityEngine.Debug.LogError("RealRail simulation batch worker failed before a result path was available: " + exception);
            }
            catch (Exception writeException)
            {
                UnityEngine.Debug.LogError("RealRail simulation batch worker could not write its failure result: " + writeException);
                EditorApplication.Exit(1);
            }
        }

        static void Finish(SimulationBatchResult result, int exitCode)
        {
            if (_finished) return;
            _finished = true;
            _stopwatch?.Stop();
            result.experimentId = _request != null ? _request.experimentId : null;
            result.candidateId = _request != null ? _request.candidateId : null;
            result.profile = _request != null ? _request.profile : BotProfileId.Average;
            result.workerProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            result.startedUtc = _startedUtc;
            result.completedUtc = DateTime.UtcNow.ToString("O");
            try
            {
                var directory = Path.GetDirectoryName(_resultPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(_resultPath, result.ToJson(true));
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError("RealRail simulation batch worker could not write result: " + exception);
                exitCode = 1;
            }
            finally
            {
                EditorApplication.update -= Tick;
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.Exit(exitCode);
            }
        }

        static string RequireArgument(string name)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
                if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase)) return arguments[index + 1];
            throw new ArgumentException("Missing required command-line argument " + name + ".");
        }
    }
}
