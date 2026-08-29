using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RealRail.Tests
{
    public sealed class EnemySpawnerTests
    {
        GameObject _sessionOwner;
        GameObject _player;
        GameObject _lanesOwner;
        GameObject _spawnerOwner;
        GameSession _session;
        EnemySpawner _spawner;

        [SetUp]
        public void SetUp()
        {
            _sessionOwner = new GameObject("Session");
            _session = _sessionOwner.AddComponent<GameSession>();
            _player = new GameObject("Player");
            _player.AddComponent<Health>().SetMaxHealth(3);
            _session.BindPlayer(_player.GetComponent<Health>());

            _lanesOwner = new GameObject("Lanes");
            var lanes = _lanesOwner.AddComponent<LaneLayout>();
            _spawnerOwner = new GameObject("Spawner");
            _spawner = _spawnerOwner.AddComponent<EnemySpawner>();

            var serializedSpawner = new SerializedObject(_spawner);
            serializedSpawner.FindProperty("session").objectReferenceValue = _session;
            serializedSpawner.FindProperty("lanes").objectReferenceValue = lanes;
            serializedSpawner.FindProperty("enemyPrefab").objectReferenceValue = LoadPrefab("Assets/Prefabs/Enemy.prefab");
            serializedSpawner.FindProperty("heavyEnemyPrefab").objectReferenceValue = LoadPrefab("Assets/Prefabs/Enemy_Heavy.prefab");
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_spawnerOwner);
            Object.DestroyImmediate(_lanesOwner);
            Object.DestroyImmediate(_player);
            Object.DestroyImmediate(_sessionOwner);
        }

        [Test]
        public void HeavyChance_SelectsAndInstantiatesTheHeavyPrefabThroughSpawner()
        {
            WaveEnemy spawned = null;
            _spawner.EnemySpawned += enemy => spawned = enemy;
            _spawner.BeginWave(new WaveConfig(1, 1f, 4f, heavySpawnChance: 1f));

            typeof(EnemySpawner)
                .GetMethod("Spawn", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_spawner, null);

            Assert.NotNull(spawned);
            Assert.AreEqual(4, spawned.GetComponent<Health>().Max);
            Assert.AreEqual(3f, spawned.GetComponent<EnemyMover>().BaseSpeed);

            Object.DestroyImmediate(spawned.gameObject);
        }

        static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.NotNull(prefab, path);
            return prefab;
        }
    }
}
