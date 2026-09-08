using UnityEngine;

namespace RealRail
{
    public sealed class VolcanoImpactShot : MonoBehaviour
    {
        GameSession _session;
        VolcanoPrototype _prototype;
        float _speed;
        float _maxZ;
        bool _resolved;

        public void Initialize(GameSession session, VolcanoPrototype prototype, float speed, float maxZ)
        {
            _session = session; _prototype = prototype; _speed = speed; _maxZ = maxZ;
        }

        void Update()
        {
            if (_session == null || !_session.IsPlaying) return;
            transform.position += Vector3.forward * (_speed * Time.deltaTime);
            if (transform.position.z >= _maxZ) Destroy(gameObject);
        }

        void OnTriggerEnter(Collider other)
        {
            if (_resolved || _session == null || !_session.IsPlaying || other.GetComponentInParent<WaveEnemy>() == null) return;
            _resolved = true;
            _prototype?.CreateEruption(transform.position);
            Destroy(gameObject);
        }
    }
}
