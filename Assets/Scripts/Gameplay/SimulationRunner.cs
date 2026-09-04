using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealRail
{
    /// <summary>
    /// Opt-in development owner for running ordinary gameplay repeatedly at an accelerated time
    /// scale. It owns orchestration only; decision-making remains inside the separately configured
    /// player bot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimulationRunner : MonoBehaviour
    {
        [SerializeField] bool simulationEnabled;
        [SerializeField, Min(0f)] float simulationSpeed = 4f;
        [SerializeField, Min(0)] int maximumRuns;
        [SerializeField] GameSession session;
        [SerializeField] WaveDirector waveDirector;
        [SerializeField] RunTelemetry telemetry;
        [SerializeField] UpgradeSystem upgradeSystem;
        [SerializeField] UpgradeRewardSelection rewardSelection;
        [SerializeField] PlayerBot playerBot;

        Coroutine _restartRoutine;
        float _timeScaleBeforeSimulation = 1f;
        bool _ownsTimeScale;
        readonly List<RunResult> _results = new List<RunResult>();

        public bool IsRunningBatch { get; private set; }
        public int CompletedRunCount { get; private set; }
        public float SimulationSpeed => simulationSpeed;
        public IReadOnlyList<RunResult> Results => _results;
        public RunStatistics LatestStatistics { get; private set; }
        public event Action<RunResult> RunCompleted;
        public event Action<RunStatistics> BatchCompleted;

        void Awake()
        {
            FindDependencies();
        }

        void OnEnable()
        {
            Subscribe();
            if (simulationEnabled) StartSimulation();
        }

        void OnDisable()
        {
            Unsubscribe();
            StopSimulation();
        }

        void Update()
        {
            if (IsRunningBatch && (rewardSelection == null || !rewardSelection.IsSelecting))
            {
                Time.timeScale = simulationSpeed;
            }
        }

        /// <summary>Starts an opt-in one-at-a-time batch. Zero maximum runs means no limit.</summary>
        public void StartSimulation()
        {
            if (IsRunningBatch) return;

            FindDependencies();
            if (session == null || waveDirector == null) return;

            simulationEnabled = true;
            IsRunningBatch = true;
            CompletedRunCount = 0;
            _results.Clear();
            LatestStatistics = null;
            _timeScaleBeforeSimulation = Time.timeScale;
            _ownsTimeScale = true;
            playerBot?.SetBotEnabled(true);
            _restartRoutine = StartCoroutine(PrepareFreshRun());
        }

        /// <summary>Stops future restarts and restores normal time and player input ownership.</summary>
        public void StopSimulation()
        {
            simulationEnabled = false;
            IsRunningBatch = false;
            if (_restartRoutine != null)
            {
                StopCoroutine(_restartRoutine);
                _restartRoutine = null;
            }
            playerBot?.SetBotEnabled(false);
            if (_ownsTimeScale)
            {
                Time.timeScale = _timeScaleBeforeSimulation;
                _ownsTimeScale = false;
            }
        }

        public void SetSimulationSpeed(float speed)
        {
            simulationSpeed = Mathf.Max(0f, speed);
            if (IsRunningBatch && (rewardSelection == null || !rewardSelection.IsSelecting))
            {
                Time.timeScale = simulationSpeed;
            }
        }

        public void ConfigureForTests(GameSession gameSession, WaveDirector director, RunTelemetry runTelemetry,
            UpgradeSystem upgrades = null, UpgradeRewardSelection selection = null, PlayerBot bot = null)
        {
            Unsubscribe();
            session = gameSession;
            waveDirector = director;
            telemetry = runTelemetry;
            upgradeSystem = upgrades;
            rewardSelection = selection;
            playerBot = bot;
            Subscribe();
        }

        void Subscribe()
        {
            if (session == null) return;
            session.Lost -= OnRunEnded;
            session.Victory -= OnRunEnded;
            session.Lost += OnRunEnded;
            session.Victory += OnRunEnded;
        }

        void Unsubscribe()
        {
            if (session == null) return;
            session.Lost -= OnRunEnded;
            session.Victory -= OnRunEnded;
        }

        void OnRunEnded()
        {
            if (!IsRunningBatch || _restartRoutine != null) return;

            var result = telemetry != null ? telemetry.CurrentResult : new RunResult(
                session.State, session.ElapsedRunSeconds, 0, 0, 0, 0, Array.Empty<AcquiredUpgrade>());
            _results.Add(result);
            RunCompleted?.Invoke(result);
            CompletedRunCount++;
            if (maximumRuns > 0 && CompletedRunCount >= maximumRuns)
            {
                LatestStatistics = RunStatisticsAggregator.Aggregate(
                    playerBot != null ? playerBot.Profile : BotProfile.FromId(BotProfileId.Average), _results);
                BatchCompleted?.Invoke(LatestStatistics);
                StopSimulation();
                return;
            }
            _restartRoutine = StartCoroutine(PrepareFreshRun());
        }

        void BeginFreshRun()
        {
            rewardSelection?.ResetSelection();
            upgradeSystem?.ResetUpgrades();
            telemetry?.ResetRun();
            session.ResetRun();
            Time.timeScale = simulationSpeed;
            waveDirector.StartRun();
        }

        IEnumerator PrepareFreshRun()
        {
            waveDirector.ResetRun();
            rewardSelection?.ResetSelection();
            RemoveTransientActors();
            yield return null;
            _restartRoutine = null;
            if (IsRunningBatch) BeginFreshRun();
        }

        static void RemoveTransientActors()
        {
            var actors = new HashSet<GameObject>();
            AddActors<WaveEnemy>(actors);
            AddActors<UpgradeTarget>(actors);
            AddActors<Projectile>(actors);
            foreach (var actor in actors)
            {
                if (actor != null) Destroy(actor);
            }
        }

        static void AddActors<T>(ISet<GameObject> actors) where T : Component
        {
            foreach (var actor in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                actors.Add(actor.gameObject);
            }
        }

        void FindDependencies()
        {
            session ??= FindFirstObjectByType<GameSession>();
            waveDirector ??= FindFirstObjectByType<WaveDirector>();
            telemetry ??= FindFirstObjectByType<RunTelemetry>();
            upgradeSystem ??= FindFirstObjectByType<UpgradeSystem>();
            rewardSelection ??= FindFirstObjectByType<UpgradeRewardSelection>();
            playerBot ??= FindFirstObjectByType<PlayerBot>();
        }
    }
}
