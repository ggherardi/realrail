using UnityEngine;

namespace RealRail
{
    public sealed class LaneLayout : MonoBehaviour
    {
        [SerializeField] float leftLaneX = -2.5f;
        [SerializeField] float rightLaneX = 2.5f;
        [SerializeField] float laneWidth = 4.5f;
        [SerializeField] float spawnEdgeInset = 0.55f;
        [SerializeField] float spawnZ = 24f;
        [SerializeField] float playerZ;
        [SerializeField] float strafeMinX = -4.5f;
        [SerializeField] float strafeMaxX = 4.5f;
        [SerializeField] float actorY = 1f;

        public float PlayerZ => playerZ;
        public float ActorY => actorY;
        public int LaneCount => 2;
        public float LaneWidth => laneWidth;

        public float GetLaneX(int laneIndex)
        {
            return laneIndex <= 0 ? leftLaneX : rightLaneX;
        }

        public Vector3 GetSpawnPosition(int laneIndex)
        {
            var minX = GetLaneX(laneIndex) - laneWidth * 0.5f + spawnEdgeInset;
            var maxX = GetLaneX(laneIndex) + laneWidth * 0.5f - spawnEdgeInset;
            return new Vector3(Random.Range(minX, maxX), actorY, spawnZ);
        }

        public float ClampStrafe(float x)
        {
            return Mathf.Clamp(x, strafeMinX, strafeMaxX);
        }
    }
}
