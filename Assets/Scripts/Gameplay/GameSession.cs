using System;
using UnityEngine;

namespace RealRail
{
    public enum SessionState
    {
        Playing,
        Lost
    }

    public sealed class GameSession : MonoBehaviour
    {
        Health _playerHealth;

        public SessionState State { get; private set; } = SessionState.Playing;
        public bool IsPlaying => State == SessionState.Playing;

        public event Action Lost;

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
            if (State == SessionState.Lost)
            {
                return;
            }

            State = SessionState.Lost;
            Lost?.Invoke();
        }
    }
}
