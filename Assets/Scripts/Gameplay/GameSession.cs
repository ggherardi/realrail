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
        Health _playerHealth;

        public SessionState State { get; private set; } = SessionState.Playing;
        public bool IsPlaying => State == SessionState.Playing;

        public event Action Lost;
        public event Action Victory;

        public void BindPlayer(Health playerHealth)
        {
            if (_playerHealth != null)
            {
                _playerHealth.Died -= OnPlayerDied;
            }

            _playerHealth = playerHealth;
            if (_playerHealth != null)
            {
                _playerHealth.Died += OnPlayerDied;
            }
        }

        void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.Died -= OnPlayerDied;
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
