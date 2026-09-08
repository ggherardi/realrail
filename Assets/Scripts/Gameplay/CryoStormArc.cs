using UnityEngine;

namespace RealRail
{
    /// <summary>Short-lived greybox transfer cue. One arc exists per propagation step and destroys its owned material.</summary>
    public sealed class CryoStormArc : MonoBehaviour
    {
        const float Lifetime = 0.16f;

        float _expiresAt;
        Material _material;

        public static void Create(Vector3 from, Vector3 to)
        {
            var owner = new GameObject("CryoStormArc");
            owner.AddComponent<CryoStormArc>().Initialize(from, to);
        }

        void Initialize(Vector3 from, Vector3 to)
        {
            var line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, from + Vector3.up * 0.6f);
            line.SetPosition(1, to + Vector3.up * 0.6f);
            line.startWidth = 0.11f;
            line.endWidth = 0.045f;
            line.numCapVertices = 2;
            _material = new Material(Shader.Find("Sprites/Default"));
            line.sharedMaterial = _material;
            var cyan = new Color(0.42f, 0.95f, 1f, 1f);
            line.startColor = cyan;
            line.endColor = new Color(cyan.r, cyan.g, cyan.b, 0.15f);
            _expiresAt = Time.time + Lifetime;
        }

        void Update()
        {
            if (Time.time < _expiresAt) return;
            if (_material != null) Destroy(_material);
            Destroy(gameObject);
        }
    }
}
