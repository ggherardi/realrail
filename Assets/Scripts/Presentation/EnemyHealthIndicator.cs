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
        public float DisplayedFillFraction { get; private set; }

        RectTransform _fillRect;
        float _fullFillWidth;
        float _fillLeftInset;
        bool _fillGeometryCached;

        void Awake()
        {
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            CacheFillGeometry();
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
            DisplayedFillFraction = max > 0 ? (float)current / max : 0f;

            if (fillImage != null)
            {
                fillImage.fillAmount = DisplayedFillFraction;
                CacheFillGeometry();
                if (_fillRect != null)
                {
                    // The current indicator uses Unity's sprite-less white image. Its Filled
                    // mode does not crop visible geometry, so size the same Image from the left.
                    _fillRect.SetInsetAndSizeFromParentEdge(
                        RectTransform.Edge.Left,
                        _fillLeftInset,
                        _fullFillWidth * DisplayedFillFraction);
                }
            }

            if (hitPointsText != null)
            {
                hitPointsText.text = $"{current}/{max}";
            }
        }

        void CacheFillGeometry()
        {
            if (_fillGeometryCached || fillImage == null)
            {
                return;
            }

            _fillRect = fillImage.rectTransform;
            var parentRect = _fillRect.parent as RectTransform;
            if (parentRect == null)
            {
                return;
            }

            _fillLeftInset = _fillRect.offsetMin.x;
            _fullFillWidth = parentRect.rect.width - _fillLeftInset + _fillRect.offsetMax.x;
            _fillGeometryCached = true;
        }
    }
}
