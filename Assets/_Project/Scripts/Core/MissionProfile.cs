using System;
using UnityEngine;

namespace SpaceMissionGame.Core
{
    /// <summary>
    /// Defines the goals, constraints, and NASA CML 4 margin thresholds for a playable mission scenario
    /// (e.g., BioNutrients-2 ISS, BisonSat Earth Imager, Artemis Lunar Ice Pathfinder).
    /// Populated by Member 3 in MissionScenariosData.cs and evaluated by Member 1's MissionCalculator.cs.
    /// </summary>
    [Serializable]
    public class MissionProfile
    {
        [Header("1. Mission Identity & NASA Brief (CML 1)")]
        public string missionId;
        public string missionTitle;
        public string difficultyLabel;

        [TextArea(2, 4)]
        public string missionObjective;

        [TextArea(2, 4)]
        [Tooltip("Concept of Operations (CONOPS) summary across Launch, Space, Ground, and User segments.")]
        public string conopsSummary;

        [TextArea(2, 3)]
        [Tooltip("Special engineering rules highlighted to the player during Mission Selection.")]
        public string specialConstraintsNote;

        [Header("2. Core Mission Requirements (CML 2 & 3)")]
        [Tooltip("Total NASA grant budget cap in thousands of USD ($k).")]
        public float budgetCapThousands = 300f;

        [Tooltip("Minimum Science Return score needed to achieve primary science objectives.")]
        public int targetScienceScore = 70;

        [Tooltip("The target GMAT orbital regime required for this mission.")]
        public OrbitType requiredOrbit = OrbitType.ISS_LEO_400km;

        [Tooltip("Recommended (or minimum practical) CubeSat form factor for this mission.")]
        public CubeSatSize recommendedChassisSize = CubeSatSize.OneU;

        [Header("3. JPL CML 4 Technical & Cost Margin Thresholds (%)")]
        [Tooltip("Minimum Mass Margin (%) needed for Green status. (0% to this value = Yellow; < 0% = Red).")]
        public float requiredMassMarginPercent = 10f;

        [Tooltip("Minimum Power Margin (%) needed for Green status after accounting for orbital sunlight/eclipse.")]
        public float requiredPowerMarginPercent = 15f;

        [Tooltip("Minimum Budget Reserve (%) that must remain unspent for Green status.")]
        public float requiredCostReservePercent = 10f;

        [Header("4. Special Engineering & Environmental Constraints")]
        [Tooltip("Maximum pointing error allowed in degrees (0 = no strict pointing requirement; smaller = needs better ADCS).")]
        public float maxAllowedPointingErrorDeg = 0f;

        [Tooltip("True for deep-space/Lunar missions where high radiation requires robust electronics and X-Band comms.")]
        public bool requiresDeepSpaceCommsAndRadProtection = false;

        [Tooltip("Minimum overall reliability score (%) required for Green status in the CML 4 Readiness Review.")]
        [Range(50f, 99f)]
        public float minReliabilityPercent = 80f;

        public MissionProfile() { }
    }
}
