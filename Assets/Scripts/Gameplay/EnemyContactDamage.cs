using UnityEngine;

namespace RealRail
{
    public sealed class EnemyContactDamage : MonoBehaviour
    {
        [SerializeField] int damage = 1;
        [SerializeField] LayerMask playerLayers;

        GameSession _session;
        bool _applied;

        public void Initialize(GameSession session)
        {
            _session = session;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_applied || _session == null || !_session.IsPlaying)
            {
                return;
            }

            if ((playerLayers.value & (1 << other.gameObject.layer)) == 0)
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
