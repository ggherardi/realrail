using System;
using UnityEngine;

namespace RealRail
{
    [Serializable]
    public struct WaveConfig
    {
        [Min(1)] public int KillGoal;
        [Min(0.01f)] public float SpawnInterval;
        [Min(0f)] public float MoveSpeed;
        [Range(0f, 1f)] public float HeavySpawnChance;
        [Min(0)] public int MaxConcurrentEnemies;
        public int[] UpgradeTriggerKillCounts;

        public WaveConfig(
            int killGoal,
            float spawnInterval,
            float moveSpeed,
            int[] upgradeTriggerKillCounts = null,
            float heavySpawnChance = 0f,
            int maxConcurrentEnemies = 0)
        {
            KillGoal = killGoal;
            SpawnInterval = spawnInterval;
            MoveSpeed = moveSpeed;
            UpgradeTriggerKillCounts = upgradeTriggerKillCounts ?? Array.Empty<int>();
            HeavySpawnChance = Mathf.Clamp01(heavySpawnChance);
            MaxConcurrentEnemies = Mathf.Max(0, maxConcurrentEnemies);
        }

        public bool ShouldSpawnHeavy(float roll)
        {
            return roll >= 0f && roll < Mathf.Clamp01(HeavySpawnChance);
        }
    }

    public enum WavePhase
    {
        None,
        Spawning,
        Complete
    }

    public sealed class WaveDirector : MonoBehaviour
    {
        [SerializeField] RunDefinition authoredRun;
        // Retained as the scene's default plan and for backward compatibility with existing scenes.
        [SerializeField] WaveConfig[] waves =
        {
            new WaveConfig(20, 0.35f, 3.6f, new[] { 8 }),
            new WaveConfig(40, 0.22f, 4f, new[] { 14, 28 }, 0.10f),
            new WaveConfig(70, 0.14f, 4.4f, new[] { 21, 46 }, 0.15f)
        };
        [SerializeField] GameObject upgradeTargetPrefab;
        [SerializeField] float upgradeTargetSpeed = 4f;
        [SerializeField] GameSession session;
        [SerializeField] EnemySpawner spawner;
        [SerializeField] LaneLayout lanes;
        [SerializeField] UpgradeSystem upgradeSystem;
        [SerializeField] UpgradeRewardSelection upgradeRewardSelection;

        WaveProgress _progress;
        RunConfiguration _run;
        int _waveIndex = -1;
        IRunRandom _upgradeTargetRandom = UnityRunRandom.Shared;

        public WavePhase Phase { get; private set; }
        public int CurrentWaveNumber => _waveIndex + 1;
        public WaveProgress Progress => _progress;
        public int WaveCount => _run != null ? _run.WaveCount : 0;

        /// <summary>Returns a defensive copy of the authored baseline for experimental comparison.</summary>
        public RunConfiguration CreateBaselineConfiguration()
        {
            return authoredRun != null ? authoredRun.CreateRuntimeConfiguration() : new RunConfiguration(waves);
        }

        /// <summary>Sets the isolated stream used only for target lane and position selection.</summary>
        public void SetUpgradeTargetRandom(IRunRandom random)
        {
            _upgradeTargetRandom = random ?? UnityRunRandom.Shared;
        }

        /// <summary>Routes independent deterministic streams to the systems owned by this director.</summary>
        public void SetRunRandomContext(RunRandomContext context)
        {
            if (context == null)
            {
                spawner?.SetRunRandom(null);
                SetUpgradeTargetRandom(null);
                return;
            }
            spawner?.SetRunRandom(context.CreateStream("enemy-spawn"));
            SetUpgradeTargetRandom(context.CreateStream("upgrade-target"));
        }

        void Awake()
        {
            if (spawner != null)
            {
                spawner.EnemySpawned += OnEnemySpawned;
            }
        }

        void Start()
        {
            StartRun();
        }

        void OnDestroy()
        {
            if (spawner != null)
            {
                spawner.EnemySpawned -= OnEnemySpawned;
            }
        }

        public void StartRun()
        {
            StartRun(CreateBaselineConfiguration());
        }

        /// <summary>
        /// Starts an arbitrary runtime plan. This is the execution seam for future directed encounters.
        /// </summary>
        public void StartRun(RunConfiguration run)
        {
            if (session == null || !session.IsPlaying || spawner == null || run == null || !run.IsValid)
            {
                return;
            }

            _run = run;
            _waveIndex = -1;
            StartNextWave();
        }

        /// <summary>
        /// Stops the current plan before a run owner removes transient actors. This intentionally
        /// does not reset the session: the owner can still capture its completed result first.
        /// </summary>
        public void ResetRun()
        {
            Phase = WavePhase.None;
            _progress = null;
            _run = null;
            _waveIndex = -1;
            spawner?.ResetSpawnerState();
        }

        void StartNextWave()
        {
            _waveIndex++;
            var wave = _run.GetWave(_waveIndex);
            _progress = new WaveProgress(wave.KillGoal);
            Phase = WavePhase.Spawning;
            spawner.BeginWave(wave);
            session.ReportWaveStarted(CurrentWaveNumber);
        }

        void OnEnemySpawned(WaveEnemy enemy)
        {
            if (!IsActiveWave() || enemy == null)
            {
                return;
            }

            _progress.RegisterSpawned();
            enemy.Resolved += OnEnemyResolved;
        }

        void OnEnemyResolved(WaveEnemy enemy, WaveEnemyResolution resolution)
        {
            enemy.Resolved -= OnEnemyResolved;
            if (!IsActiveWave())
            {
                return;
            }

            session.ReportEnemyResolved(resolution);
            _progress.RegisterResolved(resolution);
            foreach (var trigger in _run.GetWave(_waveIndex).UpgradeTriggerKillCounts ?? Array.Empty<int>())
            {
                if (_progress.TryConsumeUpgradeTrigger(trigger))
                {
                    SpawnUpgradeTarget();
                }
            }

            if (_progress.KillGoalReached)
            {
                spawner.StopSpawning();
            }

            if (_progress.IsComplete)
            {
                AdvanceAfterClear();
            }
        }

        void AdvanceAfterClear()
        {
            if (_waveIndex == _run.WaveCount - 1)
            {
                Phase = WavePhase.Complete;
                session.Win();
                return;
            }

            StartNextWave();
        }

        void SpawnUpgradeTarget()
        {
            if (upgradeTargetPrefab == null || lanes == null)
            {
                return;
            }

            var laneIndex = _upgradeTargetRandom.Next(lanes.LaneCount);
            var instance = Instantiate(upgradeTargetPrefab, lanes.GetSpawnPosition(laneIndex, _upgradeTargetRandom), Quaternion.identity);
            instance.SetActive(true);
            var target = instance.GetComponent<UpgradeTarget>();
            if (target == null)
            {
                Destroy(instance);
                return;
            }

            target.Initialize(session, lanes.GetLaneX(laneIndex), lanes.PlayerZ, lanes.ActorY, upgradeTargetSpeed);
            target.Resolved += OnUpgradeTargetResolved;
        }

        void OnUpgradeTargetResolved(UpgradeTarget target, bool collected)
        {
            target.Resolved -= OnUpgradeTargetResolved;

            if (session == null || !session.IsPlaying || !collected)
            {
                return;
            }

            upgradeRewardSelection?.RequestReward();
        }

        bool IsActiveWave()
        {
            return session != null && session.IsPlaying && Phase == WavePhase.Spawning && _progress != null;
        }
    }
}
