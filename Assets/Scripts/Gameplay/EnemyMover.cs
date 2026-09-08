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
        VolcanoPrototype _volcanoPrototype;
        LaneLayout _lanes;
        VolcanoObstacle _engagedObstacle;

        public float BaseSpeed => speed;
        public float EffectiveSpeed => speed * _temporarySpeedMultiplier;

        public event Action DestinationReached;

        public void Initialize(GameSession session, float laneX, float targetZ, float y, float movementSpeed = -1f,
            VolcanoPrototype volcanoPrototype = null, LaneLayout lanes = null)
        {
            _session = session;
            _laneX = laneX;
            _targetZ = targetZ;
            _y = y;
            _hasReachedDestination = false;
            _volcanoPrototype = volcanoPrototype;
            _lanes = lanes;
            _engagedObstacle = null;
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
            var desiredX = _laneX;
            var canAdvance = true;
            var obstacle = _volcanoPrototype != null ? _volcanoPrototype.ActiveObstacle : null;
            if (obstacle != null && obstacle.BlocksForwardPath(position, _targetZ))
            {
                if (obstacle.TryGetBypassX(position, GetLaneMin(), GetLaneMax(), out var bypassX))
                {
                    desiredX = bypassX;
                    // A mover that reaches the rock face before it has enough lateral
                    // clearance helps break it instead of ghosting through the collider.
                    if (obstacle.IsInEngagementRange(position) && Mathf.Abs(position.x - bypassX) > 0.12f)
                    {
                        canAdvance = false;
                        _engagedObstacle = obstacle;
                        obstacle.RegisterEngager(this);
                    }
                    else
                    {
                        ClearEngagement();
                    }
                }
                else if (obstacle.IsInEngagementRange(position))
                {
                    canAdvance = false;
                    _engagedObstacle = obstacle;
                    obstacle.RegisterEngager(this);
                }
            }
            else if (_engagedObstacle != null)
            {
                ClearEngagement();
            }

            position.x = Mathf.MoveTowards(position.x, desiredX, EffectiveSpeed * deltaTime);
            position.y = _y;
            if (canAdvance) position.z = Mathf.MoveTowards(position.z, _targetZ, EffectiveSpeed * deltaTime);
            transform.position = position;

            if (!_hasReachedDestination && Mathf.Approximately(position.z, _targetZ))
            {
                _hasReachedDestination = true;
                DestinationReached?.Invoke();
            }
        }

        void OnDestroy()
        {
            ClearEngagement();
        }

        void ClearEngagement()
        {
            if (_engagedObstacle != null) _engagedObstacle.UnregisterEngager(this);
            _engagedObstacle = null;
        }

        float GetLaneMin() => _lanes != null ? _laneX - _lanes.LaneWidth * 0.5f + 0.5f : _laneX - 2.25f;
        float GetLaneMax() => _lanes != null ? _laneX + _lanes.LaneWidth * 0.5f - 0.5f : _laneX + 2.25f;

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
