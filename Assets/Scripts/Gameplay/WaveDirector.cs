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
        [Min(0)] public int UpgradeTriggerKillCount;

        public WaveConfig(
            int killGoal,
            float spawnInterval,
            float moveSpeed,
            int upgradeTriggerKillCount = 0,
            float heavySpawnChance = 0f)
        {
            KillGoal = killGoal;
            SpawnInterval = spawnInterval;
            MoveSpeed = moveSpeed;
            UpgradeTriggerKillCount = upgradeTriggerKillCount;
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
            new WaveConfig(20, 0.35f, 3.6f, 8),
            new WaveConfig(40, 0.22f, 4f, 16, 0.10f),
            new WaveConfig(70, 0.14f, 4.4f, 0, 0.15f)
        };
        [SerializeField] GameObject upgradeTargetPrefab;
        [SerializeField] float upgradeTargetSpeed = 4f;
        [SerializeField] GameSession session;
        [SerializeField] EnemySpawner spawner;
        [SerializeField] LaneLayout lanes;
        [SerializeField] AutoFire autoFire;

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
            if (_progress.TryConsumeUpgradeTrigger(waves[_waveIndex].UpgradeTriggerKillCount))
            {
                SpawnUpgradeTarget();
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

            autoFire?.EnableDoubleShot();
        }

        bool IsActiveWave()
        {
            return session != null && session.IsPlaying && Phase == WavePhase.Spawning && _progress != null;
        }
    }
}
