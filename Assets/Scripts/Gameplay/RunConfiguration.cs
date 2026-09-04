using System;
using UnityEngine;

namespace RealRail
{
    /// <summary>
    /// Runtime wave plan consumed by <see cref="WaveDirector"/>. It can be built by an
    /// authored RunDefinition or by a future encounter director without changing wave execution.
    /// </summary>
    public sealed class RunConfiguration
    {
        readonly WaveConfig[] _waves;

        public RunConfiguration(WaveConfig[] waves)
        {
            if (waves == null)
            {
                _waves = Array.Empty<WaveConfig>();
                return;
            }

            _waves = new WaveConfig[waves.Length];
            for (var index = 0; index < waves.Length; index++)
            {
                _waves[index] = CopyWave(waves[index]);
            }
        }

        public int WaveCount => _waves.Length;

        public bool IsValid => WaveCount > 0;

        public WaveConfig GetWave(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= _waves.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(waveIndex));
            }

            return CopyWave(_waves[waveIndex]);
        }

        static WaveConfig CopyWave(WaveConfig wave)
        {
            var triggers = wave.UpgradeTriggerKillCounts != null
                ? (int[])wave.UpgradeTriggerKillCounts.Clone()
                : Array.Empty<int>();
            return new WaveConfig(
                wave.KillGoal,
                wave.SpawnInterval,
                wave.MoveSpeed,
                triggers,
                wave.HeavySpawnChance,
                wave.MaxConcurrentEnemies);
        }
    }

    [CreateAssetMenu(fileName = "RunDefinition", menuName = "RealRail/Run Definition")]
    public sealed class RunDefinition : ScriptableObject
    {
        [SerializeField] WaveConfig[] waves = Array.Empty<WaveConfig>();

        public RunConfiguration CreateRuntimeConfiguration()
        {
            return new RunConfiguration(waves);
        }
    }
}
