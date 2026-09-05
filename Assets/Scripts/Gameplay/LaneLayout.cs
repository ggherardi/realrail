using UnityEngine;

namespace RealRail
{
    public sealed class LaneLayout : MonoBehaviour
    {
        [SerializeField] float leftLaneX = -3.25f;
        [SerializeField] float rightLaneX = 3.25f;
        [SerializeField] float laneWidth = 5.5f;
        [SerializeField] float spawnEdgeInset = 0.55f;
        [SerializeField] float spawnZ = 36f;
        [SerializeField] float playerZ;
        [SerializeField] float defenseLineZ;
        [SerializeField] float strafeMinX = -5.75f;
        [SerializeField] float strafeMaxX = 5.75f;
        [SerializeField] float actorY = 1f;

        public float PlayerZ => playerZ;
        public float DefenseLineZ => defenseLineZ;
        public float ActorY => actorY;
        public int LaneCount => 2;
        public float LaneWidth => laneWidth;

        public float GetLaneX(int laneIndex)
        {
            return laneIndex <= 0 ? leftLaneX : rightLaneX;
        }

        public Vector3 GetSpawnPosition(int laneIndex)
        {
            return GetSpawnPosition(laneIndex, UnityRunRandom.Shared);
        }

        /// <summary>Uses the caller's run stream when a deterministic run is active.</summary>
        public Vector3 GetSpawnPosition(int laneIndex, IRunRandom random)
        {
            var minX = GetLaneX(laneIndex) - laneWidth * 0.5f + spawnEdgeInset;
            var maxX = GetLaneX(laneIndex) + laneWidth * 0.5f - spawnEdgeInset;
            return new Vector3((random ?? UnityRunRandom.Shared).NextFloat(minX, maxX), actorY, spawnZ);
        }

        public float ClampStrafe(float x)
        {
            return Mathf.Clamp(x, strafeMinX, strafeMaxX);
        }
    }
}
