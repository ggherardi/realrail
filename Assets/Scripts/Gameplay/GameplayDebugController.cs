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
        [SerializeField] RailgunPrototype railgunPrototype;
        [SerializeField] AutoFire autoFire;
        [SerializeField] EnemySpawner enemySpawner;
        [SerializeField] CryoStormPrototype cryoStormPrototype;
        [SerializeField] VolcanoPrototype volcanoPrototype;

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
            if (keyboard.f3Key.wasPressedThisFrame) ToggleRailgunPrototype();
            if (keyboard.f4Key.wasPressedThisFrame) FireRailgunPrototype();
            if (keyboard.f5Key.wasPressedThisFrame) SpawnDenseHorde();
            if (keyboard.f6Key.wasPressedThisFrame) ToggleCryoStormPrototype();
            if (keyboard.f7Key.wasPressedThisFrame) TriggerCryoStormPrototype();
            if (keyboard.f8Key.wasPressedThisFrame) ToggleVolcanoPrototype();
            if (keyboard.f9Key.wasPressedThisFrame) SpawnVolcanoPrototype();
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

        public void ToggleRailgunPrototype()
        {
            if (railgunPrototype == null) return;
            railgunPrototype.SetEnabled(!railgunPrototype.IsEnabled);
            Report($"Railgun Prototype {(railgunPrototype.IsEnabled ? "ON" : "OFF")}");
        }

        public bool FireRailgunPrototype()
        {
            if (autoFire == null || !autoFire.FireRailgunNow())
            {
                Report("Railgun prototype shot unavailable");
                return false;
            }
            Report("Railgun prototype fired");
            return true;
        }

        public void SpawnDenseHorde()
        {
            if (enemySpawner == null || !enemySpawner.SpawnDebugBurst(18))
            {
                Report("Dense horde unavailable outside an active wave");
                return;
            }
            Report("Spawned dense-horde test burst");
        }

        public void ToggleCryoStormPrototype()
        {
            if (cryoStormPrototype == null) return;
            cryoStormPrototype.SetEnabled(!cryoStormPrototype.IsEnabled);
            Report($"Cryo Storm Prototype {(cryoStormPrototype.IsEnabled ? "ON" : "OFF")}");
        }

        public bool TriggerCryoStormPrototype()
        {
            if (cryoStormPrototype == null || !cryoStormPrototype.TryActivate())
            {
                Report("Cryo Storm requires active wave enemies");
                return false;
            }
            Report("Cryo Storm triggered");
            return true;
        }

        public void ToggleVolcanoPrototype()
        {
            if (volcanoPrototype == null) return;
            volcanoPrototype.SetEnabled(!volcanoPrototype.IsEnabled);
            Report($"Volcano Prototype {(volcanoPrototype.IsEnabled ? "ON" : "OFF")}");
        }

        public bool SpawnVolcanoPrototype()
        {
            if (volcanoPrototype == null || !volcanoPrototype.TrySpawnNow())
            {
                Report("Volcano requires an active wave and an empty battlefield slot");
                return false;
            }
            Report("Volcano prototype spawned");
            return true;
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
