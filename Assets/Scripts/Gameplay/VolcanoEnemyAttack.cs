using UnityEngine;

namespace RealRail
{
    /// <summary>Small prototype distinction: Heavies naturally wear down the formation twice as quickly.</summary>
    public sealed class VolcanoEnemyAttack : MonoBehaviour
    {
        [SerializeField, Min(1)] int damagePerHit = 1;
        public int DamagePerHit => damagePerHit;
    }
}
