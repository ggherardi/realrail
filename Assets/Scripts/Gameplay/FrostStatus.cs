using UnityEngine;

namespace RealRail
{
    /// <summary>Prototype-only enemy-local Frost presentation. EnemyMover drives expiry while it already updates movement.</summary>
    public sealed class FrostStatus : MonoBehaviour
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int Color = Shader.PropertyToID("_Color");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] Color frostColor = new Color(0.32f, 0.88f, 1f, 1f);
        [SerializeField, Min(0f)] float emissionIntensity = 0.7f;

        Renderer[] _renderers;
        float _expiresAt;

        public bool IsFrosted { get; private set; }
        public float ExpiresAt => _expiresAt;

        void Awake()
        {
            _renderers = transform.Find("Visual") != null
                ? transform.Find("Visual").GetComponentsInChildren<Renderer>(true)
                : GetComponentsInChildren<Renderer>(true);
        }

        public void Apply(float durationSeconds)
        {
            _expiresAt = Time.time + Mathf.Max(0f, durationSeconds);
            IsFrosted = true;
            ApplyVisual(true);
        }

        public void ClearIfExpired(float now)
        {
            if (!IsFrosted || now < _expiresAt) return;
            IsFrosted = false;
            ApplyVisual(false);
        }

        void ApplyVisual(bool frosted)
        {
            if (_renderers == null) return;
            foreach (var targetRenderer in _renderers)
            {
                if (targetRenderer == null) continue;
                if (!frosted)
                {
                    targetRenderer.SetPropertyBlock(null);
                    continue;
                }

                var block = new MaterialPropertyBlock();
                targetRenderer.GetPropertyBlock(block);
                block.SetColor(BaseColor, frostColor);
                block.SetColor(Color, frostColor);
                block.SetColor(EmissionColor, frostColor * emissionIntensity);
                targetRenderer.SetPropertyBlock(block);
            }
        }
    }
}
