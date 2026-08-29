using UnityEngine;
using UnityEngine.UI;

namespace RealRail
{
    /// <summary>
    /// Presentation-only view of an enemy's existing <see cref="Health"/> component.
    /// </summary>
    public sealed class EnemyHealthIndicator : MonoBehaviour
    {
        [SerializeField] Health health;
        [SerializeField] Image fillImage;
        [SerializeField] Text hitPointsText;

        public Health HealthSource => health;
        public int DisplayedCurrent { get; private set; }
        public int DisplayedMax { get; private set; }

        void Awake()
        {
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            Bind(health);
        }

        void OnDestroy()
        {
            if (health != null)
            {
                health.Changed -= OnHealthChanged;
            }
        }

        public void Bind(Health source)
        {
            if (health != null)
            {
                health.Changed -= OnHealthChanged;
            }

            health = source;
            if (health == null)
            {
                return;
            }

            health.Changed += OnHealthChanged;
            OnHealthChanged(health.Current, health.Max);
        }

        void OnHealthChanged(int current, int max)
        {
            DisplayedCurrent = current;
            DisplayedMax = max;

            if (fillImage != null)
            {
                fillImage.fillAmount = max > 0 ? (float)current / max : 0f;
            }

            if (hitPointsText != null)
            {
                hitPointsText.text = $"{current}/{max}";
            }
        }
    }
}
