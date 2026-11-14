using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceMissionGame.Core;

namespace SpaceMissionGame.UI
{
    /// <summary>
    /// [Member 2 - UI] Visual card component for a single selectable `ComponentPart`
    /// from Member 1's `PartsDatabase.cs`.
    /// 
    /// OOP Principle: Observer / Callback Pattern (`Action<ComponentPart>`).
    /// Displays engineering trade-offs and special CubeSat 101 compliance tags.
    /// </summary>
    public class PartCardUI : MonoBehaviour
    {
        [Header("Card Text Elements")]
        [SerializeField] private TMP_Text partNameText;
        [SerializeField] private TMP_Text categoryBadgeText;
        [SerializeField] private TMP_Text statsSummaryText;
        [SerializeField] private TMP_Text tradeOffNoteText;
        [SerializeField] private TMP_Text selectButtonLabel;

        [Header("Card Visual Elements")]
        [SerializeField] private Button selectButton;
        [SerializeField] private Image cardBackgroundImage;
        [SerializeField] private Image equippedBorderImage;

        [Header("Visual State Colors")]
        [SerializeField] private Color normalCardColor = new Color(0.13f, 0.17f, 0.24f, 0.95f);
        [SerializeField] private Color equippedCardColor = new Color(0.12f, 0.32f, 0.26f, 0.98f);
        [SerializeField] private Color equippedBorderColor = new Color(0.18f, 0.80f, 0.44f, 1f);

        private ComponentPart _part;
        private Action<ComponentPart> _onCardSelectedCallback;

        /// <summary>
        /// Binds a `ComponentPart` from Member 1's `PartsDatabase` to this UI card.
        /// </summary>
        public void SetupCard(
            ComponentPart part,
            bool isEquipped,
            Action<ComponentPart> onSelectedCallback)
        {
            _part = part;
            _onCardSelectedCallback = onSelectedCallback;

            if (part == null) return;

            if (partNameText != null)
            {
                partNameText.text = part.Name;
            }

            if (categoryBadgeText != null)
            {
                categoryBadgeText.text = $"[{part.Segment.ToString().ToUpper()} • {part.Category}]";
            }

            if (statsSummaryText != null)
            {
                statsSummaryText.text = FormatStatsLine(part);
            }

            if (tradeOffNoteText != null)
            {
                tradeOffNoteText.text = FormatDescriptionAndWarnings(part);
            }

            SetEquippedVisualState(isEquipped);

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(HandleCardClicked);
            }
        }

        public void SetEquippedVisualState(bool isEquipped)
        {
            if (cardBackgroundImage != null)
            {
                cardBackgroundImage.color = isEquipped ? equippedCardColor : normalCardColor;
            }

            if (equippedBorderImage != null)
            {
                equippedBorderImage.enabled = isEquipped;
                equippedBorderImage.color = equippedBorderColor;
            }

            if (selectButtonLabel != null)
            {
                selectButtonLabel.text = isEquipped ? "EQUIPPED ✓" : "SELECT PART";
            }
        }

        private void HandleCardClicked()
        {
            if (_part != null)
            {
                _onCardSelectedCallback?.Invoke(_part);
            }
        }

        private string FormatStatsLine(ComponentPart p)
        {
            List<string> tags = new List<string>();

            if (p.MaxMassCapacityKg > 0f)
                tags.Add($"Max Load: {p.MaxMassCapacityKg:F2} kg");
            if (p.MassKg > 0f)
                tags.Add($"Mass: {p.MassKg:F2} kg");
            if (p.PowerGeneratedW > 0f)
                tags.Add($"Solar: +{p.PowerGeneratedW:F1} W");
            if (p.PowerConsumedW > 0f)
                tags.Add($"Draw: -{p.PowerConsumedW:F1} W");
            if (p.BatteryCapacityWh > 0f)
                tags.Add($"Battery: {p.BatteryCapacityWh:F0} Wh");
            if (p.DataRateMbps > 0f)
            {
                string dataLabel = p.Category == PartCategory.Payload ? "Data Gen" : "Downlink";
                tags.Add($"{dataLabel}: {p.DataRateMbps:F0} Mbps");
            }
            if (p.SciencePoints > 0)
                tags.Add($"Science: +{p.SciencePoints}");

            tags.Add($"Cost: ${p.CostThousands:F0}k");
            tags.Add($"Rel: {p.ReliabilityScore}%");

            return string.Join(" | ", tags);
        }

        private string FormatDescriptionAndWarnings(ComponentPart p)
        {
            List<string> warnings = new List<string>();

            if (p.RequiresNoaaCameraLicense)
                warnings.Add("<color=#F1C40F>[Needs NOAA Camera License]</color>");
            if (p.UsesDeployableBurnWire)
                warnings.Add("<color=#F1C40F>[Uses Burn-Wire Release]</color>");
            if (p.Category == PartCategory.Battery && !p.IsULListedBattery)
                warnings.Add("<color=#E84C3D>[UNSAFE: No UL 1642 Listing!]</color>");
            if (p.IsHighMeltingPointMetal)
                warnings.Add("<color=#E84C3D>[ODAR Debris Risk: Titanium!]</color>");

            if (warnings.Count == 0) return p.Description;
            return $"{p.Description}\n{string.Join(" ", warnings)}";
        }
    }
}
