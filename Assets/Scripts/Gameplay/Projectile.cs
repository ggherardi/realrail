using UnityEngine;
using System.Collections.Generic;

namespace RealRail
{
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] float speed = 22f;
        [SerializeField] int damage = 1;
        [SerializeField] float maxZ = 30f;
        [SerializeField] LayerMask enemyLayers;
        [SerializeField] LayerMask dividerLayers;

        GameSession _session;
        readonly HashSet<Health> _hitTargets = new HashSet<Health>();
        int _configuredDamage = 1;
        int _distinctHitCapacity = 1;
        bool _resolved;
        bool _isRailgunPrototype;
        Vector3 _normalVisualScale;
        Renderer _visualRenderer;

        public int DistinctHitCount => _hitTargets.Count;
        public bool IsResolved => _resolved;
        public bool IsRailgunPrototype => _isRailgunPrototype;

        public void Initialize(GameSession session)
        {
            _session = session;
            Initialize(session, damage, 1);
        }

        public void Initialize(GameSession session, int configuredDamage, int distinctHitCapacity)
        {
            Initialize(session, configuredDamage, distinctHitCapacity, false);
        }

        public void Initialize(GameSession session, int configuredDamage, int distinctHitCapacity, bool isRailgunPrototype)
        {
            _session = session;
            _configuredDamage = Mathf.Max(1, configuredDamage);
            _distinctHitCapacity = Mathf.Max(1, distinctHitCapacity);
            _isRailgunPrototype = isRailgunPrototype;
            _hitTargets.Clear();
            _resolved = false;
            ApplyPrototypePresentation();
        }

        void Update()
        {
            if (_session != null && !_session.IsPlaying)
            {
                return;
            }

            var position = transform.position;
            position.z += speed * Time.deltaTime;
            transform.position = position;

            if (position.z >= maxZ)
            {
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (_resolved || _session == null || !_session.IsPlaying)
            {
                return;
            }

            if (IsInLayerMask(other.gameObject.layer, dividerLayers))
            {
                _resolved = true;
                Destroy(gameObject);
                return;
            }

            if (!IsInLayerMask(other.gameObject.layer, enemyLayers))
            {
                return;
            }

            var health = other.GetComponentInParent<Health>();
            if (health == null)
            {
                return;
            }

            TryApplyHit(health);
        }

        public bool TryApplyHit(Health health)
        {
            if (_resolved || health == null || !_hitTargets.Add(health))
            {
                return false;
            }

            health.TakeDamage(_configuredDamage);
            if (_hitTargets.Count >= _distinctHitCapacity)
            {
                _resolved = true;
                Destroy(gameObject);
            }
            return true;
        }

        void ApplyPrototypePresentation()
        {
            if (_visualRenderer == null)
            {
                _visualRenderer = GetComponentInChildren<Renderer>();
                if (_visualRenderer != null) _normalVisualScale = _visualRenderer.transform.localScale;
            }
            if (_visualRenderer == null) return;

            _visualRenderer.transform.localScale = _isRailgunPrototype
                ? Vector3.Scale(_normalVisualScale, new Vector3(3f, 3f, 12f))
                : _normalVisualScale;

            if (!_isRailgunPrototype)
            {
                _visualRenderer.SetPropertyBlock(null);
                return;
            }

            var block = new MaterialPropertyBlock();
            _visualRenderer.GetPropertyBlock(block);
            var color = new Color(0.2f, 0.9f, 1f, 1f);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            block.SetColor("_EmissionColor", color * 2f);
            _visualRenderer.SetPropertyBlock(block);
        }

        static bool IsInLayerMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }
    }
}
