using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealRail.Tests
{
    public sealed class FrozenOwnedAssetPrototypeTests
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [Test]
        public void FrozenPrototype_UsesOwnedToonSkylineWithoutDecorativeColliders()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var frozen = FindRoot(scene, "VisualEnvironment").transform.Find("Frozen");

            Assert.NotNull(frozen);
            Assert.NotNull(frozen.Find("ToonSkyline/HeroCliff_SnowOverlay"));
            Assert.NotNull(frozen.Find("ToonSkyline/SecondaryCliff_Right"));
            Assert.NotNull(frozen.Find("ToonSparseVegetation/Pine_Left"));
            Assert.IsNull(frozen.Find("DistantMountains"));
            Assert.Zero(frozen.GetComponentsInChildren<Collider>(true).Length);
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }
    }
}
