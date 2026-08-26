using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealRail.Tests
{
    public sealed class AuthoredGameplayAssetsTests
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        Scene _scene;

        [OneTimeSetUp]
        public void OpenGameplayScene()
        {
            _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [Test]
        public void Scene_HasOneOfEachRequiredGameplaySystem_WithAssignedReferences()
        {
            var systems = FindRoot("Systems");
            Assert.NotNull(systems);
            Assert.AreEqual(1, systems.GetComponentsInChildren<GameSession>(true).Length);
            Assert.AreEqual(1, systems.GetComponentsInChildren<WaveDirector>(true).Length);
            Assert.AreEqual(1, systems.GetComponentsInChildren<EnemySpawner>(true).Length);
            Assert.AreEqual(1, systems.GetComponentsInChildren<LaneLayout>(true).Length);

            var session = systems.GetComponentInChildren<GameSession>(true);
            var director = systems.GetComponentInChildren<WaveDirector>(true);
            var spawner = systems.GetComponentInChildren<EnemySpawner>(true);
            AssertAssigned(session, "playerHealth");
            AssertAssigned(director, "session");
            AssertAssigned(director, "spawner");
            AssertAssigned(director, "lanes");
            AssertAssigned(director, "autoFire");
            AssertAssigned(director, "upgradeTargetPrefab");
            AssertAssigned(spawner, "session");
            AssertAssigned(spawner, "lanes");
            AssertAssigned(spawner, "enemyPrefab");
        }

        [Test]
        public void Player_HasAssignedMuzzleAndPreservedCombatValues()
        {
            var player = FindRoot("Player");
            Assert.NotNull(player);
            Assert.AreEqual(LayerMask.NameToLayer(GameplayLayers.Player), player.layer);
            Assert.NotNull(player.transform.Find("Muzzle"));

            var autoFire = player.GetComponent<AutoFire>();
            Assert.NotNull(autoFire);
            AssertAssigned(autoFire, "session");
            AssertAssigned(autoFire, "muzzle");
            AssertAssigned(autoFire, "projectilePrefab");
            Assert.AreEqual(0.35f, Property(autoFire, "fireInterval").floatValue);
            Assert.AreEqual(0.45f, Property(autoFire, "doubleShotSeparation").floatValue);
            Assert.AreEqual(3, Property(player.GetComponent<Health>(), "maxHealth").intValue);
        }

        [Test]
        public void Prefabs_HaveRequiredCollisionAndGameplayConfiguration()
        {
            var enemy = LoadPrefab("Assets/Prefabs/Enemy.prefab");
            Assert.AreEqual(LayerMask.NameToLayer(GameplayLayers.Enemy), enemy.layer);
            AssertPrefabPhysics(enemy, enemy.GetComponent<CapsuleCollider>());
            Assert.NotNull(enemy.GetComponent<Health>());
            Assert.NotNull(enemy.GetComponent<DestroyWhenDead>());
            Assert.NotNull(enemy.GetComponent<EnemyMover>());
            Assert.NotNull(enemy.GetComponent<EnemyContactDamage>());
            Assert.NotNull(enemy.GetComponent<WaveEnemy>());
            Assert.AreEqual(1, Property(enemy.GetComponent<Health>(), "maxHealth").intValue);
            Assert.AreEqual(1, Property(enemy.GetComponent<EnemyContactDamage>(), "damage").intValue);
            AssertVisualChild(enemy);

            var projectile = LoadPrefab("Assets/Prefabs/Projectile.prefab");
            Assert.AreEqual(LayerMask.NameToLayer(GameplayLayers.Projectile), projectile.layer);
            AssertPrefabPhysics(projectile, projectile.GetComponent<BoxCollider>());
            var projectileComponent = projectile.GetComponent<Projectile>();
            Assert.NotNull(projectileComponent);
            Assert.AreEqual(22f, Property(projectileComponent, "speed").floatValue);
            Assert.AreEqual(1, Property(projectileComponent, "damage").intValue);
            Assert.AreEqual(30f, Property(projectileComponent, "maxZ").floatValue);
            Assert.AreEqual(1 << LayerMask.NameToLayer(GameplayLayers.Enemy), Property(projectileComponent, "enemyLayers").intValue);
            Assert.AreEqual(1 << LayerMask.NameToLayer(GameplayLayers.Divider), Property(projectileComponent, "dividerLayers").intValue);
            AssertVisualChild(projectile);

            var target = LoadPrefab("Assets/Prefabs/UpgradeTarget.prefab");
            Assert.AreEqual(LayerMask.NameToLayer(GameplayLayers.Enemy), target.layer);
            AssertPrefabPhysics(target, target.GetComponent<BoxCollider>());
            Assert.NotNull(target.GetComponent<DestroyWhenDead>());
            Assert.NotNull(target.GetComponent<EnemyMover>());
            Assert.NotNull(target.GetComponent<UpgradeTarget>());
            Assert.AreEqual(3, Property(target.GetComponent<Health>(), "maxHealth").intValue);
            Assert.AreEqual(3, Property(target.GetComponent<UpgradeTarget>(), "startingHealth").intValue);
            AssertVisualChild(target);
        }

        [Test]
        public void Scene_PreservesLaneAndDividerAuthoring()
        {
            var layout = FindRoot("Systems").GetComponentInChildren<LaneLayout>(true);
            Assert.AreEqual(-2.5f, layout.GetLaneX(0));
            Assert.AreEqual(2.5f, layout.GetLaneX(1));
            Assert.AreEqual(4.5f, layout.LaneWidth);
            Assert.AreEqual(24f, layout.GetSpawnPosition(0).z);
            Assert.AreEqual(-4.5f, layout.ClampStrafe(-100f));
            Assert.AreEqual(4.5f, layout.ClampStrafe(100f));

            var environment = FindRoot("Environment");
            var divider = environment.transform.Find("Divider");
            Assert.NotNull(divider);
            Assert.AreEqual(LayerMask.NameToLayer(GameplayLayers.Divider), divider.gameObject.layer);
            Assert.AreEqual(new Vector3(0f, 0.75f, 15f), divider.position);
            Assert.AreEqual(new Vector3(0.4f, 1.5f, 24f), divider.localScale);
            Assert.NotNull(divider.GetComponent<BoxCollider>());
        }

        [Test]
        public void PhysicsMatrix_MatchesTheFormerRuntimePolicy()
        {
            var player = LayerMask.NameToLayer(GameplayLayers.Player);
            var enemy = LayerMask.NameToLayer(GameplayLayers.Enemy);
            var projectile = LayerMask.NameToLayer(GameplayLayers.Projectile);
            var divider = LayerMask.NameToLayer(GameplayLayers.Divider);

            Assert.IsFalse(Physics.GetIgnoreLayerCollision(player, enemy));
            Assert.IsFalse(Physics.GetIgnoreLayerCollision(enemy, projectile));
            Assert.IsFalse(Physics.GetIgnoreLayerCollision(projectile, divider));
            Assert.IsTrue(Physics.GetIgnoreLayerCollision(player, projectile));
            Assert.IsTrue(Physics.GetIgnoreLayerCollision(player, divider));
            Assert.IsTrue(Physics.GetIgnoreLayerCollision(enemy, divider));
            Assert.IsTrue(Physics.GetIgnoreLayerCollision(enemy, enemy));
        }

        GameObject FindRoot(string name)
        {
            foreach (var root in _scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.NotNull(prefab, path);
            return prefab;
        }

        static void AssertPrefabPhysics(GameObject prefab, Collider collider)
        {
            Assert.NotNull(collider);
            Assert.IsTrue(collider.isTrigger);
            var body = prefab.GetComponent<Rigidbody>();
            Assert.NotNull(body);
            Assert.IsTrue(body.isKinematic);
            Assert.IsFalse(body.useGravity);
            Assert.AreEqual(CollisionDetectionMode.ContinuousSpeculative, body.collisionDetectionMode);
        }

        static void AssertVisualChild(GameObject prefab)
        {
            var visual = prefab.transform.Find("Visual");
            Assert.NotNull(visual);
            Assert.NotNull(visual.GetComponent<Renderer>());
            Assert.IsNull(visual.GetComponent<Collider>());
        }

        static void AssertAssigned(Object target, string propertyName)
        {
            Assert.NotNull(Property(target, propertyName).objectReferenceValue, $"{target.name}.{propertyName}");
        }

        static SerializedProperty Property(Object target, string propertyName)
        {
            var property = new SerializedObject(target).FindProperty(propertyName);
            Assert.NotNull(property, $"Missing serialized property {propertyName} on {target.name}");
            return property;
        }
    }
}
