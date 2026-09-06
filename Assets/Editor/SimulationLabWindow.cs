using UnityEditor;
using UnityEngine;

namespace RealRail.Editor
{
    /// <summary>Development-only control surface for the real gameplay batch runner.</summary>
    public sealed class SimulationLabWindow : EditorWindow
    {
        const string MenuPath = "RealRail/Simulation Lab";

        BotProfileId _profile = BotProfileId.Average;
        int _requestedRuns = 5;
        float _simulationSpeed = 4f;
        int _seed = 12345;
        int _experimentCandidates = 2;
        int _experimentIterations = 1;
        int _experimentSeedCount = 2;
        float _averageMinWinRate = .15f, _averageMaxWinRate = .45f;
        float _strongMinWinRate = .35f, _strongMaxWinRate = .65f;
        float _perfectMinWinRate = .55f, _perfectMaxWinRate = .85f;
        float _minimumDurationSeconds = 20f, _maximumDurationSeconds = 45f;
        string _status = "Idle";
        SimulationRunner _runner;
        SimulationExperimentRunner _experimentRunner;
        RunStatistics _latestStatistics;
        BalanceSearchResult _latestExperiment;

        [MenuItem(MenuPath)]
        static void Open()
        {
            GetWindow<SimulationLabWindow>("Simulation Lab");
        }

        void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            DetachRunner();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Batch Configuration", EditorStyles.boldLabel);
            _profile = (BotProfileId)EditorGUILayout.EnumPopup("Bot Profile", _profile);
            _requestedRuns = EditorGUILayout.IntField("Number of Runs", _requestedRuns);
            _simulationSpeed = EditorGUILayout.FloatField("Simulation Speed", _simulationSpeed);
            _seed = EditorGUILayout.IntField("Base Seed", _seed);
            EditorGUILayout.HelpBox("Simulation Lab runs the authored SampleScene through SimulationRunner. It does not create an editor-only combat simulation.", MessageType.Info);

            var canRun = _requestedRuns > 0 && _simulationSpeed > 0f &&
                (!EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying);
            using (new EditorGUI.DisabledScope(!canRun || (_runner != null && _runner.IsRunningBatch)))
            {
                if (GUILayout.Button("Run Batch"))
                {
                    _latestStatistics = null;
                    if (EditorApplication.isPlaying)
                    {
                        StartRequestedBatch();
                    }
                    else
                    {
                        _status = "Starting Play Mode";
                        EditorApplication.isPlaying = true;
                    }
                }
            }

            if (EditorApplication.isPlaying && _runner != null && _runner.IsRunningBatch && GUILayout.Button("Stop Batch"))
            {
                _runner.StopSimulation();
                _status = "Stopped";
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", _status);
            var completed = _runner != null ? _runner.CompletedRunCount : 0;
            EditorGUILayout.LabelField("Completed Runs", completed + " / " + _requestedRuns);
            EditorGUILayout.LabelField("Selected Profile", BotProfile.FromId(_profile).Label);

            if (_latestStatistics != null)
            {
                DrawStatistics(_latestStatistics);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bounded Balance Experiment", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Development experiment targets only — not approved production balance.", MessageType.Info);
            DrawRange("Average win rate", ref _averageMinWinRate, ref _averageMaxWinRate, 0f, 1f);
            DrawRange("Strong win rate", ref _strongMinWinRate, ref _strongMaxWinRate, 0f, 1f);
            DrawRange("Perfect-ish win rate", ref _perfectMinWinRate, ref _perfectMaxWinRate, 0f, 1f);
            DrawRange("Run duration (seconds)", ref _minimumDurationSeconds, ref _maximumDurationSeconds, 0f, 3600f);
            _experimentCandidates = EditorGUILayout.IntField("Candidates per Iteration", _experimentCandidates);
            _experimentIterations = EditorGUILayout.IntField("Iterations", _experimentIterations);
            _experimentSeedCount = EditorGUILayout.IntField("Seeds per Profile", _experimentSeedCount);
            EditorGUILayout.HelpBox("Runs copied candidate configurations through the real scene for Average, Strong, and Perfect-ish. Candidate results never modify the authored baseline. Worker counts above one require separate Unity processes; this Lab session stays safely single-process.", MessageType.Info);
            var canExperiment = _experimentCandidates > 0 && _experimentIterations > 0 && _experimentSeedCount > 0 && _simulationSpeed > 0f &&
                (!EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying) && (_runner == null || !_runner.IsRunningBatch) && (_experimentRunner == null || !_experimentRunner.IsRunning);
            using (new EditorGUI.DisabledScope(!canExperiment))
            {
                if (GUILayout.Button("Run Experiment"))
                {
                    if (EditorApplication.isPlaying) StartExperiment();
                    else { _status = "Starting Play Mode for experiment"; EditorApplication.isPlaying = true; }
                }
            }
            if (_experimentRunner != null && _experimentRunner.IsRunning && GUILayout.Button("Stop Experiment"))
            {
                _experimentRunner.StopExperiment();
                _status = "Experiment stopped";
            }
            if (_latestExperiment != null) DrawExperiment(_latestExperiment);
            if (_experimentRunner != null && _latestExperiment != null) DrawThroughput(_experimentRunner);

            if (EditorApplication.isPlaying) Repaint();
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (_status == "Starting Play Mode for experiment") StartExperiment();
                else StartRequestedBatch();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                DetachRunner();
                if (_status == "Running") _status = "Stopped";
            }
            Repaint();
        }

