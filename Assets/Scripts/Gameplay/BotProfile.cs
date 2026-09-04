namespace RealRail
{
    /// <summary>Named decision policies for <see cref="PlayerBot"/>. These are simulation skill labels, not human difficulty settings.</summary>
    public enum BotProfileId
    {
        Average,
        Strong,
        PerfectIsh
    }

    /// <summary>Compact, immutable tuning for the bot's observation and decision policy.</summary>
    public readonly struct BotProfile
    {
        public BotProfile(BotProfileId id, string label, float reactionIntervalSeconds, bool prioritizesUpgradeTargets, bool usesUrgentThreatTargeting)
        {
            Id = id;
            Label = label;
            ReactionIntervalSeconds = reactionIntervalSeconds;
            PrioritizesUpgradeTargets = prioritizesUpgradeTargets;
            UsesUrgentThreatTargeting = usesUrgentThreatTargeting;
        }

        public BotProfileId Id { get; }
        public string Label { get; }
        /// <summary>Scaled gameplay seconds between fresh observations.</summary>
        public float ReactionIntervalSeconds { get; }
        public bool PrioritizesUpgradeTargets { get; }
        public bool UsesUrgentThreatTargeting { get; }

        public static BotProfile FromId(BotProfileId id)
        {
            switch (id)
            {
                case BotProfileId.Strong:
                    return new BotProfile(id, "Strong", 0.12f, true, true);
                case BotProfileId.PerfectIsh:
                    return new BotProfile(id, "Perfect-ish", 0.03f, true, true);
                default:
                    return new BotProfile(BotProfileId.Average, "Average", 0.4f, true, false);
            }
        }
    }
}
