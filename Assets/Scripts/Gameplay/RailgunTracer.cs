using UnityEngine;

namespace RealRail
{
    /// <summary>Short-lived, thin trajectory cue for the high-speed Railgun prototype.</summary>
    public sealed class RailgunTracer : MonoBehaviour
    {
        const float Lifetime = 0.14f;

        float _expiresAt;
        LineRenderer _line;
        Material _material;

        public static void Create(Vector3 origin, float maxZ, Material sourceMaterial, Color color)
        {
            var owner = new GameObject("RailgunTracer");
            var tracer = owner.AddComponent<RailgunTracer>();
            tracer.Initialize(origin, maxZ, sourceMaterial, color);
        }

        void Initialize(Vector3 origin, float maxZ, Material sourceMaterial, Color color)
        {
            _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.SetPosition(0, origin);
            _line.SetPosition(1, new Vector3(origin.x, origin.y, maxZ));
            _line.startWidth = 0.18f;
            _line.endWidth = 0.06f;
            _line.numCapVertices = 2;
            _material = sourceMaterial != null ? new Material(sourceMaterial) : null;
            _line.sharedMaterial = _material;
            _line.startColor = color;
            _line.endColor = new Color(color.r, color.g, color.b, 0f);
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
