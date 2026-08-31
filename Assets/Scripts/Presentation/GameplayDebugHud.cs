using UnityEngine;
using UnityEngine.UI;

namespace RealRail
{
    /// <summary>Compact development HUD displaying the authoritative effective weapon configuration.</summary>
    public sealed class GameplayDebugHud : MonoBehaviour
    {
        [SerializeField] Text displayText;
        [SerializeField] UpgradeSystem upgradeSystem;
        [SerializeField] GameSession session;

        string _feedback;

        public bool IsVisible => gameObject.activeSelf;

        void Awake()
        {
            gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (upgradeSystem != null) upgradeSystem.UpgradesChanged += Refresh;
            if (session != null) session.GodModeChanged += OnGodModeChanged;
            Refresh();
        }

        void OnDisable()
        {
            if (upgradeSystem != null) upgradeSystem.UpgradesChanged -= Refresh;
            if (session != null) session.GodModeChanged -= OnGodModeChanged;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (visible) Refresh();
        }

        public void ShowFeedback(string message)
        {
            _feedback = message;
            Refresh();
        }

        void OnGodModeChanged(bool _) => Refresh();

        void Refresh()
        {
            if (displayText == null || upgradeSystem == null)
            {
                return;
            }

            var shot = upgradeSystem.GetShotConfiguration();
            var state = upgradeSystem.State;
            displayText.text =
                "WEAPON DEBUG\n\n" +
                $"Projectiles: {shot.ProjectileCount}\n" +
                $"Fire Interval: {shot.FireInterval:0.00} s\n" +
                $"Damage: {shot.Damage}\n" +
                $"Pierce: {shot.DistinctHitCapacity} targets\n\n" +
                "UPGRADES\n" +
                FormatUpgrade(state, UpgradeId.DoubleShot) + "\n" +
                FormatUpgrade(state, UpgradeId.RapidFire) + "\n" +
                FormatUpgrade(state, UpgradeId.PiercingShot) + "\n" +
                FormatUpgrade(state, UpgradeId.PowerShot) + "\n\n" +
                $"GOD MODE: {(session != null && session.GodMode ? "ON" : "OFF")}" +
                (string.IsNullOrEmpty(_feedback) ? string.Empty : $"\n\n{_feedback}");
        }

        static string FormatUpgrade(UpgradeState state, UpgradeId upgrade) =>
            $"{GameplayDebugController.DisplayName(upgrade)}: {ToRoman(state.GetLevel(upgrade))} / {ToRoman(state.GetMaxLevel(upgrade))}";

        static string ToRoman(int level) => level switch
        {
            0 => "0",
            1 => "I",
            2 => "II",
            3 => "III",
            _ => level.ToString()
        };
    }
}
