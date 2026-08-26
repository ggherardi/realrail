namespace RealRail
{
    public sealed class WaveProgress
    {
        public WaveProgress(int killGoal)
        {
            KillGoal = killGoal;
        }

        public int KillGoal { get; }
        public int KillCount { get; private set; }
        public int ActiveEnemyCount { get; private set; }
        public bool KillGoalReached => KillCount >= KillGoal;
        public bool IsComplete => KillGoalReached && ActiveEnemyCount == 0;

        public void RegisterSpawned()
        {
            ActiveEnemyCount++;
        }

        public void RegisterResolved(WaveEnemyResolution resolution)
        {
            if (ActiveEnemyCount <= 0)
            {
                return;
            }

            ActiveEnemyCount--;
            if (resolution == WaveEnemyResolution.Killed && !KillGoalReached)
            {
                KillCount++;
            }
        }
    }
}
