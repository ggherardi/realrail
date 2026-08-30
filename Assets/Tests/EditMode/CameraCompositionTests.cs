using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealRail.Tests
{
    public sealed class CameraCompositionTests
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [Test]
        public void AuthoredCamera_FocusesTheArenaWhileKeepingThePlayerAndSpawnEdgeVisible()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var camera = FindRoot(scene, "Main Camera").GetComponent<Camera>();
            var player = FindRoot(scene, "Player");
            var lanes = FindRoot(scene, "Systems").GetComponentInChildren<LaneLayout>(true);

            Assert.NotNull(camera);
            Assert.NotNull(player);
            Assert.NotNull(lanes);
            Assert.AreEqual(new Vector3(0f, 8f, -8f), camera.transform.position);
            Assert.That(Mathf.DeltaAngle(25f, camera.transform.eulerAngles.x), Is.EqualTo(0f).Within(0.01f));
            Assert.AreEqual(45f, camera.fieldOfView);

            var playerViewport = camera.WorldToViewportPoint(player.transform.position);
            Assert.That(playerViewport.x, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(playerViewport.y, Is.InRange(0.14f, 0.4f));
            Assert.Greater(playerViewport.z, 0f);

            for (var lane = 0; lane < lanes.LaneCount; lane++)
            {
                var spawnViewport = camera.WorldToViewportPoint(lanes.GetSpawnPosition(lane));
                Assert.That(spawnViewport.x, Is.InRange(0f, 1f));
                Assert.That(spawnViewport.y, Is.InRange(0.7f, 1f));
                Assert.Greater(spawnViewport.z, 0f);
            }
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
