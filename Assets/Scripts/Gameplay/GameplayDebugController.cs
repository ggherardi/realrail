using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RealRail
{
    /// <summary>Central development-only keyboard controls for deterministic gameplay testing.</summary>
    public sealed class GameplayDebugController : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField] UpgradeSystem upgradeSystem;
        [SerializeField] GameplayDebugHud debugHud;
        [SerializeField] UpgradeRewardSelection upgradeRewardSelection;

        public event Action<string> Feedback;

        void Awake()
        {
            debugHud?.SetVisible(false);
        }

        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame) ToggleHud();
            if (keyboard.f2Key.wasPressedThisFrame) ToggleGodMode();
            if (upgradeRewardSelection != null && upgradeRewardSelection.IsSelecting) return;
            if (keyboard.digit1Key.wasPressedThisFrame) GrantUpgrade(UpgradeId.DoubleShot);
            if (keyboard.digit2Key.wasPressedThisFrame) GrantUpgrade(UpgradeId.RapidFire);
            if (keyboard.digit3Key.wasPressedThisFrame) GrantUpgrade(UpgradeId.PiercingShot);
            if (keyboard.digit4Key.wasPressedThisFrame) GrantUpgrade(UpgradeId.PowerShot);
            if (keyboard.rKey.wasPressedThisFrame) ResetUpgrades();
#endif
        }

        public void ToggleHud()
        {
            if (debugHud == null)
            {
                return;
            }

            debugHud.SetVisible(!debugHud.IsVisible);
        }

        public void ToggleGodMode()
        {
            if (session == null)
            {
                return;
            }

            session.SetGodMode(!session.GodMode);
            Report($"God Mode {(session.GodMode ? "ON" : "OFF")}");
        }

        public bool GrantUpgrade(UpgradeId upgrade)
        {
            if (upgradeSystem == null)
            {
                return false;
            }

            if (upgradeSystem.TryApplyLevel(upgrade, out var application))
            {
                Report($"Granted {DisplayName(upgrade)} {ToRoman(application.Level)}");
                return true;
            }

            Report($"{DisplayName(upgrade)} already at max level");
            return false;
        }

        public void ResetUpgrades()
        {
            if (upgradeSystem == null)
            {
                return;
            }

            upgradeSystem.ResetUpgrades();
            Report("Upgrades reset");
        }

        void Report(string message)
        {
            debugHud?.ShowFeedback(message);
            Feedback?.Invoke(message);
        }

        internal static string DisplayName(UpgradeId upgrade) => upgrade switch
        {
            UpgradeId.DoubleShot => "Double Shot",
            UpgradeId.RapidFire => "Rapid Fire",
            UpgradeId.PiercingShot => "Piercing Shot",
            UpgradeId.PowerShot => "Power Shot",
            _ => upgrade.ToString()
        };

        static string ToRoman(int level) => level switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            _ => level.ToString()
        };
    }
}
