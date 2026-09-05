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
        string _status = "Idle";
        SimulationRunner _runner;
        RunStatistics _latestStatistics;

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

            if (EditorApplication.isPlaying) Repaint();
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                StartRequestedBatch();
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
            _runner.BatchCompleted += OnBatchCompleted;
            _runner.StartSimulation(_requestedRuns, _simulationSpeed);
            _status = _runner.IsRunningBatch ? "Running" : "Failed: runner dependencies are not configured";
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
            _runner = null;
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
    }
}
