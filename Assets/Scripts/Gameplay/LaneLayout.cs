using UnityEngine;

namespace RealRail
{
    public sealed class LaneLayout : MonoBehaviour
    {
        [SerializeField] float leftLaneX = -2.5f;
        [SerializeField] float rightLaneX = 2.5f;
        [SerializeField] float spawnZ = 24f;
        [SerializeField] float playerZ;
        [SerializeField] float strafeMinX = -3.5f;
        [SerializeField] float strafeMaxX = 3.5f;
        [SerializeField] float actorY = 1f;

        public float PlayerZ => playerZ;
        public float ActorY => actorY;
        public int LaneCount => 2;

        public float GetLaneX(int laneIndex)
        {
            return laneIndex <= 0 ? leftLaneX : rightLaneX;
        }

        public Vector3 GetSpawnPosition(int laneIndex)
        {
            return new Vector3(GetLaneX(laneIndex), actorY, spawnZ);
        }

        public float ClampStrafe(float x)
        {
            return Mathf.Clamp(x, strafeMinX, strafeMaxX);
        }
    }
}
