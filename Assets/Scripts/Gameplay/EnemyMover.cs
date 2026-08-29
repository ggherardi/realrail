using System;
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
        bool _hasReachedDestination;

        public float BaseSpeed => speed;

        public event Action DestinationReached;

        public void Initialize(GameSession session, float laneX, float targetZ, float y, float movementSpeed = -1f)
        {
            _session = session;
            _laneX = laneX;
            _targetZ = targetZ;
            _y = y;
            _hasReachedDestination = false;
            if (movementSpeed >= 0f)
            {
                speed = movementSpeed;
            }
        }

        void Update()
        {
            Advance(Time.deltaTime);
        }

        public void Advance(float deltaTime)
        {
            if (_session != null && !_session.IsPlaying)
            {
                return;
            }

            var position = transform.position;
            position.x = _laneX;
            position.y = _y;
            position.z = Mathf.MoveTowards(position.z, _targetZ, speed * deltaTime);
            transform.position = position;

            if (!_hasReachedDestination && Mathf.Approximately(position.z, _targetZ))
            {
                _hasReachedDestination = true;
                DestinationReached?.Invoke();
            }
        }
    }
}
