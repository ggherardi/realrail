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
        float _temporarySpeedMultiplier = 1f;
        float _temporarySpeedExpiry;
        FrostStatus _frostStatus;

        public float BaseSpeed => speed;
        public float EffectiveSpeed => speed * _temporarySpeedMultiplier;

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

            RefreshTemporaryEffects(Time.time);
            var position = transform.position;
            position.x = _laneX;
            position.y = _y;
            position.z = Mathf.MoveTowards(position.z, _targetZ, EffectiveSpeed * deltaTime);
            transform.position = position;

            if (!_hasReachedDestination && Mathf.Approximately(position.z, _targetZ))
            {
                _hasReachedDestination = true;
                DestinationReached?.Invoke();
            }
        }

        /// <summary>Applies the prototype Frost slow without changing this enemy's authored base speed.</summary>
        public void ApplyTemporarySpeedMultiplier(float multiplier, float durationSeconds)
        {
            _temporarySpeedMultiplier = Mathf.Clamp(multiplier, 0.05f, 1f);
            _temporarySpeedExpiry = Time.time + Mathf.Max(0f, durationSeconds);
        }

        /// <summary>Refreshes temporary movement/status state. Called by the existing movement update and exposed for deterministic tests.</summary>
        public void RefreshTemporaryEffects(float now)
        {
            ExpireTemporaryModifiers(now);
        }

        void ExpireTemporaryModifiers(float now)
        {
            if (_temporarySpeedMultiplier != 1f && now >= _temporarySpeedExpiry)
            {
                _temporarySpeedMultiplier = 1f;
                _frostStatus ??= GetComponent<FrostStatus>();
                _frostStatus?.ClearIfExpired(now);
            }
        }
    }
}
