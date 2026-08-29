using System.Linq;
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
            AssertAssigned(spawner, "heavyEnemyPrefab");
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
            var enemyCollider = enemy.GetComponent<CapsuleCollider>();
            AssertPrefabPhysics(enemy, enemyCollider);
            Assert.AreEqual(1, enemy.GetComponents<Collider>().Length);
            Assert.AreEqual(0.5f, enemyCollider.radius);
            Assert.AreEqual(1f, enemyCollider.height);
            Assert.AreEqual(Vector3.zero, enemyCollider.center);
            Assert.AreEqual(1, enemyCollider.direction);
            Assert.NotNull(enemy.GetComponent<Health>());
            Assert.NotNull(enemy.GetComponent<DestroyWhenDead>());
            Assert.NotNull(enemy.GetComponent<EnemyMover>());
            Assert.NotNull(enemy.GetComponent<EnemyDefenseLine>());
            Assert.NotNull(enemy.GetComponent<WaveEnemy>());
            Assert.AreEqual(1, Property(enemy.GetComponent<Health>(), "maxHealth").intValue);
            Assert.AreEqual(4f, Property(enemy.GetComponent<EnemyMover>(), "speed").floatValue);
            AssertVisualChild(enemy);
            AssertEnemyGruntVisualAnimation(enemy);

            var heavy = LoadPrefab("Assets/Prefabs/Enemy_Heavy.prefab");
            AssertEnemyHeavyVariant(enemy, heavy);
            AssertEnemyHeavyVisualAnimation(heavy);

            var projectile = LoadPrefab("Assets/Prefabs/Projectile.prefab");
            Assert.AreEqual(LayerMask.NameToLayer(GameplayLayers.Projectile), projectile.layer);
            AssertPrefabPhysics(projectile, projectile.GetComponent<BoxCollider>());
            var projectileComponent = projectile.GetComponent<Projectile>();
            Assert.NotNull(projectileComponent);
            Assert.AreEqual(22f, Property(projectileComponent, "speed").floatValue);
            Assert.AreEqual(1, Property(projectileComponent, "damage").intValue);
            Assert.AreEqual(45f, Property(projectileComponent, "maxZ").floatValue);
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
            Assert.AreEqual(36f, layout.GetSpawnPosition(0).z);
            Assert.AreEqual(0f, layout.DefenseLineZ);
            Assert.AreEqual(-4.5f, layout.ClampStrafe(-100f));
            Assert.AreEqual(4.5f, layout.ClampStrafe(100f));

            var environment = FindRoot("Environment");
            var divider = environment.transform.Find("Divider");
            Assert.NotNull(divider);
            Assert.AreEqual(LayerMask.NameToLayer(GameplayLayers.Divider), divider.gameObject.layer);
            Assert.AreEqual(new Vector3(0f, 0.75f, 21f), divider.position);
            Assert.AreEqual(new Vector3(0.4f, 1.5f, 36f), divider.localScale);
            Assert.NotNull(divider.GetComponent<BoxCollider>());
        }

        [Test]
        public void Scene_UsesExplicitIncreasingHeavyWaveComposition()
        {
            var director = FindRoot("Systems").GetComponentInChildren<WaveDirector>(true);
            var waves = Property(director, "waves");

            Assert.AreEqual(3, waves.arraySize);
            Assert.AreEqual(0f, waves.GetArrayElementAtIndex(0).FindPropertyRelative("HeavySpawnChance").floatValue);
            Assert.AreEqual(0.15f, waves.GetArrayElementAtIndex(1).FindPropertyRelative("HeavySpawnChance").floatValue);
            Assert.AreEqual(0.30f, waves.GetArrayElementAtIndex(2).FindPropertyRelative("HeavySpawnChance").floatValue);
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
            Assert.NotNull(visual.GetComponentInChildren<Renderer>(true));
            Assert.IsEmpty(visual.GetComponentsInChildren<Collider>(true));
            Assert.IsEmpty(visual.GetComponentsInChildren<Rigidbody>(true));
            Assert.IsEmpty(visual.GetComponentsInChildren<MonoBehaviour>(true));
        }

        static void AssertEnemyGruntVisualAnimation(GameObject enemy)
        {
            Assert.IsNull(enemy.GetComponent<Animator>());

            var visual = enemy.transform.Find("Visual");
            var animator = visual.GetComponentInChildren<Animator>(true);
            Assert.NotNull(animator);
            Assert.NotNull(animator.runtimeAnimatorController);
            Assert.IsFalse(animator.applyRootMotion);

            var importer = AssetImporter.GetAtPath("Assets/Art/Enemies/Enemy_Grunt/Enemy_Grunt.fbx") as ModelImporter;
            Assert.NotNull(importer);
            Assert.IsTrue(importer.importAnimation);
            Assert.AreEqual(ModelImporterAnimationType.Generic, importer.animationType);

            var walk = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Enemies/Enemy_Grunt/Enemy_Grunt.fbx")
                .OfType<AnimationClip>()
                .Single(clip => clip.name == "Walk");
            Assert.IsTrue(walk.isLooping);
            Assert.Contains(walk, animator.runtimeAnimatorController.animationClips);
        }

        static void AssertEnemyHeavyVariant(GameObject enemy, GameObject heavy)
        {
            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(heavy));
            Assert.AreSame(enemy, PrefabUtility.GetCorrespondingObjectFromSource(heavy));
            Assert.AreEqual(enemy.layer, heavy.layer);

            CollectionAssert.AreEquivalent(
                enemy.GetComponents<Component>().Select(component => component.GetType()),
                heavy.GetComponents<Component>().Select(component => component.GetType()));

            var collider = heavy.GetComponent<CapsuleCollider>();
            AssertPrefabPhysics(heavy, collider);
            Assert.AreEqual(1, heavy.GetComponents<Collider>().Length);
            Assert.AreEqual(0.5f, collider.radius);
            Assert.AreEqual(1f, collider.height);
            Assert.AreEqual(Vector3.zero, collider.center);
            Assert.AreEqual(1, collider.direction);
            Assert.AreEqual(4, Property(heavy.GetComponent<Health>(), "maxHealth").intValue);
            Assert.AreEqual(3f, Property(heavy.GetComponent<EnemyMover>(), "speed").floatValue);

            AssertVisualChild(heavy);
        }

        static void AssertEnemyHeavyVisualAnimation(GameObject heavy)
        {
            const string heavyFbxPath = "Assets/Art/Enemies/Enemy_Heavy/Enemy_Heavy.fbx";
            const string rootBoneName = "HV_Root";

            Assert.IsNull(heavy.GetComponent<Animator>());
            var visual = heavy.transform.Find("Visual");
            var animators = heavy.GetComponentsInChildren<Animator>(true);
            Assert.AreEqual(1, animators.Length);
            var animator = animators.Single();
            Assert.IsTrue(animator.transform.IsChildOf(visual));
            Assert.NotNull(animator.runtimeAnimatorController);
            Assert.IsFalse(animator.applyRootMotion);

            var importer = AssetImporter.GetAtPath(heavyFbxPath) as ModelImporter;
            Assert.NotNull(importer);
            Assert.IsTrue(importer.importAnimation);
            Assert.AreEqual(ModelImporterAnimationType.Generic, importer.animationType);
            Assert.IsEmpty(importer.motionNodeName);

            var clips = AssetDatabase.LoadAllAssetsAtPath(heavyFbxPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__"))
                .ToArray();
            Assert.AreEqual(1, clips.Length, "Enemy Heavy must import exactly one intended animation clip.");
            var walk = clips.Single();
            Assert.AreEqual("Walk", walk.name);
            Assert.IsTrue(walk.isLooping);
            CollectionAssert.AreEquivalent(new[] { walk }, animator.runtimeAnimatorController.animationClips);

            AssertRootHasNoAnimatedMotion(walk, rootBoneName);
        }

        static void AssertRootHasNoAnimatedMotion(AnimationClip clip, string rootBoneName)
        {
            var rootTransformBindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.path.Split('/').LastOrDefault() == rootBoneName)
                .Where(binding => binding.propertyName.StartsWith("m_LocalPosition") ||
                                  binding.propertyName.StartsWith("m_LocalRotation") ||
                                  binding.propertyName.StartsWith("localEulerAngles"));

            foreach (var binding in rootTransformBindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                Assert.NotNull(curve, $"Missing root curve for {binding.propertyName}");
                Assert.Greater(curve.length, 0, $"Empty root curve for {binding.propertyName}");

                var initialValue = curve.keys[0].value;
                Assert.IsTrue(
                    curve.keys.All(key => Mathf.Approximately(initialValue, key.value)),
                    $"{rootBoneName}.{binding.propertyName} changes over the Walk clip and would animate root motion.");
            }
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
