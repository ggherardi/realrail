using UnityEngine;
using UnityEngine.UI;

namespace RealRail
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] Text hpText;
        [SerializeField] Text gameOverText;
        [SerializeField] Text victoryText;

        Health _health;
        GameSession _session;

        public void Bind(Text hitPoints, Text gameOver, Text victory, Health playerHealth, GameSession session)
        {
            hpText = hitPoints;
            gameOverText = gameOver;
            victoryText = victory;
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

            if (victoryText != null)
            {
                victoryText.gameObject.SetActive(false);
            }

            if (_session != null)
            {
                _session.Lost += OnLost;
                _session.Victory += OnVictory;
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
                _session.Victory -= OnVictory;
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

        void OnVictory()
        {
            if (victoryText != null)
            {
                victoryText.gameObject.SetActive(true);
            }
        }
    }
}
