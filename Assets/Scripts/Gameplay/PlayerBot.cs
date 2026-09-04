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

        public bool IsEnabled => botEnabled;

        void Awake()
        {
            motor ??= GetComponent<PlayerMotor>();
            session ??= FindFirstObjectByType<GameSession>();
            rewardSelection ??= FindFirstObjectByType<UpgradeRewardSelection>();
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
            if (!enabled) motor?.ClearAutomatedInput();
        }

        public void ConfigureForTests(PlayerMotor playerMotor, GameSession gameSession, UpgradeRewardSelection selection = null)
        {
            if (rewardSelection != null) rewardSelection.SelectionStarted -= OnSelectionStarted;
            motor = playerMotor;
            session = gameSession;
            rewardSelection = selection;
            if (isActiveAndEnabled && rewardSelection != null) rewardSelection.SelectionStarted += OnSelectionStarted;
        }

        Transform FindPriorityTarget()
        {
            // Upgrade targets are time-sensitive rewards, so they take priority over normal threats.
            var upgradeTargets = FindObjectsByType<UpgradeTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var priority = FindClosestToPlayer(upgradeTargets, target => target.transform);
            if (priority != null) return priority;

            var movers = FindObjectsByType<EnemyMover>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            return FindClosestToPlayer(movers, mover => mover.GetComponent<UpgradeTarget>() == null ? mover.transform : null);
        }

        Transform FindClosestToPlayer<T>(T[] candidates, Func<T, Transform> transformOf) where T : Component
        {
            Transform closest = null;
            var closestZ = float.PositiveInfinity;
            foreach (var candidate in candidates)
            {
                var candidateTransform = transformOf(candidate);
                if (candidateTransform == null || candidateTransform.position.z < transform.position.z)
                {
                    continue;
                }

                if (candidateTransform.position.z < closestZ)
                {
                    closest = candidateTransform;
                    closestZ = candidateTransform.position.z;
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
            rewardSelection.Select(ChooseUpgrade(choices));
        }

        static UpgradeId ChooseUpgrade(System.Collections.Generic.IReadOnlyList<UpgradeId> choices)
        {
            // A stable, intentionally modest policy provides a seam for future skill profiles.
            foreach (var preferred in new[] { UpgradeId.PowerShot, UpgradeId.RapidFire, UpgradeId.PiercingShot, UpgradeId.DoubleShot })
            {
                foreach (var choice in choices)
                {
                    if (choice == preferred) return choice;
                }
            }
            return choices[0];
        }
    }
}
