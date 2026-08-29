using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RealRail.Tests
{
    public sealed class HeavyEnemyHealthIndicatorTests
    {
        const string EnemyPrefabPath = "Assets/Prefabs/Enemy.prefab";
        const string HeavyPrefabPath = "Assets/Prefabs/Enemy_Heavy.prefab";

        [Test]
        public void HeavyPrefab_HasAnIndicatorBoundToItsOwnHealth()
        {
            var heavy = LoadPrefab(HeavyPrefabPath);
            var indicator = heavy.GetComponentInChildren<EnemyHealthIndicator>(true);

            Assert.NotNull(indicator);
            Assert.AreEqual("HealthIndicator", indicator.gameObject.name);
            Assert.AreSame(heavy.GetComponent<Health>(), indicator.HealthSource);
            Assert.NotNull(indicator.GetComponent<Canvas>());
            Assert.AreEqual(RenderMode.WorldSpace, indicator.GetComponent<Canvas>().renderMode);
            Assert.NotNull(indicator.transform.Find("Bar/Fill").GetComponent<Image>());
            Assert.NotNull(indicator.transform.Find("HP Text").GetComponent<Text>());
        }

        [Test]
        public void GruntPrefab_HasNoHealthIndicator()
        {
            Assert.IsNull(LoadPrefab(EnemyPrefabPath).GetComponentInChildren<EnemyHealthIndicator>(true));
        }

        [Test]
        public void Indicator_InitializesAndUpdatesFromTheBoundHealth()
        {
            var host = CreateHeavy("First Heavy");
            try
            {
                var health = host.GetComponent<Health>();
                var indicator = host.GetComponentInChildren<EnemyHealthIndicator>();
                indicator.Bind(health);
                health.SetMaxHealth(4);

                Assert.AreEqual(4, indicator.DisplayedCurrent);
                Assert.AreEqual(4, indicator.DisplayedMax);
                health.TakeDamage(1);
                Assert.AreEqual(3, indicator.DisplayedCurrent);
                Assert.AreEqual(4, indicator.DisplayedMax);
                health.TakeDamage(3);
                Assert.AreEqual(0, indicator.DisplayedCurrent);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MultipleHeavyIndicators_KeepTheirOwnHealthPresentation()
        {
            var first = CreateHeavy("First Heavy");
            var second = CreateHeavy("Second Heavy");
            try
            {
                first.GetComponentInChildren<EnemyHealthIndicator>().Bind(first.GetComponent<Health>());
                second.GetComponentInChildren<EnemyHealthIndicator>().Bind(second.GetComponent<Health>());
                first.GetComponent<Health>().SetMaxHealth(4);
                second.GetComponent<Health>().SetMaxHealth(4);
                first.GetComponent<Health>().TakeDamage(1);
                second.GetComponent<Health>().TakeDamage(3);

                var firstIndicator = first.GetComponentInChildren<EnemyHealthIndicator>();
                var secondIndicator = second.GetComponentInChildren<EnemyHealthIndicator>();
                Assert.AreEqual(3, firstIndicator.DisplayedCurrent);
                Assert.AreEqual(1, secondIndicator.DisplayedCurrent);
                Assert.AreNotSame(firstIndicator.HealthSource, secondIndicator.HealthSource);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        static GameObject CreateHeavy(string name)
        {
            var heavy = Object.Instantiate(LoadPrefab(HeavyPrefabPath));
            heavy.name = name;
            return heavy;
        }

        static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.NotNull(prefab, path);
            return prefab;
        }
    }
}
