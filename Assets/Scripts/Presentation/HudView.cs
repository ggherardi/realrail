using UnityEngine;
using UnityEngine.UI;

namespace RealRail
{
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] Text hpText;
        [SerializeField] Text gameOverText;
        [SerializeField] Text victoryText;
        [SerializeField] Health playerHealth;
        [SerializeField] GameSession session;

        void Awake()
        {
            Bind(hpText, gameOverText, victoryText, playerHealth, session);
        }

        public void Bind(Text hitPoints, Text gameOver, Text victory, Health playerHealth, GameSession session)
        {
            hpText = hitPoints;
            gameOverText = gameOver;
            victoryText = victory;
            this.playerHealth = playerHealth;
            this.session = session;

            if (this.playerHealth != null)
            {
                this.playerHealth.Changed += OnHealthChanged;
                OnHealthChanged(this.playerHealth.Current, this.playerHealth.Max);
            }

            if (gameOverText != null)
            {
                gameOverText.gameObject.SetActive(false);
            }

            if (victoryText != null)
            {
                victoryText.gameObject.SetActive(false);
            }

            if (this.session != null)
            {
                this.session.Lost += OnLost;
                this.session.Victory += OnVictory;
            }
        }

        void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.Changed -= OnHealthChanged;
            }

            if (session != null)
            {
                session.Lost -= OnLost;
                session.Victory -= OnVictory;
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
