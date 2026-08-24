using UnityEngine;

namespace RealRail
{
    public sealed class EnemyMover : MonoBehaviour
    {
        [SerializeField] float speed = 4f;

        GameSession _session;
        float _laneX;
        float _targetZ;
        float _y;

        public void Initialize(GameSession session, float laneX, float targetZ, float y)
        {
            _session = session;
            _laneX = laneX;
            _targetZ = targetZ;
            _y = y;
        }

        void Update()
        {
            if (_session != null && !_session.IsPlaying)
            {
                return;
            }

            var position = transform.position;
            position.x = _laneX;
            position.y = _y;
            position.z = Mathf.MoveTowards(position.z, _targetZ, speed * Time.deltaTime);
            transform.position = position;
        }
    }
}
