using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealRail.Tests
{
    public sealed class FrozenOwnedAssetPrototypeTests
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [Test]
        public void FrozenBaseline_RemovesRejectedSkylineAndPrototypeSnowOverlay()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var frozen = FindRoot(scene, "VisualEnvironment").transform.Find("Frozen");

            Assert.NotNull(frozen);
            Assert.IsNull(frozen.Find("GroundContinuation"));
            Assert.IsNull(FindDescendant(frozen, "SnowApproach"));
            Assert.IsNull(FindDescendant(frozen, "IceRiver"));
            Assert.IsNull(frozen.Find("ToonSkyline"));
            Assert.IsNull(frozen.Find("ToonMidgroundRocks"));
            Assert.IsNull(frozen.Find("ToonSparseVegetation"));
            Assert.IsNull(frozen.Find("DistantMountains"));
            Assert.IsNull(FindDescendant(frozen, "HeroCliff_SnowOverlay"));
            Assert.IsNull(FindDescendant(frozen, "SecondaryCliff_Right"));
            Assert.IsNull(FindDescendant(frozen, "MountainLarge_Single"));
            Assert.IsNull(FindDescendant(frozen, "Mountain_Group_1"));
            Assert.IsNull(FindDescendant(frozen, "Mountain_Group_2"));
            Assert.IsNull(FindDescendant(frozen, "SnowOverlay"));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FrozenHeroSnowOverlay.mat"));
            Assert.Zero(frozen.GetComponentsInChildren<Collider>(true).Length);
            Assert.IsEmpty(frozen.GetComponentsInChildren<Renderer>(true));
        }

        static Transform FindDescendant(Transform parent, string name)
        {
            foreach (var child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
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
