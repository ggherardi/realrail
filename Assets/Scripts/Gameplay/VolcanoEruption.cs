using System.Collections.Generic;
using UnityEngine;

namespace RealRail
{
    /// <summary>Discrete impact and secondary weapon bursts. No blocking collider or per-enemy update is used.</summary>
    public sealed class VolcanoEruption : MonoBehaviour
    {
        const int CandidateBufferSize = 48;
        readonly Collider[] _candidates = new Collider[CandidateBufferSize];
        readonly HashSet<Health> _hitThisBurst = new HashSet<Health>();
        GameSession _session;
        int _secondaryRemaining;
        int _secondaryDamage;
        float _secondaryRadius;
        float _nextEruptionTime;
        float _expiryTime;

        public int SecondaryRemaining => _secondaryRemaining;

        public void Configure(GameSession session, int impactDamage, float impactRadius, int secondaryCount, float delay,
            int secondaryDamage, float secondaryRadius, float lifetime)
        {
            _session = session;
            _secondaryRemaining = Mathf.Clamp(secondaryCount, 1, 4);
            _secondaryDamage = Mathf.Max(1, secondaryDamage);
            _secondaryRadius = Mathf.Max(0.5f, secondaryRadius);
            _nextEruptionTime = Time.time + Mathf.Max(0.1f, delay);
            _expiryTime = Time.time + Mathf.Max(0.5f, lifetime);
            Burst(Mathf.Max(1, impactDamage), Mathf.Max(0.5f, impactRadius), Vector3.zero);
            CreateVisual(Mathf.Max(0.5f, impactRadius));
        }

        void Update()
        {
            if (_session == null || !_session.IsPlaying) { Destroy(gameObject); return; }
            if (_secondaryRemaining > 0 && Time.time >= _nextEruptionTime)
            {
                var offset = new Vector3((_secondaryRemaining % 2 == 0 ? -0.35f : 0.35f), 0f, 0.25f);
                Burst(_secondaryDamage, _secondaryRadius, offset);
                CreateVisual(_secondaryRadius);
                _secondaryRemaining--;
                _nextEruptionTime += 0.8f;
            }
            if (Time.time >= _expiryTime) Destroy(gameObject);
        }

        public int Burst(int damage, float radius, Vector3 localOffset)
        {
            _hitThisBurst.Clear();
            var center = transform.position + localOffset;
            var count = Physics.OverlapSphereNonAlloc(center, radius, _candidates, 1 << LayerMask.NameToLayer(GameplayLayers.Enemy), QueryTriggerInteraction.Collide);
            var hitCount = 0;
            for (var index = 0; index < count; index++)
            {
                var health = _candidates[index] != null ? _candidates[index].GetComponentInParent<Health>() : null;
                if (health == null || !_hitThisBurst.Add(health)) continue;
                if (health.Current <= damage) _candidates[index].GetComponentInParent<WaveEnemy>()?.ResolveKilled();
                health.TakeDamage(damage);
                hitCount++;
            }
            return hitCount;
        }

        void CreateVisual(float radius)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Eruption Burst";
            visual.transform.position = transform.position + Vector3.up * 0.55f;
            visual.transform.localScale = new Vector3(radius * 0.65f, 1.4f, radius * 0.65f);
            visual.transform.SetParent(transform, true);
            Destroy(visual.GetComponent<Collider>());
            var renderer = visual.GetComponent<Renderer>();
            renderer.material.color = new Color(0.9f, 0.12f, 0.01f);
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", new Color(1f, 0.16f, 0f) * 2.5f);
            Destroy(visual, 0.28f);
        }
    }
}
