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

        public WaveConfig(int killGoal, float spawnInterval, float moveSpeed)
        {
            KillGoal = killGoal;
            SpawnInterval = spawnInterval;
            MoveSpeed = moveSpeed;
        }
    }

    public enum WavePhase
    {
        None,
        Spawning,
        Upgrade,
        Complete
    }

    public sealed class WaveDirector : MonoBehaviour
    {
        [SerializeField] WaveConfig[] waves =
        {
            new WaveConfig(5, 1.6f, 4f),
            new WaveConfig(8, 1.2f, 4.5f),
            new WaveConfig(12, 0.9f, 5f)
        };
        [SerializeField] GameObject upgradeTargetPrefab;
        [SerializeField] float upgradeTargetSpeed = 4f;

        GameSession _session;
        EnemySpawner _spawner;
        LaneLayout _lanes;
        AutoFire _autoFire;
        WaveProgress _progress;
        UpgradeTarget _activeUpgradeTarget;
        int _waveIndex = -1;

        public WavePhase Phase { get; private set; }
        public int CurrentWaveNumber => _waveIndex + 1;
        public WaveProgress Progress => _progress;

        public void Bind(GameSession session, EnemySpawner spawner, LaneLayout lanes, AutoFire autoFire, GameObject targetPrefab)
        {
            if (_spawner != null)
            {
                _spawner.EnemySpawned -= OnEnemySpawned;
            }

            _session = session;
            _spawner = spawner;
            _lanes = lanes;
            _autoFire = autoFire;
            upgradeTargetPrefab = targetPrefab;

            if (_spawner != null)
            {
                _spawner.EnemySpawned += OnEnemySpawned;
            }
        }

        void OnDestroy()
        {
            if (_spawner != null)
            {
                _spawner.EnemySpawned -= OnEnemySpawned;
            }
        }

        public void StartRun()
        {
            if (_session == null || !_session.IsPlaying || waves == null || waves.Length != 3)
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
            _spawner.BeginWave(waves[_waveIndex]);
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
            if (_progress.KillGoalReached)
            {
                _spawner.StopSpawning();
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
                _session.Win();
                return;
            }

            Phase = WavePhase.Upgrade;
            SpawnUpgradeTarget();
        }

        void SpawnUpgradeTarget()
        {
            if (upgradeTargetPrefab == null || _lanes == null)
            {
                StartNextWave();
                return;
            }

            var laneIndex = UnityEngine.Random.Range(0, _lanes.LaneCount);
            var instance = Instantiate(upgradeTargetPrefab, _lanes.GetSpawnPosition(laneIndex), Quaternion.identity);
            instance.SetActive(true);
            _activeUpgradeTarget = instance.GetComponent<UpgradeTarget>();
            if (_activeUpgradeTarget == null)
            {
                Destroy(instance);
                StartNextWave();
                return;
            }

            _activeUpgradeTarget.Initialize(_session, _lanes.GetLaneX(laneIndex), _lanes.PlayerZ, _lanes.ActorY, upgradeTargetSpeed);
            _activeUpgradeTarget.Resolved += OnUpgradeTargetResolved;
        }

        void OnUpgradeTargetResolved(UpgradeTarget target, bool collected)
        {
            target.Resolved -= OnUpgradeTargetResolved;
            if (_activeUpgradeTarget == target)
            {
                _activeUpgradeTarget = null;
            }

            if (_session == null || !_session.IsPlaying || Phase != WavePhase.Upgrade)
            {
                return;
            }

            if (collected)
            {
                _autoFire?.EnableDoubleShot();
            }

            StartNextWave();
        }

        bool IsActiveWave()
        {
            return _session != null && _session.IsPlaying && Phase == WavePhase.Spawning && _progress != null;
        }
    }
}
