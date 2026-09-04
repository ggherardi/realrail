using System.Collections.Generic;
using UnityEngine;

namespace RealRail
{
    /// <summary>Collects a small structured result from session-level facts and upgrade application.</summary>
    [DisallowMultipleComponent]
    public sealed class RunTelemetry : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField] UpgradeSystem upgradeSystem;

        int _finalWaveReached;
        int _enemiesKilled;
        int _enemiesLeaked;
        int _playerDamageTaken;

        public RunResult CurrentResult => BuildResult();

        void Awake()
        {
            session ??= FindFirstObjectByType<GameSession>();
            upgradeSystem ??= FindFirstObjectByType<UpgradeSystem>();
        }

        void OnEnable()
        {
            if (session != null) session.TelemetryEvent += OnSessionTelemetry;
        }

        void OnDisable()
        {
            if (session != null) session.TelemetryEvent -= OnSessionTelemetry;
        }

        public void ConfigureForTests(GameSession gameSession, UpgradeSystem upgrades)
        {
            if (session != null) session.TelemetryEvent -= OnSessionTelemetry;
            session = gameSession;
            upgradeSystem = upgrades;
            if (isActiveAndEnabled && session != null) session.TelemetryEvent += OnSessionTelemetry;
        }

        public RunResult BuildResult()
        {
            var upgrades = new List<AcquiredUpgrade>();
            if (upgradeSystem != null)
            {
                foreach (UpgradeId upgrade in System.Enum.GetValues(typeof(UpgradeId)))
                {
                    var level = upgradeSystem.State.GetLevel(upgrade);
                    if (level > 0) upgrades.Add(new AcquiredUpgrade(upgrade, level));
                }
            }

            return new RunResult(
                session != null ? session.State : SessionState.Playing,
                session != null ? session.ElapsedRunSeconds : 0f,
                _finalWaveReached,
                _enemiesKilled,
                _enemiesLeaked,
                _playerDamageTaken,
                upgrades);
        }

        void OnSessionTelemetry(SessionTelemetryEvent telemetryEvent)
        {
            switch (telemetryEvent.Type)
            {
                case SessionTelemetryEventType.WaveStarted:
                    _finalWaveReached = Mathf.Max(_finalWaveReached, telemetryEvent.Value);
                    break;
                case SessionTelemetryEventType.EnemyKilled:
                    _enemiesKilled++;
                    break;
                case SessionTelemetryEventType.EnemyLeaked:
                    _enemiesLeaked++;
                    break;
                case SessionTelemetryEventType.PlayerDamaged:
                    _playerDamageTaken += telemetryEvent.Value;
                    break;
            }
        }
    }
}
