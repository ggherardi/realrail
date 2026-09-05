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
        [SerializeField] int simulationSeed = 12345;

        Coroutine _restartRoutine;
        float _timeScaleBeforeSimulation = 1f;
        bool _ownsTimeScale;
        readonly List<RunResult> _results = new List<RunResult>();
        RunConfiguration _requestedRun;

        public bool IsRunningBatch { get; private set; }
        public int CompletedRunCount { get; private set; }
        public float SimulationSpeed => simulationSpeed;
        public int SimulationSeed => simulationSeed;
        public int CurrentRunSeed => RunRandomContext.SeedForRun(simulationSeed, CompletedRunCount);
        public IReadOnlyList<RunResult> Results => _results;
        public RunStatistics LatestStatistics { get; private set; }
        public event Action<RunResult> RunCompleted;
        public event Action<RunStatistics> BatchCompleted;

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

        /// <summary>Sets the finite batch size used by development tooling. Values below one mean unlimited.</summary>
        public void SetMaximumRuns(int runCount)
        {
            maximumRuns = Mathf.Max(0, runCount);
        }

        /// <summary>Sets the deterministic seed for the next batch; each run derives a stable unique seed.</summary>
        public void SetSimulationSeed(int seed)
        {
            simulationSeed = seed;
        }

        /// <summary>Configures and starts a finite development batch through the ordinary gameplay lifecycle.</summary>
        public void StartSimulation(int runCount, float speed)
        {
            _requestedRun = null;
            SetMaximumRuns(runCount);
            SetSimulationSpeed(speed);
            StartSimulation();
        }

        /// <summary>Starts a batch using an isolated configuration copy so callers may safely reuse theirs.</summary>
        public void StartSimulation(int runCount, float speed, RunConfiguration run)
        {
            _requestedRun = run != null ? new RunConfiguration(run.Waves) : null;
            StartSimulation(runCount, speed);
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
                session.State, session.ElapsedRunSeconds, 0, 0, 0, 0, Array.Empty<AcquiredUpgrade>(),
                RunRandomContext.SeedForRun(simulationSeed, CompletedRunCount));
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
            var random = new RunRandomContext(RunRandomContext.SeedForRun(simulationSeed, CompletedRunCount));
            ConfigureRunRandom(random);
            rewardSelection?.ResetSelection();
            upgradeSystem?.ResetUpgrades();
            telemetry?.ResetRun();
            telemetry?.SetRunSeed(random.Seed);
            session.ResetRun();
            Time.timeScale = simulationSpeed;
            if (_requestedRun != null) waveDirector.StartRun(new RunConfiguration(_requestedRun.Waves));
            else waveDirector.StartRun();
        }

        void ConfigureRunRandom(RunRandomContext random)
        {
            // The director owns the spawner reference, so lookup is intentionally avoided here.
            // It exposes a routing method to keep the runner's scene contract small.
            waveDirector?.SetRunRandomContext(random);
            upgradeSystem?.SetRewardRandom(random.CreateStream("upgrade-rewards"));
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
            foreach (var actor in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include))
            {
                actors.Add(actor.gameObject);
            }
        }

    }
}
