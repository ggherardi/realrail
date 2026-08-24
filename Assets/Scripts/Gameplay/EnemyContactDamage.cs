using UnityEngine;

namespace RealRail
{
    public sealed class EnemyContactDamage : MonoBehaviour
    {
        [SerializeField] int damage = 1;

        GameSession _session;
        int _playerLayer;
        bool _applied;

        public void Initialize(GameSession session, int playerLayer)
        {
            _session = session;
            _playerLayer = playerLayer;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_applied || _session == null || !_session.IsPlaying)
            {
                return;
            }

            if (other.gameObject.layer != _playerLayer)
            {
                return;
            }

            var health = other.GetComponentInParent<Health>();
            if (health == null)
            {
                return;
            }

            _applied = true;
            health.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
