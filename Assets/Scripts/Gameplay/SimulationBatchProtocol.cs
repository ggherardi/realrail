using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealRail
{
    /// <summary>Immutable-on-dispatch JSON request for one independent Unity worker process.</summary>
    [Serializable]
    public sealed class SimulationBatchRequest
    {
        public string jobId;
        public string experimentId;
        public string candidateId;
        public int seed;
        public BotProfileId profile;
        public float simulationSpeed;
        public float timeoutSeconds;
        public SimulationBatchWave[] waves;

        public SimulationBatchRequest() { }

        public SimulationBatchRequest(string jobId, int seed, BotProfileId profile, float simulationSpeed, float timeoutSeconds, RunConfiguration configuration, string experimentId = null, string candidateId = null)
        {
            this.jobId = string.IsNullOrWhiteSpace(jobId) ? Guid.NewGuid().ToString("N") : jobId;
            this.seed = seed; this.profile = profile; this.simulationSpeed = Mathf.Max(0.01f, simulationSpeed); this.timeoutSeconds = Mathf.Max(0f, timeoutSeconds);
            this.experimentId = experimentId; this.candidateId = candidateId; waves = SimulationBatchWave.FromConfiguration(configuration);
        }

        public bool IsValid(out string failure)
        {
            if (string.IsNullOrWhiteSpace(jobId)) { failure = "jobId is required."; return false; }
            if (simulationSpeed <= 0f) { failure = "simulationSpeed must be greater than zero."; return false; }
            if (waves == null || waves.Length == 0) { failure = "At least one wave is required."; return false; }
            foreach (var wave in waves)
            {
                if (wave == null) { failure = "Wave cannot be null."; return false; }
                if (!wave.IsValid(out failure)) return false;
            }
            failure = null; return true;
        }

        public RunConfiguration CreateConfiguration()
        {
            var source = waves ?? Array.Empty<SimulationBatchWave>(); var copy = new WaveConfig[source.Length];
            for (var index = 0; index < copy.Length; index++) copy[index] = source[index].ToWaveConfig();
            return new RunConfiguration(copy);
        }
        public string ToJson(bool prettyPrint = false) => JsonUtility.ToJson(this, prettyPrint);
        public static SimulationBatchRequest FromJson(string json) => JsonUtility.FromJson<SimulationBatchRequest>(json);
    }

    [Serializable]
    public sealed class SimulationBatchWave
    {
        public int killGoal; public float spawnInterval; public float moveSpeed; public float heavySpawnChance; public int maxConcurrentEnemies; public int[] upgradeTriggerKillCounts;
        public bool IsValid(out string failure)
        {
            if (killGoal < 1) { failure = "killGoal must be at least one."; return false; }
            if (spawnInterval <= 0f) { failure = "spawnInterval must be greater than zero."; return false; }
            if (moveSpeed < 0f || heavySpawnChance < 0f || heavySpawnChance > 1f || maxConcurrentEnemies < 0) { failure = "Wave values are out of range."; return false; }
            failure = null; return true;
        }
        public WaveConfig ToWaveConfig() => new WaveConfig(killGoal, spawnInterval, moveSpeed, upgradeTriggerKillCounts != null ? (int[])upgradeTriggerKillCounts.Clone() : Array.Empty<int>(), heavySpawnChance, maxConcurrentEnemies);
        public static SimulationBatchWave[] FromConfiguration(RunConfiguration configuration)
        {
            var source = configuration != null ? configuration.Waves : Array.Empty<WaveConfig>(); var copy = new SimulationBatchWave[source.Length];
            for (var index = 0; index < copy.Length; index++) { var wave = source[index]; copy[index] = new SimulationBatchWave { killGoal = wave.KillGoal, spawnInterval = wave.SpawnInterval, moveSpeed = wave.MoveSpeed, heavySpawnChance = wave.HeavySpawnChance, maxConcurrentEnemies = wave.MaxConcurrentEnemies, upgradeTriggerKillCounts = wave.UpgradeTriggerKillCounts != null ? (int[])wave.UpgradeTriggerKillCounts.Clone() : Array.Empty<int>() }; }
            return copy;
        }
    }

    /// <summary>Terminal worker output with authoritative run diagnostics and process timing.</summary>
    [Serializable]
    public sealed class SimulationBatchResult
    {
        public string jobId; public string experimentId; public string candidateId; public BotProfileId profile; public int seed; public string state; public string failure;
        public int workerProcessId; public string startedUtc; public string completedUtc; public double wallClockMilliseconds;
        public float durationSeconds; public int finalWaveReached; public bool victory; public int enemiesKilled; public int enemiesLeaked; public int playerDamageTaken;
        public string[] upgradeOffers; public string[] upgradeSelections; public SimulationBatchUpgrade[] finalBuild;
        public static SimulationBatchResult Succeeded(string jobId, int seed, RunResult run, double milliseconds)
        {
            var upgrades = run != null ? run.Upgrades : Array.Empty<AcquiredUpgrade>(); var build = new SimulationBatchUpgrade[upgrades.Count];
            for (var index = 0; index < build.Length; index++) build[index] = new SimulationBatchUpgrade { upgrade = upgrades[index].Upgrade.ToString(), level = upgrades[index].Level };
            return new SimulationBatchResult { jobId = jobId, seed = seed, state = SimulationJobState.Succeeded.ToString(), wallClockMilliseconds = milliseconds, durationSeconds = run != null ? run.DurationSeconds : 0f, finalWaveReached = run != null ? run.FinalWaveReached : 0, victory = run != null && run.IsVictory, enemiesKilled = run != null ? run.EnemiesKilled : 0, enemiesLeaked = run != null ? run.EnemiesLeaked : 0, playerDamageTaken = run != null ? run.PlayerDamageTaken : 0, upgradeOffers = Copy(run != null ? run.UpgradeOffers : null), upgradeSelections = Names(run != null ? run.UpgradeSelections : null), finalBuild = build };
        }
        public static SimulationBatchResult Failed(string jobId, int seed, string failure, double milliseconds) => new SimulationBatchResult { jobId = jobId, seed = seed, state = SimulationJobState.Failed.ToString(), failure = failure, wallClockMilliseconds = milliseconds, upgradeOffers = Array.Empty<string>(), upgradeSelections = Array.Empty<string>(), finalBuild = Array.Empty<SimulationBatchUpgrade>() };
        public string ToJson(bool prettyPrint = false) => JsonUtility.ToJson(this, prettyPrint);
        public static SimulationBatchResult FromJson(string json) => JsonUtility.FromJson<SimulationBatchResult>(json);
        static string[] Copy(IReadOnlyList<string> values) { if (values == null) return Array.Empty<string>(); var copy = new string[values.Count]; for (var index = 0; index < copy.Length; index++) copy[index] = values[index]; return copy; }
        static string[] Names(IReadOnlyList<UpgradeId> values) { if (values == null) return Array.Empty<string>(); var copy = new string[values.Count]; for (var index = 0; index < copy.Length; index++) copy[index] = values[index].ToString(); return copy; }
    }
    [Serializable] public sealed class SimulationBatchUpgrade { public string upgrade; public int level; }
}
