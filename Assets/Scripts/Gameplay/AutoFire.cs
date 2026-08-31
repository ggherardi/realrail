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

        float _cooldown;

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

            var shot = GetShotConfiguration();
            _cooldown = shot.FireInterval;
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

        void FireProjectile(Vector3 position, ShotConfiguration shot)
        {
            var instance = Instantiate(projectilePrefab, position, Quaternion.identity);
            instance.SetActive(true);
            var projectile = instance.GetComponent<Projectile>();
            projectile.Initialize(session, shot.Damage, shot.DistinctHitCapacity);
        }
    }
}
