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
        [SerializeField] UpgradeRewardSelection rewardSelection;

        int _finalWaveReached;
        int _enemiesKilled;
        int _enemiesLeaked;
        int _playerDamageTaken;
        int? _runSeed;
        readonly List<string> _upgradeOffers = new List<string>();
        readonly List<UpgradeId> _upgradeSelections = new List<UpgradeId>();

        public RunResult CurrentResult => BuildResult();

        /// <summary>Clears collected facts so the component can observe a fresh session run.</summary>
        public void ResetRun()
        {
            _finalWaveReached = 0;
            _enemiesKilled = 0;
            _enemiesLeaked = 0;
            _playerDamageTaken = 0;
            _runSeed = null;
            _upgradeOffers.Clear();
            _upgradeSelections.Clear();
        }

        public void SetRunSeed(int? seed) => _runSeed = seed;

        void Awake()
        {
            session ??= FindAnyObjectByType<GameSession>();
            upgradeSystem ??= FindAnyObjectByType<UpgradeSystem>();
            rewardSelection ??= FindAnyObjectByType<UpgradeRewardSelection>();
        }

        void OnEnable()
        {
            if (session != null) session.TelemetryEvent += OnSessionTelemetry;
            if (upgradeSystem != null) upgradeSystem.UpgradeApplied += OnUpgradeApplied;
            if (rewardSelection != null) rewardSelection.SelectionStarted += OnRewardOffered;
        }

        void OnDisable()
        {
            if (session != null) session.TelemetryEvent -= OnSessionTelemetry;
            if (upgradeSystem != null) upgradeSystem.UpgradeApplied -= OnUpgradeApplied;
            if (rewardSelection != null) rewardSelection.SelectionStarted -= OnRewardOffered;
        }

        public void ConfigureForTests(GameSession gameSession, UpgradeSystem upgrades)
        {
            if (session != null) session.TelemetryEvent -= OnSessionTelemetry;
            if (upgradeSystem != null) upgradeSystem.UpgradeApplied -= OnUpgradeApplied;
            if (rewardSelection != null) rewardSelection.SelectionStarted -= OnRewardOffered;
            session = gameSession;
            upgradeSystem = upgrades;
            if (isActiveAndEnabled && session != null) session.TelemetryEvent += OnSessionTelemetry;
            if (isActiveAndEnabled && upgradeSystem != null) upgradeSystem.UpgradeApplied += OnUpgradeApplied;
        }

        public void ConfigureRewardSelectionForTests(UpgradeRewardSelection selection)
        {
            if (rewardSelection != null) rewardSelection.SelectionStarted -= OnRewardOffered;
            rewardSelection = selection;
            if (isActiveAndEnabled && rewardSelection != null) rewardSelection.SelectionStarted += OnRewardOffered;
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
                upgrades, _runSeed, _upgradeOffers, _upgradeSelections);
        }

        void OnRewardOffered(IReadOnlyList<UpgradeId> choices)
        {
            if (choices == null) return;
            _upgradeOffers.Add(string.Join(",", choices));
        }

        void OnUpgradeApplied(UpgradeApplication application)
        {
            _upgradeSelections.Add(application.Upgrade);
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
