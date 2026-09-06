using System;
using System.IO;
using UnityEngine;

namespace RealRail
{
    /// <summary>Persists one completed development experiment outside versioned project content.</summary>
    public static class ExperimentResultStorage
    {
        public static string DefaultDirectory => Path.Combine(Application.persistentDataPath, "RealRail", "SimulationResults");

        public static string Save(string json, string experimentId, string directory = null)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("Experiment JSON is required.", nameof(json));
            directory ??= DefaultDirectory;
            Directory.CreateDirectory(directory);
            var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
            var safeId = string.IsNullOrEmpty(experimentId) ? "experiment" : experimentId.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
            var path = Path.Combine(directory, safeId + "_" + stamp + ".json");
            File.WriteAllText(path, json);
            return path;
        }
    }
}
