using UnityEngine;
using TMPro;
using SpaceMissionGame.Core;

namespace SpaceMissionGame.UI
{
    /// <summary>
    /// [Member 2 - UI] Master controller for the Live Resource Bars Panel (`ResourceBarsUI.cs`).
    /// Directly consumes Member 1's `MissionEvaluationResult` from `MissionCalculator.EvaluateMission(...)`
    /// to update all 6 core engineering trade-off metrics and the JPL CML 4 RYG readiness status.
    /// </summary>
    public class ResourceBarsUI : MonoBehaviour
    {
        [Header("The 6 Core Resource Meters")]
        [SerializeField] private ResourceMeterItemUI massMeter;
        [SerializeField] private ResourceMeterItemUI powerMeter;
        [SerializeField] private ResourceMeterItemUI budgetMeter;
        [SerializeField] private ResourceMeterItemUI dataMeter;
        [SerializeField] private ResourceMeterItemUI scienceMeter;
        [SerializeField] private ResourceMeterItemUI marginRiskMeter;

        [Header("CML 4 Readiness & Diagnostic Banner")]
        [Tooltip("Displays overall CML 4 status, e.g., 'CML 4 RYG Status: GO FOR LAUNCH (86% GREEN)'")]
        [SerializeField] private TMP_Text overallReadinessText;

        [Tooltip("Explains WHY a trade-off helps or hurts based on Member 1's MissionCalculator")]
        [SerializeField] private TMP_Text tradeOffDiagnosticText;

        /// <summary>
        /// Directly refreshes all 6 resource bars using Member 1's `MissionEvaluationResult`
        /// returned by `MissionCalculator.EvaluateMission(design)`.
        /// </summary>
        public void RefreshDashboard(MissionEvaluationResult eval)
        {
            if (eval == null) return;

            // 1. Mass (kg) vs Max Allowed Dispenser/Rocket Limit
            if (massMeter != null)
            {
                massMeter.SetMeterData(
                    "Mass",
                    eval.TotalMassKg,
                    eval.MaxAllowedMassKg,
                    "kg",
                    eval.MassMarginStatus,
                    $"Margin: {eval.MassMarginPercent:F0}%"
                );
            }

            // 2. Power Consumed (W) vs Orbit-Adjusted Solar Power Generated (W)
            if (powerMeter != null)
            {
                powerMeter.SetMeterData(
                    "Power Load",
                    eval.TotalPowerConsumedW,
                    eval.TotalPowerGeneratedW,
                    "W",
                    eval.PowerMarginStatus,
                    $"Margin: {eval.PowerMarginPercent:F0}%"
                );
            }

            // 3. Budget Spent ($k) vs Mission Grant Cap ($k)
            if (budgetMeter != null)
            {
                budgetMeter.SetMeterData(
                    "Budget",
                    eval.TotalCostThousands,
                    eval.BudgetCapThousands,
                    "$k",
                    eval.CostStatus,
                    $"Reserve: {eval.BudgetReservePercent:F0}%"
                );
            }

            // 4. Data Volume (Mbps generated vs Downlink capacity)
            if (dataMeter != null)
            {
                dataMeter.SetMeterData(
                    "Telecom Link",
                    eval.DataGeneratedPerOrbitMb,
                    eval.DataDownlinkedPerOrbitMb,
                    "Mbps",
                    eval.TelecomLinkStatus
                );
            }

            // 5. Science Return (Score vs Target Requirement)
            if (scienceMeter != null)
            {
                scienceMeter.SetMeterData(
                    "Science Return",
                    eval.TotalScienceScore,
                    eval.TargetScienceScore,
                    "pts",
                    eval.ScienceStatus
                );
            }

            // 6. Reliability (%) & Testing/VnV Status
            if (marginRiskMeter != null)
            {
                RYGStatus relStatus = eval.FinalReliabilityPercent >= 80
                    ? RYGStatus.Green
                    : (eval.FinalReliabilityPercent >= 65 ? RYGStatus.Yellow : RYGStatus.Red);

                marginRiskMeter.SetMeterData(
                    "Reliability",
                    eval.FinalReliabilityPercent,
                    100f,
                    "%",
                    relStatus,
                    $"VnV: {eval.TestingAndVnvStatus}"
                );
            }

            // Update JPL CML 4 Readiness Summary Banner
            if (overallReadinessText != null)
            {
                if (!eval.IsLaunchApproved)
                {
                    overallReadinessText.text =
                        $"CML 4 RYG Checklist: {eval.CmlChecklistGreenPercentage:F0}% GREEN — NO-GO (SCRUB RISK)";
                    overallReadinessText.color = new Color(0.91f, 0.30f, 0.24f);
                }
                else if (eval.CmlChecklistGreenPercentage >= 80f)
                {
                    overallReadinessText.text =
                        $"CML 4 RYG Checklist: {eval.CmlChecklistGreenPercentage:F0}% GREEN — GO FOR LAUNCH";
                    overallReadinessText.color = new Color(0.18f, 0.80f, 0.44f);
                }
                else
                {
                    overallReadinessText.text =
                        $"CML 4 RYG Checklist: {eval.CmlChecklistGreenPercentage:F0}% GREEN — MARGINAL (Need >=80%)";
                    overallReadinessText.color = new Color(0.95f, 0.77f, 0.06f);
                }
            }

            // Update Diagnostic Explanation from Member 1's FlightSimulationLogs / RYG statuses
            if (tradeOffDiagnosticText != null)
            {
                tradeOffDiagnosticText.text = BuildDiagnosticMessage(eval);
            }
        }

