using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealRail
{
    /// <summary>Coordinates target rewards, player choice, and the temporary gameplay pause.</summary>
    public sealed class UpgradeRewardSelection : MonoBehaviour
    {
        [SerializeField] UpgradeSystem upgradeSystem;
        [SerializeField] UpgradeSelectionView selectionView;

        readonly Queue<byte> _pendingRewards = new Queue<byte>();
        List<UpgradeId> _activeChoices;
        float _timeScaleBeforeSelection;
        bool _pausedGameplay;

        public bool IsSelecting => _activeChoices != null;
        public int PendingRewardCount => _pendingRewards.Count;
        public event Action<IReadOnlyList<UpgradeId>> SelectionStarted;
        public event Action SelectionEnded;

        void Awake()
        {
            if (selectionView != null) selectionView.ChoiceSelected += Select;
        }

        void OnDestroy()
        {
            if (selectionView != null) selectionView.ChoiceSelected -= Select;
        }

        /// <summary>Registers one collected target. Simultaneous rewards are handled one selection at a time.</summary>
        public void RequestReward()
        {
            _pendingRewards.Enqueue(0);
            TryPresentNext();
        }

        public bool Select(UpgradeId upgrade)
        {
            if (!IsSelecting || !_activeChoices.Contains(upgrade)) return false;

            var selected = _activeChoices;
            _activeChoices = null;
            selectionView?.Hide();
            if (_pausedGameplay)
            {
                Time.timeScale = _timeScaleBeforeSelection;
                _pausedGameplay = false;
            }

            var applied = upgradeSystem != null && upgradeSystem.TryApplyLevel(upgrade, out _);
            SelectionEnded?.Invoke();
            TryPresentNext();
            return applied;
        }

        public void ConfigureForTests(UpgradeSystem system)
        {
            upgradeSystem = system;
        }

        void TryPresentNext()
        {
            if (IsSelecting || _pendingRewards.Count == 0) return;

            _pendingRewards.Dequeue();
            var candidates = upgradeSystem?.GenerateRewardCandidates() ?? new List<UpgradeId>();
            if (candidates.Count == 0)
            {
                TryPresentNext();
                return;
            }

            _activeChoices = candidates;
            _timeScaleBeforeSelection = Time.timeScale;
            _pausedGameplay = !Mathf.Approximately(Time.timeScale, 0f);
            if (_pausedGameplay) Time.timeScale = 0f;
            selectionView?.Show(candidates, upgradeSystem.State);
            SelectionStarted?.Invoke(candidates);
        }
    }
}
