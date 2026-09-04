using System;
using UnityEngine;

namespace RealRail
{
    public enum SessionState
    {
        Playing,
        Lost,
        Victory
    }

    /// <summary>Gameplay facts reported by normal systems without exposing their implementation details.</summary>
    public enum SessionTelemetryEventType
    {
        WaveStarted,
        EnemyKilled,
        EnemyLeaked,
        PlayerDamaged
    }

    public readonly struct SessionTelemetryEvent
    {
        public SessionTelemetryEvent(SessionTelemetryEventType type, int value = 0)
        {
            Type = type;
            Value = value;
        }

        public SessionTelemetryEventType Type { get; }
        /// <summary>Wave number for WaveStarted; damage amount for PlayerDamaged.</summary>
        public int Value { get; }
    }

    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] Health playerHealth;

        public SessionState State { get; private set; } = SessionState.Playing;
        public bool IsPlaying => State == SessionState.Playing;
        public bool GodMode { get; private set; }
        public float ElapsedRunSeconds { get; private set; }

        public event Action Lost;
        public event Action Victory;
        public event Action<bool> GodModeChanged;
        public event Action<SessionTelemetryEvent> TelemetryEvent;

        void Update()
        {
            if (IsPlaying)
            {
                ElapsedRunSeconds += Time.deltaTime;
            }
        }

        void Awake()
        {
            BindPlayer(playerHealth);
        }

        public void BindPlayer(Health playerHealth)
        {
            if (playerHealth != null)
            {
                playerHealth.Died -= OnPlayerDied;
            }

            this.playerHealth = playerHealth;
            if (this.playerHealth != null)
            {
                this.playerHealth.Died += OnPlayerDied;
            }
        }

        void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.Died -= OnPlayerDied;
            }
        }

        void OnPlayerDied()
        {
            if (!IsPlaying)
            {
                return;
            }

            State = SessionState.Lost;
            Lost?.Invoke();
        }

        public void Win()
        {
            if (!IsPlaying)
            {
                return;
            }

            State = SessionState.Victory;
            Victory?.Invoke();
        }

        public void ApplyPlayerDamage(int amount)
        {
            if (!IsPlaying || playerHealth == null || amount <= 0 || GodMode)
            {
                return;
            }

            var before = playerHealth.Current;
            playerHealth.TakeDamage(amount);
            var applied = before - playerHealth.Current;
            if (applied > 0)
            {
                TelemetryEvent?.Invoke(new SessionTelemetryEvent(SessionTelemetryEventType.PlayerDamaged, applied));
            }
        }

        public void SetGodMode(bool enabled)
        {
            if (GodMode == enabled)
            {
                return;
            }

            GodMode = enabled;
            GodModeChanged?.Invoke(GodMode);
        }

        /// <summary>
        /// Returns the session to its initial playable state. Runtime owners are responsible for
        /// clearing transient actors before calling this method.
        /// </summary>
        public void ResetRun()
        {
            State = SessionState.Playing;
            ElapsedRunSeconds = 0f;
            if (playerHealth != null)
            {
                playerHealth.SetMaxHealth(playerHealth.Max);
            }
        }

        /// <summary>Records a gameplay wave boundary without coupling the session to any wave implementation.</summary>
        public void ReportWaveStarted(int waveNumber)
        {
            if (IsPlaying && waveNumber > 0)
            {
                TelemetryEvent?.Invoke(new SessionTelemetryEvent(SessionTelemetryEventType.WaveStarted, waveNumber));
            }
        }

        /// <summary>Records the authoritative resolution reported by an enemy-owning gameplay system.</summary>
        public void ReportEnemyResolved(WaveEnemyResolution resolution)
        {
            if (!IsPlaying)
            {
                return;
            }

            var type = resolution == WaveEnemyResolution.Killed
                ? SessionTelemetryEventType.EnemyKilled
                : SessionTelemetryEventType.EnemyLeaked;
            TelemetryEvent?.Invoke(new SessionTelemetryEvent(type));
        }
    }
}
