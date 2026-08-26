using UnityEngine;

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
        bool _consumed;

        public void Initialize(GameSession session)
        {
            _session = session;
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
            if (_consumed || _session == null || !_session.IsPlaying)
            {
                return;
            }

            if (IsInLayerMask(other.gameObject.layer, dividerLayers))
            {
                _consumed = true;
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

            _consumed = true;
            health.TakeDamage(damage);
            Destroy(gameObject);
        }

        static bool IsInLayerMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }
    }
}