        void StartRequestedBatch()
        {
            DetachRunner();
            _runner = Object.FindAnyObjectByType<SimulationRunner>();
            var bot = Object.FindAnyObjectByType<PlayerBot>();
            if (_runner == null || bot == null)
            {
                _status = "Failed: SampleScene is missing SimulationRunner or PlayerBot";
                return;
            }

            bot.SetProfile(_profile);
            _runner.SetSimulationSeed(_seed);
            _runner.BatchCompleted += OnBatchCompleted;
            _runner.StartSimulation(_requestedRuns, _simulationSpeed);
            _status = _runner.IsRunningBatch ? "Running" : "Failed: runner dependencies are not configured";
        }

        void StartExperiment()
        {
            DetachRunner();
            _runner = Object.FindAnyObjectByType<SimulationRunner>();
            var bot = Object.FindAnyObjectByType<PlayerBot>();
            if (_runner == null || bot == null)
            {
                _status = "Failed: SampleScene is missing SimulationRunner or PlayerBot";
                return;
            }
            _experimentRunner = _runner.GetComponent<SimulationExperimentRunner>() ?? _runner.gameObject.AddComponent<SimulationExperimentRunner>();
            _experimentRunner.ConfigureForTests(_runner, bot);
            _experimentRunner.ExperimentCompleted -= OnExperimentCompleted;
            _experimentRunner.ExperimentCompleted += OnExperimentCompleted;
            var seeds = new int[_experimentSeedCount];
            for (var index = 0; index < seeds.Length; index++) seeds[index] = RunRandomContext.SeedForRun(_seed, index);
            var objective = new BalanceObjective(new[]
            {
                new BalanceProfileObjective(BotProfileId.Average, _averageMinWinRate, _averageMaxWinRate, _minimumDurationSeconds, _maximumDurationSeconds),
                new BalanceProfileObjective(BotProfileId.Strong, _strongMinWinRate, _strongMaxWinRate, _minimumDurationSeconds, _maximumDurationSeconds),
                new BalanceProfileObjective(BotProfileId.PerfectIsh, _perfectMinWinRate, _perfectMaxWinRate, _minimumDurationSeconds, _maximumDurationSeconds)
            });
            var director = Object.FindAnyObjectByType<WaveDirector>();
            if (director == null)
            {
                _status = "Failed: SimulationRunner is missing WaveDirector";
                return;
            }
            var definition = new BalanceExperimentDefinition(director.CreateBaselineConfiguration(), objective,
                new BalanceCandidateConstraints(), seeds, _experimentCandidates, _experimentIterations, _seed);
            _latestExperiment = null;
            _status = _experimentRunner.StartExperiment(definition, _simulationSpeed) ? "Experiment running" : "Failed to start experiment";
        }

