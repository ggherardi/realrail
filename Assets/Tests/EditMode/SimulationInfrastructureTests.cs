using NUnit.Framework;
using UnityEngine;

namespace RealRail.Tests
{
    public sealed class SimulationInfrastructureTests
    {
        GameObject _owner;

        [TearDown]
        public void TearDown()
        {
            if (_owner != null) Object.DestroyImmediate(_owner);
        }

        [Test]
        public void SeededStreams_ReproduceValuesAndDoNotCrossPerturbSubsystems()
        {
            var first = new RunRandomContext(12345);
            var second = new RunRandomContext(12345);
            var firstEnemies = first.CreateStream("enemy-spawn");
            var secondEnemies = second.CreateStream("enemy-spawn");
            var firstRewards = first.CreateStream("upgrade-rewards");
            var secondRewards = second.CreateStream("upgrade-rewards");

            Assert.AreEqual(firstEnemies.Next(2), secondEnemies.Next(2));
            firstRewards.Next(4);
            firstRewards.Next(4);
            Assert.AreEqual(firstEnemies.NextFloat(-2f, 2f), secondEnemies.NextFloat(-2f, 2f));
            Assert.AreEqual(secondRewards.Next(4), new RunRandomContext(12345).CreateStream("upgrade-rewards").Next(4));
            Assert.AreNotEqual(RunRandomContext.SeedForRun(12345, 0), RunRandomContext.SeedForRun(12345, 1));
        }

        [Test]
        public void Scheduler_RunsOnlyOneUniqueJobAtATimeAndMapsResultSeed()
        {
            var scheduler = CreateScheduler();
            var one = scheduler.Enqueue(new SimulationJob("one", 11));
            var two = scheduler.Enqueue(new SimulationJob("two", 22));
            scheduler.TickScheduler();
            Assert.AreSame(one, scheduler.Active);

            scheduler.CompleteActive(Result(11));
            scheduler.TickScheduler();
            Assert.AreSame(two, scheduler.Active);
            scheduler.CompleteActive(Result(22));

            Assert.AreEqual(SimulationJobState.Succeeded, one.State);
            Assert.AreEqual(11, one.Result.RunSeed);
            Assert.AreEqual(SimulationJobState.Succeeded, two.State);
            Assert.AreEqual(22, two.Result.RunSeed);
        }

        [Test]
        public void Scheduler_RecordsFailureAndWorkerPlanIsHonestAboutUnityConcurrency()
        {
            var scheduler = CreateScheduler();
            scheduler.SetRequestedWorkerCount(4);
            var job = scheduler.Enqueue(new SimulationJob("bad", 4));
            scheduler.TickScheduler();
            scheduler.FailActive("scene missing");

            Assert.AreEqual(SimulationJobState.Failed, job.State);
            Assert.AreEqual("scene missing", job.Failure);
            Assert.AreEqual(4, scheduler.WorkerPlan.RequestedWorkerCount);
            Assert.AreEqual(1, scheduler.WorkerPlan.InProcessWorkerCount);
            Assert.IsTrue(scheduler.WorkerPlan.RequiresSeparateProcesses);
        }

        [Test]
        public void JsonReport_ContainsMachineReadableStateSeedAndWallClockMetrics()
        {
            var scheduler = CreateScheduler();
            var job = scheduler.Enqueue(new SimulationJob("json-job", 789));
            scheduler.TickScheduler();
            scheduler.CompleteActive(Result(789));
            var json = SimulationJsonReport.Serialize(job);

            StringAssert.Contains("\"id\":\"json-job\"", json);
            StringAssert.Contains("\"seed\":789", json);
            StringAssert.Contains("\"state\":\"Succeeded\"", json);
            StringAssert.Contains("wallClockMilliseconds", json);
        }

        SimulationJobScheduler CreateScheduler()
        {
            _owner = new GameObject("Scheduler");
            return _owner.AddComponent<SimulationJobScheduler>();
        }

        static RunResult Result(int seed) => new RunResult(SessionState.Victory, 2f, 1, 4, 0, 0,
            System.Array.Empty<AcquiredUpgrade>(), seed);
    }
}
