using UnityEngine;

namespace RealRail
{
    public sealed class AutoFire : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField] Transform muzzle;
        [SerializeField] GameObject projectilePrefab;
        [SerializeField] float fireInterval = 0.35f;

        int _enemyLayer;
        float _cooldown;

        public void Bind(GameSession gameSession, Transform muzzleTransform, GameObject prefab, int enemyLayer)
        {
            session = gameSession;
            muzzle = muzzleTransform;
            projectilePrefab = prefab;
            _enemyLayer = enemyLayer;
        }

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
            var instance = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
            instance.SetActive(true);
            var projectile = instance.GetComponent<Projectile>();
            projectile.Initialize(session, _enemyLayer);
        }
    }
}
