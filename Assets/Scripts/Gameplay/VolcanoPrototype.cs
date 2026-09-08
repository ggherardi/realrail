using System;
using UnityEngine;

namespace RealRail
{
    /// <summary>Experimental Fire + Explosion impact-eruption prototype; deliberately independent from Fusion acquisition.</summary>
    public sealed class VolcanoPrototype : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField] AutoFire autoFire;
        [SerializeField] bool enabledForDebug;
        [SerializeField, Min(2f)] float cadenceSeconds = 5f;
        [SerializeField, Min(1)] int impactDamage = 3;
        [SerializeField, Min(0.5f)] float impactRadius = 2.4f;
        [SerializeField, Range(1, 4)] int secondaryEruptionCount = 3;
        [SerializeField, Min(0.1f)] float secondaryDelaySeconds = 0.8f;
        [SerializeField, Min(1)] int secondaryDamage = 2;
        [SerializeField, Min(0.5f)] float secondaryRadius = 2f;
        [SerializeField, Min(0.5f)] float eruptionLifetimeSeconds = 3f;

        float _nextSpawnTime;
        public bool IsEnabled => enabledForDebug;
        public float CadenceSeconds => cadenceSeconds;
        public event Action<bool> Changed;

        void Update()
        {
            if (!enabledForDebug || session == null || !session.IsPlaying || Time.time < _nextSpawnTime) return;
        }

        public void SetEnabled(bool enabled)
        {
            if (enabledForDebug == enabled) return;
            enabledForDebug = enabled;
            _nextSpawnTime = Time.time + cadenceSeconds;
            Changed?.Invoke(enabled);
        }

        public bool TryConsumeScheduledShot(float now)
        {
            if (!enabledForDebug || now < _nextSpawnTime) return false;
            _nextSpawnTime = now + cadenceSeconds;
            return true;
        }

        public bool FireImmediate()
        {
            if (session == null || !session.IsPlaying || autoFire == null) return false;
            FireShot(autoFire.MuzzlePosition);
            return true;
        }

        public void FireShot(Vector3 origin)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "Volcano Shot (Prototype)";
            root.transform.position = origin;
            root.transform.localScale = Vector3.one * 0.38f;
            root.layer = LayerMask.NameToLayer(GameplayLayers.Projectile);
            var collider = root.GetComponent<SphereCollider>();
            collider.isTrigger = true;
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var shot = root.AddComponent<VolcanoImpactShot>();
            shot.Initialize(session, this, 28f, 42f);
            var renderer = root.GetComponent<Renderer>();
            renderer.material.color = new Color(1f, 0.18f, 0.02f);
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", new Color(1f, 0.08f, 0f) * 2f);
        }

        public void CreateEruption(Vector3 impactPosition)
        {
            var root = new GameObject("Volcano Impact Eruption (Prototype)");
            root.transform.position = impactPosition;
            var eruption = root.AddComponent<VolcanoEruption>();
            eruption.Configure(session, impactDamage, impactRadius, secondaryEruptionCount, secondaryDelaySeconds,
                secondaryDamage, secondaryRadius, eruptionLifetimeSeconds);
        }
    }
}
