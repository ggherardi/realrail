using UnityEngine;

namespace RealRail
{
    public sealed class AutoFire : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField] Transform muzzle;
        [SerializeField] GameObject projectilePrefab;
        [SerializeField] float fireInterval = 0.35f;
        [SerializeField] float doubleShotSeparation = 0.45f;
        [SerializeField] UpgradeSystem upgradeSystem;
        [SerializeField] RailgunPrototype railgunPrototype;
        [SerializeField] VolcanoPrototype volcanoPrototype;

        float _cooldown;
        public Vector3 MuzzlePosition => muzzle != null ? muzzle.position : transform.position;

        void Update()
        {
            if (session == null || !session.IsPlaying || projectilePrefab == null || muzzle == null)
            {
                return;
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f)
            {
                return;
            }

            if (volcanoPrototype != null && volcanoPrototype.TryConsumeScheduledShot(Time.time))
            {
                _cooldown = GetShotConfiguration().FireInterval;
                volcanoPrototype.FireShot(muzzle.position);
                return;
            }

            var shot = railgunPrototype != null && railgunPrototype.TryConsumeScheduledShot(Time.time)
                ? railgunPrototype.GetShotConfiguration()
                : GetShotConfiguration();
            _cooldown = shot.FireInterval;
            if (shot.IsRailgunPrototype)
            {
                FireProjectile(muzzle.position, shot);
                return;
            }
            if (shot.ProjectileCount == 2)
            {
                var offset = muzzle.right * (doubleShotSeparation * 0.5f);
                FireProjectile(muzzle.position - offset, shot);
                FireProjectile(muzzle.position + offset, shot);
                return;
            }

            FireProjectile(muzzle.position, shot);
        }

        public ShotConfiguration GetShotConfiguration()
        {
            return upgradeSystem != null ? upgradeSystem.GetShotConfiguration() : new ShotConfiguration(1, fireInterval, 1, 1);
        }

        /// <summary>Clears the previous run's firing cadence without changing weapon configuration.</summary>
        public void ResetFireCycle()
        {
            _cooldown = 0f;
        }

        /// <summary>Development-only immediate prototype shot; normal firing cadence is unchanged.</summary>
        public bool FireRailgunNow()
        {
            if (session == null || !session.IsPlaying || projectilePrefab == null || muzzle == null || railgunPrototype == null) return false;
            FireProjectile(muzzle.position, railgunPrototype.GetShotConfiguration());
            return true;
        }

        void FireProjectile(Vector3 position, ShotConfiguration shot)
        {
            var instance = Instantiate(projectilePrefab, position, Quaternion.identity);
            instance.SetActive(true);
            var projectile = instance.GetComponent<Projectile>();
            projectile.Initialize(session, shot.Damage, shot.DistinctHitCapacity, shot.IsRailgunPrototype, shot.ProjectileSpeed);
        }
    }
}
