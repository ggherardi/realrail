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

    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] Health playerHealth;

        public SessionState State { get; private set; } = SessionState.Playing;
        public bool IsPlaying => State == SessionState.Playing;

        public event Action Lost;
        public event Action Victory;

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
    }
}
