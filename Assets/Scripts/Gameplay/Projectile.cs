using UnityEngine;

namespace RealRail
{
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] float speed = 22f;
        [SerializeField] int damage = 1;
        [SerializeField] float maxZ = 30f;

        GameSession _session;
        int _enemyLayer;
        int _dividerLayer;
        bool _consumed;

        public void Initialize(GameSession session, int enemyLayer, int dividerLayer)
        {
            _session = session;
            _enemyLayer = enemyLayer;
            _dividerLayer = dividerLayer;
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

            if (other.gameObject.layer == _dividerLayer)
            {
                _consumed = true;
                Destroy(gameObject);
                return;
            }

            if (other.gameObject.layer != _enemyLayer)
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
    }
}
