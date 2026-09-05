using System;
using UnityEngine;

namespace RealRail
{
    /// <summary>
    /// A deliberately simple simulation controller. It observes ordinary scene actors and drives
    /// <see cref="PlayerMotor"/>; it has no knowledge of a wave director or its configuration.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerBot : MonoBehaviour
    {
        [SerializeField] bool botEnabled;
        [SerializeField, Min(0f)] float targetDeadZone = 0.1f;
        [SerializeField] PlayerMotor motor;
        [SerializeField] GameSession session;
        [SerializeField] UpgradeRewardSelection rewardSelection;
        [SerializeField] UpgradeSystem upgradeSystem;
        [SerializeField] BotProfileId profileId = BotProfileId.Average;

        bool _hasDecision;
        float _nextDecisionTime;

        public bool IsEnabled => botEnabled;
        public BotProfile Profile => BotProfile.FromId(profileId);

        void Awake()
        {
            motor ??= GetComponent<PlayerMotor>();
            session ??= FindAnyObjectByType<GameSession>();
            rewardSelection ??= FindAnyObjectByType<UpgradeRewardSelection>();
            upgradeSystem ??= FindAnyObjectByType<UpgradeSystem>();
        }

        void OnEnable()
        {
            if (rewardSelection != null)
            {
                rewardSelection.SelectionStarted += OnSelectionStarted;
            }
        }

        void OnDisable()
        {
            if (rewardSelection != null)
            {
                rewardSelection.SelectionStarted -= OnSelectionStarted;
            }
            motor?.ClearAutomatedInput();
        }

        void Update()
        {
            Tick();
        }

        /// <summary>Executes one observation and movement decision; simulation runners can call this explicitly.</summary>
        public void Tick()
        {
            if (!botEnabled || motor == null || (session != null && !session.IsPlaying))
            {
                if (!botEnabled) motor?.ClearAutomatedInput();
                return;
            }

            if (_hasDecision && Time.time < _nextDecisionTime)
            {
                return;
            }
            _hasDecision = true;
            _nextDecisionTime = Time.time + Profile.ReactionIntervalSeconds;

            var target = FindPriorityTarget();
            if (target == null)
            {
                motor.SetAutomatedInput(Vector2.zero);
                return;
            }

            var horizontalDistance = target.position.x - transform.position.x;
            motor.SetAutomatedInput(new Vector2(Mathf.Abs(horizontalDistance) <= targetDeadZone ? 0f : Mathf.Sign(horizontalDistance), 0f));
        }

        /// <summary>Allows a simulation runner to enable this controller without touching human input setup.</summary>
        public void SetBotEnabled(bool enabled)
        {
            botEnabled = enabled;
            _hasDecision = false;
            if (!enabled) motor?.ClearAutomatedInput();
        }

        /// <summary>Changes decision quality only; it does not alter any combat or session statistic.</summary>
        public void SetProfile(BotProfileId profile)
        {
            profileId = profile;
            _hasDecision = false;
        }

        public void ConfigureForTests(PlayerMotor playerMotor, GameSession gameSession, UpgradeRewardSelection selection = null, UpgradeSystem upgrades = null)
        {
            if (rewardSelection != null) rewardSelection.SelectionStarted -= OnSelectionStarted;
            motor = playerMotor;
            session = gameSession;
            rewardSelection = selection;
            upgradeSystem = upgrades;
            if (isActiveAndEnabled && rewardSelection != null) rewardSelection.SelectionStarted += OnSelectionStarted;
        }

        Transform FindPriorityTarget()
        {
            // Upgrade targets are time-sensitive rewards, so they take priority over normal threats.
            var upgradeTargets = FindObjectsByType<UpgradeTarget>(FindObjectsInactive.Exclude);
            if (Profile.PrioritizesUpgradeTargets)
            {
                var priority = FindTarget(upgradeTargets, target => target.transform);
                if (priority != null) return priority;
            }

            var movers = FindObjectsByType<EnemyMover>(FindObjectsInactive.Exclude);
            return FindTarget(movers, mover => mover.GetComponent<UpgradeTarget>() == null ? mover.transform : null);
        }

        Transform FindTarget<T>(T[] candidates, Func<T, Transform> transformOf) where T : Component
        {
            Transform closest = null;
            var bestScore = float.PositiveInfinity;
            foreach (var candidate in candidates)
            {
                var candidateTransform = transformOf(candidate);
                if (candidateTransform == null || candidateTransform.position.z < transform.position.z)
                {
                    continue;
                }

                var relative = candidateTransform.position - transform.position;
                // Average reacts to the nearby object it notices first; stronger profiles focus the
                // earliest threat to reach the defense line, with lateral distance as a stable tie-breaker.
                var score = Profile.UsesUrgentThreatTargeting
                    ? candidateTransform.position.z + Mathf.Abs(relative.x) * 0.01f
                    : relative.sqrMagnitude;
                if (score < bestScore)
                {
                    closest = candidateTransform;
                    bestScore = score;
                }
            }
            return closest;
        }

        void OnSelectionStarted(System.Collections.Generic.IReadOnlyList<UpgradeId> choices)
        {
            if (!botEnabled || choices == null || choices.Count == 0)
            {
                return;
            }

            // Selection remains authoritative: this calls the same public path as the UI buttons.
            rewardSelection.Select(ChooseUpgradeForCurrentProfile(choices));
        }

        /// <summary>Returns one offered upgrade using only the currently visible choices and current build state.</summary>
        public UpgradeId ChooseUpgradeForCurrentProfile(System.Collections.Generic.IReadOnlyList<UpgradeId> choices)
        {
            if (choices == null || choices.Count == 0) return default;

            var preferred = Profile.Id == BotProfileId.Average
                ? new[] { UpgradeId.RapidFire, UpgradeId.PowerShot, UpgradeId.DoubleShot, UpgradeId.PiercingShot }
                : Profile.Id == BotProfileId.Strong
                    ? new[] { UpgradeId.PowerShot, UpgradeId.DoubleShot, UpgradeId.RapidFire, UpgradeId.PiercingShot }
                    : new[] { UpgradeId.DoubleShot, UpgradeId.PowerShot, UpgradeId.RapidFire, UpgradeId.PiercingShot };
            UpgradeId best = choices[0];
            var bestScore = int.MinValue;
            foreach (var choice in choices)
            {
                var preference = Array.IndexOf(preferred, choice);
                var currentLevel = upgradeSystem != null ? upgradeSystem.State.GetLevel(choice) : 0;
                // Stronger profiles deliberately avoid repeatedly selecting an already-developed option.
                var score = -preference * 10;
                if (Profile.Id != BotProfileId.Average) score -= currentLevel * 3;
                if (score > bestScore)
                {
                    best = choice;
                    bestScore = score;
                }
            }
            return best;
        }
    }
}
