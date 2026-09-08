using System;
using UnityEngine;

namespace RealRail
{
    /// <summary>Experimental Fire + Explosion prototype. It owns one temporary world object and deliberately has no upgrade/fusion acquisition dependency.</summary>
    public sealed class VolcanoPrototype : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField] LaneLayout lanes;
        [SerializeField] bool enabledForDebug;
        [SerializeField, Min(2f)] float cadenceSeconds = 8f;
        [SerializeField, Min(1f)] float spawnZ = 18f;
        [SerializeField] int laneIndex = 0;
        [SerializeField] float laneOffset = 0.45f;
        [SerializeField, Min(1f)] float width = 3.1f;
        [SerializeField, Min(0.5f)] float depth = 1.8f;
        [SerializeField, Min(1)] int hitPoints = 42;
        [SerializeField, Min(0.1f)] float lifetimeSeconds = 12f;
        [SerializeField, Min(1)] int volcanicDamage = 1;
        [SerializeField, Min(0.1f)] float damageIntervalSeconds = 1f;

        float _nextSpawnTime;
        public VolcanoObstacle ActiveObstacle { get; private set; }
        public bool IsEnabled => enabledForDebug;
        public event Action<bool> Changed;

        void Update()
        {
            if (!enabledForDebug || ActiveObstacle != null || session == null || !session.IsPlaying || Time.time < _nextSpawnTime) return;
            TrySpawnNow();
        }

        public void SetEnabled(bool enabled)
        {
            if (enabledForDebug == enabled) return;
            enabledForDebug = enabled;
            _nextSpawnTime = Time.time + cadenceSeconds;
            Changed?.Invoke(enabled);
        }

        public bool TrySpawnNow()
        {
            if (ActiveObstacle != null || session == null || !session.IsPlaying || lanes == null) return false;
            var centerX = lanes.GetLaneX(laneIndex) + laneOffset;
            var laneMin = lanes.GetLaneX(laneIndex) - lanes.LaneWidth * 0.5f + 0.5f;
            var laneMax = lanes.GetLaneX(laneIndex) + lanes.LaneWidth * 0.5f - 0.5f;
            var root = new GameObject("Volcano Formation (Prototype)");
            root.transform.position = new Vector3(centerX, lanes.ActorY, Mathf.Clamp(spawnZ, lanes.DefenseLineZ + 4f, 34f));
            root.layer = 0;
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(width, 2.3f, depth);
            collider.center = new Vector3(0f, 0.65f, 0f);
            root.AddComponent<Health>();
            var obstacle = root.AddComponent<VolcanoObstacle>();
            root.AddComponent<DestroyWhenDead>();
            CreateGreyboxVisual(root.transform, width, depth);
            obstacle.Configure(width, depth, lanes.GetLaneX(laneIndex), laneMin, laneMax, hitPoints, volcanicDamage, damageIntervalSeconds);
            obstacle.Health.Died += OnObstacleDied;
            ActiveObstacle = obstacle;
            _nextSpawnTime = Time.time + cadenceSeconds;
            Destroy(root, lifetimeSeconds);
            return true;
        }

        public void ConfigureForTests(float obstacleWidth, float obstacleDepth, int health, int damage, float interval)
        {
            width = Mathf.Max(1f, obstacleWidth);
            depth = Mathf.Max(0.5f, obstacleDepth);
            hitPoints = Mathf.Max(1, health);
            volcanicDamage = Mathf.Max(1, damage);
            damageIntervalSeconds = Mathf.Max(0.1f, interval);
        }

        void OnObstacleDied()
        {
            if (ActiveObstacle != null) ActiveObstacle.Health.Died -= OnObstacleDied;
            ActiveObstacle = null;
        }

        void OnDestroy()
        {
            if (ActiveObstacle != null) ActiveObstacle.Health.Died -= OnObstacleDied;
        }

        static void CreateGreyboxVisual(Transform root, float obstacleWidth, float obstacleDepth)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rock.name = "Hot Lava Rock";
            rock.transform.SetParent(root, false);
            rock.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            rock.transform.localScale = new Vector3(obstacleWidth * 0.52f, 0.65f, obstacleDepth * 0.62f);
            UnityEngine.Object.Destroy(rock.GetComponent<Collider>());
            var renderer = rock.GetComponent<Renderer>();
            renderer.material.color = new Color(0.55f, 0.09f, 0.015f);
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", new Color(1f, 0.12f, 0.01f) * 1.5f);
        }
    }
}
