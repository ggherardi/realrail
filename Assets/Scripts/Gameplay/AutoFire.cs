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

        float _cooldown;

        public bool HasDoubleShot { get; private set; }

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

            _cooldown = fireInterval;
            if (HasDoubleShot)
            {
                var offset = muzzle.right * (doubleShotSeparation * 0.5f);
                FireProjectile(muzzle.position - offset);
                FireProjectile(muzzle.position + offset);
                return;
            }

            FireProjectile(muzzle.position);
        }

        public void EnableDoubleShot()
        {
            HasDoubleShot = true;
        }

        void FireProjectile(Vector3 position)
        {
            var instance = Instantiate(projectilePrefab, position, Quaternion.identity);
            instance.SetActive(true);
            var projectile = instance.GetComponent<Projectile>();
            projectile.Initialize(session);
        }
    }
}
