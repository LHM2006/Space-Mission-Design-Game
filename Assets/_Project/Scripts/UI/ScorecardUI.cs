using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceMissionGame.Core;

namespace SpaceMissionGame.UI
{
    /// <summary>
    /// [Member 2 - UI] Controller for Phase 5: Mission Debrief & NASA CML Scorecard (`ScorecardUI.cs`).
    /// 
    /// Directly consumes Member 1's `SpacecraftDesign` and `MissionEvaluationResult`!
    /// 1. Displays Final Grade ("S", "A", "B", "C", "SCRUBBED", or "MISSION FAILED") with color coding.
    /// 2. Displays the 4 NASA Mission Segments (Launch, Space, Ground, User) with RYG status and reasons.
    /// 3. Displays summary statistics and NASA Chief Engineer debrief logs.
    /// 4. Provides buttons to loop back to CML 3 Builder (Redesign) or return to CML 1 Mission Select.
    /// </summary>
    public class ScorecardUI : MonoBehaviour
    {
        [Header("1. Top Grade & Mission Outcome Header")]
        [SerializeField] private TMP_Text missionTitleText;
        [SerializeField] private TMP_Text cmlOutcomeBannerText;
        [SerializeField] private TMP_Text finalGradeBadgeText;
        [SerializeField] private Image finalGradeBadgeBackground;

        [Header("2. 4-Segment RYG Breakdown Texts")]
        [SerializeField] private TMP_Text launchSegmentResultText;
        [SerializeField] private TMP_Text spaceSegmentResultText;
        [SerializeField] private TMP_Text groundSegmentResultText;
        [SerializeField] private TMP_Text userSegmentResultText;

        [Header("3. Core Stats & NASA Chief Engineer Debrief")]
        [SerializeField] private TMP_Text finalScienceScoreText;
        [SerializeField] private TMP_Text finalBudgetReserveText;
        [SerializeField] private TMP_Text finalDataDownlinkedText;
        [SerializeField] private TMP_Text chiefEngineerAdviceText;

        [Header("4. Iterative Loop Navigation Buttons")]
        [SerializeField] private Button redesignInBuilderButton;
        [SerializeField] private Button selectNewMissionButton;

        [Header("NASA RYG & Grade Palette")]
        [SerializeField] private Color goldRankColor = new Color(0.96f, 0.78f, 0.18f);
        [SerializeField] private Color greenColor = new Color(0.18f, 0.80f, 0.44f);
        [SerializeField] private Color yellowColor = new Color(0.95f, 0.77f, 0.06f);
        [SerializeField] private Color redColor = new Color(0.91f, 0.30f, 0.24f);

        // Navigation events for Member 3's GameManager
        public event Action OnRedesignSpacecraftRequested;
        public event Action OnReturnToMissionSelectRequested;

        private void Awake()
        {
            if (redesignInBuilderButton != null)
            {
                redesignInBuilderButton.onClick.AddListener(() => OnRedesignSpacecraftRequested?.Invoke());
            }

            if (selectNewMissionButton != null)
            {
                selectNewMissionButton.onClick.AddListener(() => OnReturnToMissionSelectRequested?.Invoke());
            }
        }

