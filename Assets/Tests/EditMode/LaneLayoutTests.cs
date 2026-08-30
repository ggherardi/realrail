using NUnit.Framework;
using UnityEngine;

namespace RealRail.Tests
{
    public sealed class LaneLayoutTests
    {
        GameObject _owner;
        LaneLayout _lanes;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("LaneLayoutHost");
            _lanes = _owner.AddComponent<LaneLayout>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
        }

        [Test]
        public void SpawnPositions_StayWithinTheirLaneRanges()
        {
            for (var sample = 0; sample < 100; sample++)
            {
                var left = _lanes.GetSpawnPosition(0);
                var right = _lanes.GetSpawnPosition(1);

                Assert.That(left.x, Is.InRange(-5.45f, -1.05f));
                Assert.That(right.x, Is.InRange(1.05f, 5.45f));
                Assert.AreEqual(_lanes.ActorY, left.y);
                Assert.AreEqual(36f, left.z);
                Assert.AreEqual(36f, right.z);
            }
        }

        [Test]
        public void ClampStrafe_UsesExpandedCorridorBounds()
        {
            Assert.AreEqual(-5.75f, _lanes.ClampStrafe(-10f));
            Assert.AreEqual(5.75f, _lanes.ClampStrafe(10f));
        }

        [Test]
        public void DefenseLine_IsIndependentFromLaneAndPlayerPosition()
        {
            Assert.AreEqual(0f, _lanes.DefenseLineZ);
            Assert.AreEqual(0f, _lanes.PlayerZ);
        }
    }
}
