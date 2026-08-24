using UnityEngine;
using UnityEngine.UI;

namespace RealRail
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] Text hpText;
        [SerializeField] Text gameOverText;

        Health _health;
        GameSession _session;

        public void Bind(Text hitPoints, Text gameOver, Health playerHealth, GameSession session)
        {
            hpText = hitPoints;
            gameOverText = gameOver;
            _health = playerHealth;
            _session = session;

            if (_health != null)
            {
                _health.Changed += OnHealthChanged;
                OnHealthChanged(_health.Current, _health.Max);
            }

            if (gameOverText != null)
            {
                gameOverText.gameObject.SetActive(false);
            }

            if (_session != null)
            {
                _session.Lost += OnLost;
            }
        }

        void OnDestroy()
        {
            if (_health != null)
            {
                _health.Changed -= OnHealthChanged;
            }

            if (_session != null)
            {
                _session.Lost -= OnLost;
            }
        }

        void OnHealthChanged(int current, int max)
        {
            if (hpText != null)
            {
                hpText.text = $"HP {current}/{max}";
            }
        }

        void OnLost()
        {
            if (gameOverText != null)
            {
                gameOverText.gameObject.SetActive(true);
            }
        }
    }
}
