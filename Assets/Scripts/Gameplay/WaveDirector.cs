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
        public int[] UpgradeTriggerKillCounts;

        public WaveConfig(
            int killGoal,
            float spawnInterval,
            float moveSpeed,
            int[] upgradeTriggerKillCounts = null,
            float heavySpawnChance = 0f)
        {
            KillGoal = killGoal;
            SpawnInterval = spawnInterval;
            MoveSpeed = moveSpeed;
            UpgradeTriggerKillCounts = upgradeTriggerKillCounts ?? Array.Empty<int>();
            HeavySpawnChance = Mathf.Clamp01(heavySpawnChance);
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
        int _waveIndex = -1;

        public WavePhase Phase { get; private set; }
        public int CurrentWaveNumber => _waveIndex + 1;
        public WaveProgress Progress => _progress;

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
            if (session == null || !session.IsPlaying || spawner == null || waves == null || waves.Length != 3)
            {
                return;
            }

            StartNextWave();
        }

        void StartNextWave()
        {
            _waveIndex++;
            _progress = new WaveProgress(waves[_waveIndex].KillGoal);
            Phase = WavePhase.Spawning;
            spawner.BeginWave(waves[_waveIndex]);
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

            _progress.RegisterResolved(resolution);
            foreach (var trigger in waves[_waveIndex].UpgradeTriggerKillCounts ?? Array.Empty<int>())
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
            if (_waveIndex == waves.Length - 1)
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

            var laneIndex = UnityEngine.Random.Range(0, lanes.LaneCount);
            var instance = Instantiate(upgradeTargetPrefab, lanes.GetSpawnPosition(laneIndex), Quaternion.identity);
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