        void OnExperimentCompleted(BalanceSearchResult result)
        {
            _latestExperiment = result;
            _status = "Experiment completed";
            Repaint();
        }

        void OnBatchCompleted(RunStatistics statistics)
        {
            _latestStatistics = statistics;
            _status = "Completed";
            Repaint();
        }

        void DetachRunner()
        {
            if (_runner != null) _runner.BatchCompleted -= OnBatchCompleted;
            if (_experimentRunner != null) _experimentRunner.ExperimentCompleted -= OnExperimentCompleted;
            _runner = null;
            _experimentRunner = null;
        }

        static void DrawStatistics(RunStatistics statistics)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Batch Results", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Victories / Defeats", statistics.Victories + " / " + statistics.Defeats);
            EditorGUILayout.LabelField("Win Rate", statistics.WinRate.ToString("P1"));
            EditorGUILayout.LabelField("Duration (avg / median)", statistics.AverageDurationSeconds.ToString("0.##") + "s / " + statistics.MedianDurationSeconds.ToString("0.##") + "s");
            EditorGUILayout.LabelField("Wave (avg / median)", statistics.AverageFinalWave.ToString("0.##") + " / " + statistics.MedianFinalWave.ToString("0.##"));
            EditorGUILayout.LabelField("Average Kills / Leaks", statistics.AverageEnemiesKilled.ToString("0.##") + " / " + statistics.AverageEnemiesLeaked.ToString("0.##"));
            EditorGUILayout.LabelField("Average Player Damage", statistics.AveragePlayerDamageTaken.ToString("0.##"));
            EditorGUILayout.LabelField("Most Common Defeat Wave", statistics.MostCommonDefeatWave == 0 ? "n/a" : statistics.MostCommonDefeatWave.ToString());
        }

        static void DrawExperiment(BalanceSearchResult result)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Experiment Ranking", EditorStyles.boldLabel);
            DrawCandidate("Baseline", result.Baseline);
            foreach (var candidate in result.RankedCandidates) DrawCandidate(candidate.Candidate.Id, candidate);
        }

        static void DrawCandidate(string label, BalanceCandidateEvaluation candidate)
        {
            EditorGUILayout.LabelField(label + " — score", candidate.Score.ToString("0.###"));
            foreach (var profile in candidate.Profiles)
                EditorGUILayout.LabelField("  " + BotProfile.FromId(profile.Profile).Label, profile.Statistics.WinRate.ToString("P1") + ", " + profile.Statistics.AverageDurationSeconds.ToString("0.##") + "s, " + profile.Statistics.RunCount + " runs");
        }

        static void DrawRange(string label, ref float minimum, ref float maximum, float lower, float upper)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            minimum = Mathf.Clamp(EditorGUILayout.FloatField(minimum), lower, upper);
            maximum = Mathf.Clamp(EditorGUILayout.FloatField(maximum), lower, upper);
            if (maximum < minimum) maximum = minimum;
            EditorGUILayout.EndHorizontal();
        }

        static void DrawThroughput(SimulationExperimentRunner runner)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Measured Execution", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Completed jobs", runner.CompletedJobCount.ToString());
            EditorGUILayout.LabelField("Wall-clock duration", (runner.WallClockMilliseconds / 1000d).ToString("0.###") + "s");
            EditorGUILayout.LabelField("Throughput", runner.RunsPerMinute.ToString("0.##") + " runs/min");
            EditorGUILayout.LabelField("Simulated game time", runner.TotalSimulatedSeconds.ToString("0.##") + "s");
            EditorGUILayout.LabelField("Workers / mode", runner.WorkerCount + " / " + runner.ExecutionMode);
            EditorGUILayout.LabelField("Saved JSON", string.IsNullOrEmpty(runner.LatestResultPath) ? (runner.Failure ?? "not saved") : runner.LatestResultPath);
        }
    }
}
