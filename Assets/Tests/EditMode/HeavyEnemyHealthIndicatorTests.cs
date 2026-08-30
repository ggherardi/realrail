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
            Assert.AreEqual(Quaternion.identity, indicator.transform.localRotation);
            Assert.AreEqual(Vector3.one * 0.0135f, indicator.transform.localScale);
            Assert.AreEqual(3.1f, indicator.GetComponent<RectTransform>().anchoredPosition.y);
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
                AssertFillFraction(indicator, 1f);
                health.TakeDamage(1);
                Assert.AreEqual(3, indicator.DisplayedCurrent);
                Assert.AreEqual(4, indicator.DisplayedMax);
                AssertFillFraction(indicator, 0.75f);
                health.TakeDamage(1);
                Assert.AreEqual(2, indicator.DisplayedCurrent);
                AssertFillFraction(indicator, 0.5f);
                health.TakeDamage(1);
                Assert.AreEqual(1, indicator.DisplayedCurrent);
                AssertFillFraction(indicator, 0.25f);
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
                AssertFillFraction(firstIndicator, 0.75f);
                AssertFillFraction(secondIndicator, 0.25f);
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

        static void AssertFillFraction(EnemyHealthIndicator indicator, float expectedFraction)
        {
            var bar = indicator.transform.Find("Bar").GetComponent<RectTransform>();
            var fill = indicator.transform.Find("Bar/Fill").GetComponent<Image>();
            var expectedWidth = (bar.rect.width - 4f) * expectedFraction;

            Assert.AreEqual(expectedFraction, indicator.DisplayedFillFraction, 0.0001f);
            Assert.AreEqual(expectedFraction, fill.fillAmount, 0.0001f);
            Assert.AreEqual(expectedWidth, fill.rectTransform.rect.width, 0.0001f);
        }

        static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.NotNull(prefab, path);
            return prefab;
        }
    }
}