        /// <summary>
        /// Primary entry point for Phase 5 (`PostMission_Scorecard`).
        /// Directly populates the Scorecard UI from Member 1's `SpacecraftDesign` and `MissionEvaluationResult`!
        /// </summary>
        public void ShowScorecard(SpacecraftDesign design, MissionEvaluationResult eval = null)
        {
            if (design == null) design = new SpacecraftDesign();
            if (eval == null) eval = MissionCalculator.EvaluateMission(design);

            string missionTitle = design.SelectedMission != null
                ? design.SelectedMission.Title
                : "NASA CubeSat Mission Debrief";

            if (missionTitleText != null)
            {
                missionTitleText.text = missionTitle;
            }

            if (cmlOutcomeBannerText != null)
            {
                string outcomeTag = eval.IsMissionSuccess
                    ? $"CML 4 Readiness: {eval.CmlChecklistGreenPercentage:F0}% GREEN | Flight Objectives Achieved"
                    : $"CML 4 Readiness: {eval.CmlChecklistGreenPercentage:F0}% GREEN | Mission Terminated ({eval.FinalGrade})";
                cmlOutcomeBannerText.text = outcomeTag;
            }

            // 1. Format Final Grade Badge ("S", "A", "B", "C", "SCRUBBED", "MISSION FAILED")
            ApplyGradeBadgeVisuals(eval.FinalGrade);

            // 2. Populate 4 NASA Mission Segments RYG Breakdown
            FormatSegmentRow(
                launchSegmentResultText,
                "LAUNCH SEGMENT",
                eval.MassMarginStatus,
                $"Mass {eval.TotalMassKg:F2}/{eval.MaxAllowedMassKg:F2} kg (Margin: {eval.MassMarginPercent:F0}%), Vibration Test: {(design.CompletedVibrationTest ? "PASS" : "SKIPPED")}"
            );

            RYGStatus spaceCombined = CombineStatuses(eval.PowerMarginStatus, eval.TestingAndVnvStatus);
            FormatSegmentRow(
                spaceSegmentResultText,
                "SPACE SEGMENT",
                spaceCombined,
                $"Solar Avg {eval.TotalPowerGeneratedW:F1}W vs {eval.TotalPowerConsumedW:F1}W Load (Margin: {eval.PowerMarginPercent:F0}%), Reliability: {eval.FinalReliabilityPercent}%"
            );

            RYGStatus groundCombined = CombineStatuses(eval.TelecomLinkStatus, eval.LicensingStatus);
            FormatSegmentRow(
                groundSegmentResultText,
                "GROUND SEGMENT",
                groundCombined,
                $"Downlink {eval.DataDownlinkedPerOrbitMb:F0} Mbps vs Payload {eval.DataGeneratedPerOrbitMb:F0} Mbps, Licensing: {eval.LicensingStatus}"
            );

            RYGStatus userCombined = CombineStatuses(eval.ScienceStatus, eval.CostStatus);
            FormatSegmentRow(
                userSegmentResultText,
                "USER SEGMENT",
                userCombined,
                $"Science Return: {eval.TotalScienceScore}/{eval.TargetScienceScore} pts, Budget: ${eval.TotalCostThousands:F0}k/${eval.BudgetCapThousands:F0}k"
            );

            // 3. Populate Summary Stats & Chief Engineer Debrief
            if (finalScienceScoreText != null)
            {
                finalScienceScoreText.text = $"Science Score: {eval.TotalScienceScore} / {eval.TargetScienceScore} pts";
            }

            if (finalBudgetReserveText != null)
            {
                finalBudgetReserveText.text =
                    $"Budget Spent: ${eval.TotalCostThousands:F0}k / ${eval.BudgetCapThousands:F0}k (Reserve: {eval.BudgetReservePercent:F0}%)";
            }

            if (finalDataDownlinkedText != null)
            {
                finalDataDownlinkedText.text =
                    $"Telecom Link: {eval.DataDownlinkedPerOrbitMb:F0} Mbps Downlink ({eval.DataGeneratedPerOrbitMb:F0} Mbps Generated)";
            }

            if (chiefEngineerAdviceText != null)
            {
                if (eval.FlightSimulationLogs != null && eval.FlightSimulationLogs.Count > 0)
                {
                    chiefEngineerAdviceText.text = string.Join("\n", eval.FlightSimulationLogs);
                }
                else
                {
                    chiefEngineerAdviceText.text = "Mission simulation completed.";
                }
            }
        }

        private RYGStatus CombineStatuses(RYGStatus a, RYGStatus b)
        {
            if (a == RYGStatus.Red || b == RYGStatus.Red) return RYGStatus.Red;
            if (a == RYGStatus.Yellow || b == RYGStatus.Yellow) return RYGStatus.Yellow;
            return RYGStatus.Green;
        }

        private void ApplyGradeBadgeVisuals(string grade)
        {
            string normalized = string.IsNullOrEmpty(grade) ? "C" : grade.Trim().ToUpperInvariant();

            Color badgeColor = yellowColor;
            if (normalized == "S")
            {
                badgeColor = goldRankColor;
            }
            else if (normalized == "A")
            {
                badgeColor = greenColor;
            }
            else if (normalized == "B" || normalized == "C")
            {
                badgeColor = yellowColor;
            }
            else
            {
                badgeColor = redColor;
            }

            if (finalGradeBadgeText != null)
            {
                finalGradeBadgeText.text = normalized;
                finalGradeBadgeText.color = badgeColor;
            }

            if (finalGradeBadgeBackground != null)
            {
                finalGradeBadgeBackground.color = new Color(badgeColor.r, badgeColor.g, badgeColor.b, 0.20f);
            }
        }

