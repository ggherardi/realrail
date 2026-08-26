using UnityEngine;
using UnityEngine.UI;

namespace RealRail
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameplayInstaller : MonoBehaviour
    {
        void Awake()
        {
            ConfigureCollisionMatrix();

            var lanes = gameObject.AddComponent<LaneLayout>();
            var session = gameObject.AddComponent<GameSession>();
            var spawner = gameObject.AddComponent<EnemySpawner>();
            var waveDirector = gameObject.AddComponent<WaveDirector>();

            CreateEnvironment(lanes);
            var player = CreatePlayer(lanes, session);
            var projectileTemplate = CreateProjectileTemplate();
            var enemyTemplate = CreateEnemyTemplate();
            var upgradeTargetTemplate = CreateUpgradeTargetTemplate();

            var autoFire = player.GetComponent<AutoFire>();
            autoFire.Bind(
                session,
                player.transform.Find("Muzzle"),
                projectileTemplate,
                LayerMask.NameToLayer(GameplayLayers.Enemy),
                LayerMask.NameToLayer(GameplayLayers.Divider));

            spawner.Bind(session, lanes, enemyTemplate, LayerMask.NameToLayer(GameplayLayers.Player));

            session.BindPlayer(player.GetComponent<Health>());
            CreateHud(player.GetComponent<Health>(), session);
            waveDirector.Bind(session, spawner, lanes, autoFire, upgradeTargetTemplate);
            waveDirector.StartRun();
            PositionCamera();
        }

        static void ConfigureCollisionMatrix()
        {
            var player = LayerMask.NameToLayer(GameplayLayers.Player);
            var enemy = LayerMask.NameToLayer(GameplayLayers.Enemy);
            var projectile = LayerMask.NameToLayer(GameplayLayers.Projectile);
            var divider = LayerMask.NameToLayer(GameplayLayers.Divider);
            if (player < 0 || enemy < 0 || projectile < 0 || divider < 0)
            {
                Debug.LogError("Player, Enemy, Projectile, and Divider layers must exist in Tags and Layers.");
                return;
            }

            for (var layer = 0; layer < 32; layer++)
            {
                Physics.IgnoreLayerCollision(player, layer, layer != enemy);
                Physics.IgnoreLayerCollision(projectile, layer, layer != enemy && layer != divider);
                Physics.IgnoreLayerCollision(enemy, layer, layer != player && layer != projectile);
                Physics.IgnoreLayerCollision(divider, layer, layer != projectile);
            }
        }

        static void CreateEnvironment(LaneLayout lanes)
        {
            var root = new GameObject("Environment");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(root.transform, false);
            ground.transform.position = new Vector3(0f, 0f, 12f);
            ground.transform.localScale = new Vector3(10f, 0.2f, 32f);
            ApplyColor(ground, new Color(0.18f, 0.2f, 0.22f));
            Destroy(ground.GetComponent<Collider>());

            CreateLaneStrip(root.transform, lanes.GetLaneX(0), lanes.LaneWidth, new Color(0.25f, 0.45f, 0.7f));
            CreateLaneStrip(root.transform, lanes.GetLaneX(1), lanes.LaneWidth, new Color(0.7f, 0.35f, 0.25f));
            CreateDivider(root.transform);
        }

        static void CreateLaneStrip(Transform parent, float x, float width, Color color)
        {
            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = $"Lane_{x}";
            strip.transform.SetParent(parent, false);
            strip.transform.position = new Vector3(x, 0.12f, 12f);
            strip.transform.localScale = new Vector3(width, 0.02f, 32f);
            ApplyColor(strip, color);
            Destroy(strip.GetComponent<Collider>());
        }

        static void CreateDivider(Transform parent)
        {
            var divider = GameObject.CreatePrimitive(PrimitiveType.Cube);
            divider.name = "Divider";
            divider.layer = LayerMask.NameToLayer(GameplayLayers.Divider);
            divider.transform.SetParent(parent, false);
            divider.transform.position = new Vector3(0f, 0.75f, 15f);
            divider.transform.localScale = new Vector3(0.4f, 1.5f, 24f);
            ApplyColor(divider, new Color(0.1f, 0.12f, 0.14f));
        }

        static GameObject CreatePlayer(LaneLayout lanes, GameSession session)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.layer = LayerMask.NameToLayer(GameplayLayers.Player);
            player.transform.position = new Vector3(0f, lanes.ActorY, lanes.PlayerZ);
            ApplyColor(player, new Color(0.3f, 0.8f, 0.45f));

            var collider = player.GetComponent<CapsuleCollider>();
            collider.isTrigger = true;

            var body = player.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var health = player.AddComponent<Health>();
            health.SetMaxHealth(3);

            var motor = player.AddComponent<PlayerMotor>();
            motor.Bind(lanes, session);
            player.AddComponent<AutoFire>();

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(player.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0f, 0.8f);

            return player;
        }

        static GameObject CreateProjectileTemplate()
        {
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            projectile.name = "Projectile";
            projectile.layer = LayerMask.NameToLayer(GameplayLayers.Projectile);
            projectile.transform.localScale = new Vector3(0.15f, 0.15f, 0.4f);
            ApplyColor(projectile, new Color(1f, 0.9f, 0.2f));

            var collider = projectile.GetComponent<BoxCollider>();
            collider.isTrigger = true;

            var body = projectile.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            projectile.AddComponent<Projectile>();
            projectile.SetActive(false);
            return projectile;
        }

        static GameObject CreateEnemyTemplate()
        {
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "Enemy";
            enemy.layer = LayerMask.NameToLayer(GameplayLayers.Enemy);
            ApplyColor(enemy, new Color(0.85f, 0.25f, 0.25f));

            var collider = enemy.GetComponent<CapsuleCollider>();
            collider.isTrigger = true;

            var body = enemy.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var health = enemy.AddComponent<Health>();
            health.SetMaxHealth(1);
            enemy.AddComponent<DestroyWhenDead>();
            enemy.AddComponent<EnemyMover>();
            enemy.AddComponent<EnemyContactDamage>();
            enemy.AddComponent<WaveEnemy>();
            enemy.SetActive(false);
            return enemy;
        }

        static GameObject CreateUpgradeTargetTemplate()
        {
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "UpgradeTarget";
            target.layer = LayerMask.NameToLayer(GameplayLayers.Enemy);
            target.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            ApplyColor(target, new Color(0.35f, 0.9f, 1f));

            var collider = target.GetComponent<BoxCollider>();
            collider.isTrigger = true;

            var body = target.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            target.AddComponent<Health>();
            target.AddComponent<DestroyWhenDead>();
            target.AddComponent<EnemyMover>();
            target.AddComponent<UpgradeTarget>();
            target.SetActive(false);
            return target;
        }

        static void CreateHud(Health playerHealth, GameSession session)
        {
            var canvasObject = new GameObject("Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var hpText = CreateLabel(canvasObject.transform, "HpText", new Vector2(16f, -16f), TextAnchor.UpperLeft, 28);
            var gameOverText = CreateLabel(canvasObject.transform, "GameOverText", Vector2.zero, TextAnchor.MiddleCenter, 48);
            gameOverText.text = "Game Over";
            gameOverText.alignment = TextAnchor.MiddleCenter;
            var victoryText = CreateLabel(canvasObject.transform, "VictoryText", Vector2.zero, TextAnchor.MiddleCenter, 48);
            victoryText.text = "Victory";
            victoryText.alignment = TextAnchor.MiddleCenter;

            var hud = canvasObject.AddComponent<HudView>();
            hud.Bind(hpText, gameOverText, victoryText, playerHealth, session);
        }

        static Text CreateLabel(Transform parent, string name, Vector2 anchoredPosition, TextAnchor anchor, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor == TextAnchor.MiddleCenter ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = anchor == TextAnchor.MiddleCenter ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(480f, 80f);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = anchor;
            text.raycastTarget = false;
            return text;
        }

        static void PositionCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var transform = camera.transform;
            transform.position = new Vector3(0f, 9f, -11f);
            transform.rotation = Quaternion.Euler(28f, 0f, 0f);
        }

        static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return;
            }

            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            renderer.material = material;
        }
    }
}