        private string BuildDiagnosticMessage(MissionEvaluationResult eval)
        {
            if (eval.MassMarginStatus == RYGStatus.Red)
                return $"[MASS / ODAR RED] Mass ({eval.TotalMassKg:F2}/{eval.MaxAllowedMassKg:F2} kg, Margin {eval.MassMarginPercent:F0}%) fails dispenser or ODAR debris rules!";
            if (eval.PowerMarginStatus == RYGStatus.Red)
                return $"[POWER RED] Orbit-averaged solar power ({eval.TotalPowerGeneratedW:F1}W) or battery capacity cannot sustain {eval.TotalPowerConsumedW:F1}W load through eclipse!";
            if (eval.CostStatus == RYGStatus.Red)
                return $"[BUDGET RED] Total cost (${eval.TotalCostThousands:F0}k) exceeds mission grant cap (${eval.BudgetCapThousands:F0}k)!";
            if (eval.TelecomLinkStatus == RYGStatus.Red)
                return $"[TELECOM RED] Downlink capacity ({eval.DataDownlinkedPerOrbitMb:F0} Mbps) cannot support payload data ({eval.DataGeneratedPerOrbitMb:F0} Mbps)!";
            if (eval.ScienceStatus == RYGStatus.Red)
                return $"[SCIENCE RED] Science score ({eval.TotalScienceScore}/{eval.TargetScienceScore}) or selected orbit does not meet mission requirements!";

            if (eval.FlightSimulationLogs != null && eval.FlightSimulationLogs.Count > 0)
            {
                return eval.FlightSimulationLogs[eval.FlightSimulationLogs.Count - 1];
            }

            return "[ALL SYSTEMS NOMINAL] Design meets NASA CubeSat 101 & JPL CML 4 margins.";
        }

        // =====================================================================
        // EDITOR TEST HELPERS (Uses Member 1's actual PartsDatabase & MissionCalculator!)
        // =====================================================================

        [ContextMenu("Test 1: Evaluate BioNutrients-2 Nominal 3U Design (Member 1 Engine)")]
        public void TestMember1NominalEvaluation()
        {
            SpacecraftDesign design = new SpacecraftDesign
            {
                SelectedMission = new MissionProfile
                {
                    MissionId = "MISSION_1",
                    Title = "BioNutrients-2 ISS Research",
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

            MissionEvaluationResult result = MissionCalculator.EvaluateMission(design);
            RefreshDashboard(result);
        }
    }
}
