using UnityEngine;

namespace RealRail
{
    public sealed class DestroyWhenDead : MonoBehaviour
    {
        Health _health;

        void Awake()
        {
            _health = GetComponent<Health>();
            if (_health != null)
            {
                _health.Died += OnDied;
            }
        }

        void OnDestroy()
        {
            if (_health != null)
            {
                _health.Died -= OnDied;
            }
        }

        void OnDied()
        {
            Destroy(gameObject);
        }
    }
}