        private void FormatSegmentRow(TMP_Text targetText, string segmentTitle, RYGStatus status, string reason)
        {
            if (targetText == null) return;

            string hexColor = "#2ECC71";
            string statusTag = "GREEN";

            if (status == RYGStatus.Yellow)
            {
                hexColor = "#F1C40F";
                statusTag = "YELLOW";
            }
            else if (status == RYGStatus.Red)
            {
                hexColor = "#E84C3D";
                statusTag = "RED";
            }

            targetText.text = $"<b>{segmentTitle}</b> [<color={hexColor}>{statusTag}</color>]: {reason}";
        }

        // =====================================================================
        // EDITOR TEST HELPERS (Uses Member 1's actual PartsDatabase & MissionCalculator!)
        // =====================================================================

        [ContextMenu("Test 1: Show S-Rank BioNutrients-2 Scorecard")]
        public void TestSRankScorecard()
        {
            SpacecraftDesign design = new SpacecraftDesign
            {
                SelectedMission = new MissionProfile
                {
                    MissionId = "MISSION_1",
                    Title = "Mission 1: BioNutrients-2 (ISS Research)",
                    Difficulty = MissionDifficulty.Easy_ISSCubeSat,
                    BudgetCapThousands = 200f,
                    TargetScienceScore = 70,
                    RequiredOrbitId = "ORBIT_ISS",
                    RequiredMassMarginPct = 15f,
                    RequiredPowerMarginPct = 15f
                },
                SelectedChassis = PartsDatabase.GetPartById("CHASSIS_3U"),
                SelectedLaunchVehicle = PartsDatabase.GetPartById("LAUNCH_ISS"),
                SelectedOrbit = PartsDatabase.GetPartById("ORBIT_ISS"),
                SelectedPayload = PartsDatabase.GetPartById("PAYLOAD_BIONUTRIENTS"),
                SelectedSolarPanel = PartsDatabase.GetPartById("SOLAR_1U_BODY"),
                SelectedBattery = PartsDatabase.GetPartById("BATTERY_UL_12WH"),
                SelectedADCS = PartsDatabase.GetPartById("ADCS_PASSIVE_MAGNET"),
                SelectedComms = PartsDatabase.GetPartById("COMMS_UHF"),
                SelectedGroundStation = PartsDatabase.GetPartById("GROUND_UHF_YAGI"),
                HasFccIaruRfLicense = true,
                CompletedVibrationTest = true,
                CompletedTvacBakeoutTest = true,
                CompletedDitlSoftwareTest = true,
                UsedDoubleBurnWire = true
            };

            ShowScorecard(design, MissionCalculator.EvaluateMission(design));
        }

        [ContextMenu("Test 2: Show Scrubbed Scorecard (Missing NOAA Camera License)")]
        public void TestScrubbedScorecard()
        {
            SpacecraftDesign design = new SpacecraftDesign
            {
                SelectedMission = new MissionProfile
                {
                    MissionId = "MISSION_2",
                    Title = "Mission 2: BisonSat / Earth Forest Imager",
                    Difficulty = MissionDifficulty.Medium_EarthObserver,
                    BudgetCapThousands = 300f,
                    TargetScienceScore = 85,
                    RequiredOrbitId = "ORBIT_SSO",
                    RequiredMassMarginPct = 20f,
                    RequiredPowerMarginPct = 20f
                },
                SelectedChassis = PartsDatabase.GetPartById("CHASSIS_3U"),
                SelectedLaunchVehicle = PartsDatabase.GetPartById("LAUNCH_RIDESHARE_LEO"),
                SelectedOrbit = PartsDatabase.GetPartById("ORBIT_SSO"),
                SelectedPayload = PartsDatabase.GetPartById("PAYLOAD_EARTH_CAMERA"),
                SelectedSolarPanel = PartsDatabase.GetPartById("SOLAR_3U_DEPLOYABLE"),
                SelectedBattery = PartsDatabase.GetPartById("BATTERY_UL_12WH"),
                SelectedADCS = PartsDatabase.GetPartById("ADCS_MAGNETORQUERS"),
                SelectedComms = PartsDatabase.GetPartById("COMMS_SBAND"),
                SelectedGroundStation = PartsDatabase.GetPartById("GROUND_SBAND_DISH"),
                HasFccIaruRfLicense = true,
                HasNoaaRemoteSensingLicense = false, // Missing NOAA license!
                CompletedVibrationTest = true
            };

            ShowScorecard(design, MissionCalculator.EvaluateMission(design));
        }
    }
}
