using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RealRail
{
    /// <summary>Small mobile-friendly overlay for selecting one generated run upgrade.</summary>
    public sealed class UpgradeSelectionView : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button[] choiceButtons;
        [SerializeField] Text[] choiceTexts;

        public event Func<UpgradeId, bool> ChoiceSelected;

        void Awake() => Hide();

        public void Show(IReadOnlyList<UpgradeId> choices, UpgradeState state)
        {
            panel.SetActive(true);
            for (var index = 0; index < choiceButtons.Length; index++)
            {
                var active = index < choices.Count;
                choiceButtons[index].gameObject.SetActive(active);
                if (!active) continue;

                var upgrade = choices[index];
                choiceTexts[index].text = $"{DisplayName(upgrade).ToUpperInvariant()}\nLevel {ToRoman(state.GetLevel(upgrade) + 1)}";
                choiceButtons[index].onClick.RemoveAllListeners();
                choiceButtons[index].onClick.AddListener(() => ChoiceSelected?.Invoke(upgrade));
            }
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        static string DisplayName(UpgradeId upgrade) => upgrade switch
        {
            UpgradeId.DoubleShot => "Double Shot",
            UpgradeId.RapidFire => "Rapid Fire",
            UpgradeId.PiercingShot => "Piercing Shot",
            UpgradeId.PowerShot => "Power Shot",
            _ => upgrade.ToString()
        };

        static string ToRoman(int level) => level switch { 1 => "I", 2 => "II", 3 => "III", _ => level.ToString() };
    }
}
