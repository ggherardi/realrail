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
        [SerializeField] Text upgradeText;
        [SerializeField] UpgradeSystem upgradeSystem;

        float _upgradeTextHideTime;

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

            if (upgradeText != null)
            {
                upgradeText.gameObject.SetActive(false);
            }

            if (upgradeSystem != null)
            {
                upgradeSystem.UpgradeApplied += OnUpgradeApplied;
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

            if (upgradeSystem != null)
            {
                upgradeSystem.UpgradeApplied -= OnUpgradeApplied;
            }
        }

        void Update()
        {
            if (upgradeText != null && upgradeText.gameObject.activeSelf && Time.time >= _upgradeTextHideTime)
            {
                upgradeText.gameObject.SetActive(false);
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

        void OnUpgradeApplied(UpgradeApplication application)
        {
            if (upgradeText == null)
            {
                return;
            }

            upgradeText.text = $"{DisplayName(application.Upgrade)} {ToRoman(application.Level)}";
            upgradeText.gameObject.SetActive(true);
            _upgradeTextHideTime = Time.time + 2.5f;
        }

        static string DisplayName(UpgradeId upgrade) => upgrade switch
        {
            UpgradeId.DoubleShot => "Double Shot",
            UpgradeId.RapidFire => "Rapid Fire",
            UpgradeId.PiercingShot => "Piercing Shot",
            UpgradeId.PowerShot => "Power Shot",
            _ => upgrade.ToString()
        };

        static string ToRoman(int level) => level switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            _ => level.ToString()
        };
    }
}
