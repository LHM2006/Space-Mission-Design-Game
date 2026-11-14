using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMissionGame.Core
{
    /// <summary>
    /// Member 1: The "Brain" — Trade-Off Math Calculator & JPL CML 4 RYG Checklist Evaluator.
    /// Directly consumes and populates the shared OOP models in GameEnumsAndModels.cs.
    /// </summary>
    public static class MissionCalculator
    {
        /// <summary>
        /// Overload that evaluates a SpacecraftDesign using its internal design.SelectedMission.
        /// </summary>
        public static MissionEvaluationResult EvaluateMission(SpacecraftDesign design)
        {
            MissionProfile mission = design != null ? design.SelectedMission : null;
            return EvaluateMission(mission, design);
        }

        /// <summary>
        /// Full evaluation of a player's SpacecraftDesign against a NASA MissionProfile.
        /// Safe to call at any time (Phase 2 live UI bars, Phase 3 CML 4 RYG checklist, Phase 4/5 debrief).
        /// </summary>
        public static MissionEvaluationResult EvaluateMission(MissionProfile mission, SpacecraftDesign design)
        {
            MissionEvaluationResult result = new MissionEvaluationResult();

            if (design == null) design = new SpacecraftDesign();
            if (mission == null) mission = design.SelectedMission ?? new MissionProfile();

            List<ComponentPart> equippedParts = design.GetAllSelectedParts();

            // =========================================================================
            // 1. MASS & ENVELOPE MARGIN CALCULATIONS
            // =========================================================================
            float totalMass = design.ExtraMassKg;
            foreach (ComponentPart part in equippedParts)
            {
                totalMass += part.MassKg;
            }
            result.TotalMassKg = totalMass;

            float chassisMaxMass = design.SelectedChassis != null && design.SelectedChassis.MaxMassCapacityKg > 0f
                ? design.SelectedChassis.MaxMassCapacityKg
                : 1.33f;
            float launcherMaxMass = design.SelectedLaunchVehicle != null && design.SelectedLaunchVehicle.MaxMassCapacityKg > 0f
                ? design.SelectedLaunchVehicle.MaxMassCapacityKg
                : chassisMaxMass;

            if (design.SelectedChassis != null && design.SelectedLaunchVehicle != null)
            {
                result.MaxAllowedMassKg = Mathf.Min(chassisMaxMass, launcherMaxMass);
            }
            else if (design.SelectedChassis != null)
            {
                result.MaxAllowedMassKg = chassisMaxMass;
            }
            else if (design.SelectedLaunchVehicle != null)
            {
                result.MaxAllowedMassKg = launcherMaxMass;
            }
            else
            {
                result.MaxAllowedMassKg = 1.33f;
            }

            result.MassMarginPercent = result.MaxAllowedMassKg > 0f
                ? ((result.MaxAllowedMassKg - result.TotalMassKg) / result.MaxAllowedMassKg) * 100f
                : -100f;

            float reqMassMargin = mission.RequiredMassMarginPct > 0f ? mission.RequiredMassMarginPct : 15f;
            bool hasOdardebrisViolation = false;
            foreach (ComponentPart part in equippedParts)
            {
                if (part.IsHighMeltingPointMetal) hasOdardebrisViolation = true;
            }

            if (design.SelectedChassis == null || result.MassMarginPercent < 0f || hasOdardebrisViolation)
            {
                result.MassMarginStatus = RYGStatus.Red;
            }
            else if (result.MassMarginPercent < reqMassMargin)
            {
                result.MassMarginStatus = RYGStatus.Yellow;
            }
            else
            {
                result.MassMarginStatus = RYGStatus.Green;
            }

            // =========================================================================
            // 2. POWER & ORBITAL ECLIPSE CALCULATIONS (GMAT Sunlight Ratio)
            // =========================================================================
            float peakSolarW = 0f;
            float powerConsumedW = 0f;
            float batteryWh = 0f;

            foreach (ComponentPart part in equippedParts)
            {
                peakSolarW += part.PowerGeneratedW;
                powerConsumedW += part.PowerConsumedW;
                batteryWh += part.BatteryCapacityWh;
            }

            float sunlightFraction = GetOrbitSunlightFraction(design.SelectedOrbit);
            float orbitPeriodMins = GetOrbitPeriodMinutes(design.SelectedOrbit);

            result.TotalPowerGeneratedW = peakSolarW * sunlightFraction;
            result.TotalPowerConsumedW = powerConsumedW;

            if (result.TotalPowerGeneratedW > 0f)
            {
                result.PowerMarginPercent = ((result.TotalPowerGeneratedW - result.TotalPowerConsumedW) / result.TotalPowerGeneratedW) * 100f;
            }
            else
            {
                result.PowerMarginPercent = -100f;
            }

            float eclipseHours = (orbitPeriodMins * (1f - sunlightFraction)) / 60f;
            float eclipseEnergyNeededWh = result.TotalPowerConsumedW * eclipseHours;
            bool survivesEclipse = batteryWh > 0f && batteryWh >= eclipseEnergyNeededWh;

            float reqPowerMargin = mission.RequiredPowerMarginPct > 0f ? mission.RequiredPowerMarginPct : 15f;

            if (result.TotalPowerGeneratedW <= 0f || result.PowerMarginPercent < 0f || !survivesEclipse)
            {
                result.PowerMarginStatus = RYGStatus.Red;
            }
            else if (result.PowerMarginPercent < reqPowerMargin)
            {
                result.PowerMarginStatus = RYGStatus.Yellow;
            }
            else
            {
                result.PowerMarginStatus = RYGStatus.Green;
            }

            // =========================================================================
            // 3. BUDGET & COST RESERVE CALCULATIONS
            // =========================================================================
            float totalCost = design.ExtraTestingCostThousands;
            foreach (ComponentPart part in equippedParts)
            {
                totalCost += part.CostThousands;
            }

            result.TotalCostThousands = totalCost;
            result.BudgetCapThousands = mission.BudgetCapThousands > 0f ? mission.BudgetCapThousands : 200f;
            result.BudgetReservePercent = result.BudgetCapThousands > 0f
                ? ((result.BudgetCapThousands - result.TotalCostThousands) / result.BudgetCapThousands) * 100f
                : -100f;

            if (result.TotalCostThousands > result.BudgetCapThousands)
            {
                result.CostStatus = RYGStatus.Red;
            }
            else if (result.BudgetReservePercent < 10f)
            {
                result.CostStatus = RYGStatus.Yellow;
            }
            else
            {
                result.CostStatus = RYGStatus.Green;
            }

            // =========================================================================
            // 4. TELECOM DATA LINK, SCIENCE SCORE & RELIABILITY
            // =========================================================================
            float payloadDataRate = design.SelectedPayload != null ? design.SelectedPayload.DataRateMbps : 0f;
            float commsRate = design.SelectedComms != null ? design.SelectedComms.DataRateMbps : 0f;
            float groundRate = design.SelectedGroundStation != null ? design.SelectedGroundStation.DataRateMbps : 0f;

            result.DataGeneratedPerOrbitMb = payloadDataRate;
            result.DataDownlinkedPerOrbitMb = Mathf.Min(commsRate, groundRate);

            bool isLunarMission = mission.Difficulty == MissionDifficulty.Hard_LunarPathfinder
                || string.Equals(mission.RequiredOrbitId, "ORBIT_LUNAR", StringComparison.OrdinalIgnoreCase);

            bool lunarCommsValid = true;
            if (isLunarMission)
            {
                bool hasXBand = design.SelectedComms != null && design.SelectedComms.DataRateMbps >= 100f;
                bool hasDsn = design.SelectedGroundStation != null && design.SelectedGroundStation.DataRateMbps >= 100f;
                lunarCommsValid = hasXBand && hasDsn;
            }

            if (design.SelectedComms == null || design.SelectedGroundStation == null
                || result.DataDownlinkedPerOrbitMb <= 0f
                || (result.DataGeneratedPerOrbitMb > 0f && result.DataDownlinkedPerOrbitMb < result.DataGeneratedPerOrbitMb * 0.7f)
                || !lunarCommsValid)
            {
                result.TelecomLinkStatus = RYGStatus.Red;
            }
            else if (result.DataDownlinkedPerOrbitMb < result.DataGeneratedPerOrbitMb)
            {
                result.TelecomLinkStatus = RYGStatus.Yellow;
            }
            else
            {
                result.TelecomLinkStatus = RYGStatus.Green;
            }

            // Science Score & Orbit Match
            int rawScience = 0;
            foreach (ComponentPart part in equippedParts)
            {
                rawScience += part.SciencePoints;
            }

            bool orbitMatchesMission = string.IsNullOrEmpty(mission.RequiredOrbitId)
                || (design.SelectedOrbit != null && string.Equals(design.SelectedOrbit.Id, mission.RequiredOrbitId, StringComparison.OrdinalIgnoreCase));

            result.TotalScienceScore = rawScience;
            result.TargetScienceScore = mission.TargetScienceScore > 0 ? mission.TargetScienceScore : 70;

            if (design.SelectedPayload == null || !orbitMatchesMission || result.TotalScienceScore < result.TargetScienceScore * 0.75f)
            {
                result.ScienceStatus = RYGStatus.Red;
            }
            else if (result.TotalScienceScore < result.TargetScienceScore)
            {
                result.ScienceStatus = RYGStatus.Yellow;
            }
            else
            {
                result.ScienceStatus = RYGStatus.Green;
            }

            // =========================================================================
            // 5. REGULATORY LICENSING & ENVIRONMENTAL TESTING (CubeSat 101)
            // =========================================================================
            bool requiresNoaa = design.SelectedPayload != null && design.SelectedPayload.RequiresNoaaCameraLicense;
            bool requiresFcc = design.SelectedComms != null;

            bool fccOk = !requiresFcc || design.HasFccIaruRfLicense;
            bool noaaOk = !requiresNoaa || design.HasNoaaRemoteSensingLicense;

            result.LicensingStatus = (fccOk && noaaOk) ? RYGStatus.Green : RYGStatus.Red;

            bool hasBurnWireParts = false;
            bool hasSafeBattery = design.SelectedBattery != null && design.SelectedBattery.IsULListedBattery;
            foreach (ComponentPart part in equippedParts)
            {
                if (part.UsesDeployableBurnWire) hasBurnWireParts = true;
            }

            bool burnWireOk = !hasBurnWireParts || design.UsedDoubleBurnWire;
            bool allCoreTestsDone = design.CompletedVibrationTest
                && design.CompletedTvacBakeoutTest
                && design.CompletedDitlSoftwareTest;

            if (!hasSafeBattery || (!design.CompletedVibrationTest && !design.CompletedDitlSoftwareTest))
            {
                result.TestingAndVnvStatus = RYGStatus.Red;
            }
            else if (!allCoreTestsDone || !burnWireOk)
            {
                result.TestingAndVnvStatus = RYGStatus.Yellow;
            }
            else
            {
                result.TestingAndVnvStatus = RYGStatus.Green;
            }

            // Overall Reliability Calculation
            float avgPartReliability = 0f;
            if (equippedParts.Count > 0)
            {
                foreach (ComponentPart part in equippedParts)
                {
                    avgPartReliability += part.ReliabilityScore;
                }
                avgPartReliability /= equippedParts.Count;
            }

            int testReliabilityMod = design.ExtraReliabilityModifier;
            testReliabilityMod += design.CompletedVibrationTest ? 3 : -10;
            testReliabilityMod += design.CompletedTvacBakeoutTest ? 3 : -8;
            testReliabilityMod += design.CompletedDitlSoftwareTest ? 3 : -10;
            if (hasBurnWireParts) testReliabilityMod += design.UsedDoubleBurnWire ? 3 : -6;
            if (!hasSafeBattery) testReliabilityMod -= 20;

            result.FinalReliabilityPercent = equippedParts.Count > 0
                ? Mathf.Clamp(Mathf.RoundToInt(avgPartReliability) + testReliabilityMod, 0, 99)
                : 0;

            // =========================================================================
            // 6. JPL CML 4 GREEN CHECKLIST PERCENTAGE (7 Core RYG Pillars)
            // =========================================================================
            RYGStatus[] statuses = new RYGStatus[]
            {
                result.ScienceStatus,
                result.MassMarginStatus,
                result.PowerMarginStatus,
                result.TelecomLinkStatus,
                result.LicensingStatus,
                result.TestingAndVnvStatus,
                result.CostStatus
            };

            int greenCount = 0;
            int redCount = 0;
            foreach (RYGStatus s in statuses)
            {
                if (s == RYGStatus.Green) greenCount++;
                else if (s == RYGStatus.Red) redCount++;
            }

            result.CmlChecklistGreenPercentage = (greenCount / 7f) * 100f;

            // =========================================================================
            // 7. PHASE 4 & 5 FLIGHT SIMULATION LOGS & FINAL GRADE
            // =========================================================================
            PopulateFlightSimulationAndGrade(
                mission,
                design,
                result,
                fccOk,
                noaaOk,
                hasSafeBattery,
                hasOdardebrisViolation,
                orbitMatchesMission,
                survivesEclipse,
                hasBurnWireParts,
                redCount);

            return result;
        }

        private static void PopulateFlightSimulationAndGrade(
            MissionProfile mission,
            SpacecraftDesign design,
            MissionEvaluationResult result,
            bool fccOk,
            bool noaaOk,
            bool hasSafeBattery,
            bool hasOdardebrisViolation,
            bool orbitMatchesMission,
            bool survivesEclipse,
            bool hasBurnWireParts,
            int redCount)
        {
            result.FlightSimulationLogs.Clear();

            // Check Pre-Launch Scrub (Demanifested before liftoff)
            bool scrubbed = !design.IsCoreDesignComplete()
                || !fccOk
                || !noaaOk
                || !hasSafeBattery
                || hasOdardebrisViolation
                || result.TotalMassKg > result.MaxAllowedMassKg
                || result.TotalCostThousands > result.BudgetCapThousands;

            if (scrubbed)
            {
                result.IsLaunchApproved = false;
                result.IsMissionSuccess = false;
                result.FinalGrade = "SCRUBBED";

                result.FlightSimulationLogs.Add("[PRE-LAUNCH REVIEW] NASA Launch Integration Authority halted spacecraft manifesting.");

                if (!fccOk)
                    result.FlightSimulationLogs.Add("[SCRUBBED] Missing FCC/IARU Radio Frequency License. Unlicensed transmitters are prohibited from launch.");
                if (!noaaOk)
                    result.FlightSimulationLogs.Add("[SCRUBBED] Missing NOAA Remote Sensing License for Earth-imaging camera payload.");
                if (!hasSafeBattery)
                    result.FlightSimulationLogs.Add("[SCRUBBED] Range Safety Violation: Battery lacks UL 1642 certification and deployment inhibits.");
                if (hasOdardebrisViolation)
                    result.FlightSimulationLogs.Add("[SCRUBBED] ODAR Orbital Debris Violation: Titanium chassis will not burn up safely upon atmospheric re-entry.");
                if (result.TotalMassKg > result.MaxAllowedMassKg)
                    result.FlightSimulationLogs.Add($"[SCRUBBED] Spacecraft mass ({result.TotalMassKg:F2} kg) exceeds dispenser limit ({result.MaxAllowedMassKg:F2} kg).");
                if (result.TotalCostThousands > result.BudgetCapThousands)
                    result.FlightSimulationLogs.Add($"[SCRUBBED] Budget overrun: Spent ${result.TotalCostThousands:F0}k vs ${result.BudgetCapThousands:F0}k cap.");

                return;
            }

            result.IsLaunchApproved = true;
            result.FlightSimulationLogs.Add("[PHASE 4: LAUNCH] Pre-launch checks passed. Rocket liftoff nominal!");

            // Stage 1: Vibration & Ascent
            if (!design.CompletedVibrationTest)
            {
                result.IsMissionSuccess = false;
                result.FinalGrade = "MISSION FAILED";
                result.FlightSimulationLogs.Add("[STAGE 1: ASCENT FAILURE] Spacecraft suffered structural/solder failure during Max-Q vibration (Vibration Test was skipped!).");
                return;
            }
            result.FlightSimulationLogs.Add("[STAGE 1: ASCENT] Survived rocket vibration loads and reached target altitude.");

            // Stage 2: Dispenser Ejection & 45-min DITL Timer
            if (!design.CompletedDitlSoftwareTest)
            {
                result.IsMissionSuccess = false;
                result.FinalGrade = "MISSION FAILED";
                result.FlightSimulationLogs.Add("[STAGE 2: EJECTION FAILURE] Flight software hung during the 45-minute post-deployment timer (DITL Test was skipped!).");
                return;
            }
            result.FlightSimulationLogs.Add("[STAGE 2: DEPLOYMENT] Ejected from dispenser; 45-minute DITL inhibit timer completed.");

            // Stage 3: Burn-Wire Deployment
            if (hasBurnWireParts && !design.UsedDoubleBurnWire && result.MassMarginPercent < mission.RequiredMassMarginPct)
            {
                result.IsMissionSuccess = false;
                result.FinalGrade = "MISSION FAILED";
                result.FlightSimulationLogs.Add("[STAGE 3: MECHANISM JAM] Single non-redundant burn wire failed to release stowed solar/antenna mechanisms in cold vacuum!");
                return;
            }
            result.FlightSimulationLogs.Add("[STAGE 3: BURN-WIRE] Solar panels and antennas deployed cleanly.");

            // Stage 4: Orbit, Power/Eclipse & Telecom
            if (!orbitMatchesMission)
            {
                result.IsMissionSuccess = false;
                result.FinalGrade = "MISSION FAILED";
                result.FlightSimulationLogs.Add($"[STAGE 4: ORBIT MISMATCH] Spacecraft inserted into wrong orbit ({design.SelectedOrbit?.Id}) instead of required {mission.RequiredOrbitId}!");
                return;
            }

            if (result.PowerMarginStatus == RYGStatus.Red || !survivesEclipse)
            {
                result.IsMissionSuccess = false;
                result.FinalGrade = "MISSION FAILED";
                result.FlightSimulationLogs.Add($"[STAGE 4: ECLIPSE BROWN-OUT] Power deficit ({result.TotalPowerGeneratedW:F1}W orbit-avg vs {result.TotalPowerConsumedW:F1}W draw) drained battery in eclipse!");
                return;
            }

            if (result.TelecomLinkStatus == RYGStatus.Red)
            {
                result.IsMissionSuccess = false;
                result.FinalGrade = "MISSION FAILED";
                result.FlightSimulationLogs.Add("[STAGE 4: TELECOM FAILURE] Radio/Ground station could not close the downlink budget for payload science data.");
                return;
            }

            // Survived all stages! Check TVAC Outgassing & Final Grade
            result.IsMissionSuccess = true;
            if (!design.CompletedTvacBakeoutTest && design.SelectedPayload != null && design.SelectedPayload.RequiresNoaaCameraLicense)
            {
                result.TotalScienceScore = Mathf.RoundToInt(result.TotalScienceScore * 0.70f);
                result.FlightSimulationLogs.Add("[WARNING: OUTGASSING] Skipping TVAC Bakeout caused adhesive outgassing to fog the camera lens in vacuum (-30% Science)!");
            }

            result.FlightSimulationLogs.Add($"[STAGE 4: OPERATIONS] Collecting science in sunlight, surviving eclipse on battery, and downlinking {result.DataDownlinkedPerOrbitMb:F0} Mbps to {design.SelectedGroundStation.Name}!");

            if (result.CmlChecklistGreenPercentage >= 85f && redCount == 0 && result.TotalScienceScore >= result.TargetScienceScore)
            {
                result.FinalGrade = result.CmlChecklistGreenPercentage >= 99f ? "S" : "A";
            }
            else if (result.TotalScienceScore >= result.TargetScienceScore * 0.85f)
            {
                result.FinalGrade = "B";
            }
            else
            {
                result.FinalGrade = "C";
            }
        }

        /// <summary>
        /// Returns the GMAT sunlit fraction of an orbit (0.0 to 1.0).
        /// </summary>
        public static float GetOrbitSunlightFraction(ComponentPart orbitPart)
        {
            if (orbitPart == null || string.IsNullOrEmpty(orbitPart.Id)) return 0.60f;

            string id = orbitPart.Id.ToUpperInvariant();
            if (id.Contains("LUNAR")) return 0.85f;
            if (id.Contains("SSO") || id.Contains("SUNSYNC")) return 0.68f;
            return 0.60f; // Default ISS 400km LEO (60% sun, 40% eclipse)
        }

        /// <summary>
        /// Returns the GMAT orbital period in minutes.
        /// </summary>
        public static float GetOrbitPeriodMinutes(ComponentPart orbitPart)
        {
            if (orbitPart == null || string.IsNullOrEmpty(orbitPart.Id)) return 92.5f;

            string id = orbitPart.Id.ToUpperInvariant();
            if (id.Contains("LUNAR")) return 118.0f;
            if (id.Contains("SSO") || id.Contains("SUNSYNC")) return 96.7f;
            return 92.5f; // Default ISS 400km LEO
        }

        /// <summary>
        /// Extension helper: Equips a ComponentPart into the matching slot on SpacecraftDesign.
        /// </summary>
        public static void EquipPart(this SpacecraftDesign design, ComponentPart part)
        {
            if (design == null || part == null) return;

            switch (part.Category)
            {
                case PartCategory.Chassis:
                    design.SelectedChassis = part;
                    break;
                case PartCategory.LaunchVehicle:
                    design.SelectedLaunchVehicle = part;
                    break;
                case PartCategory.Orbit:
                    design.SelectedOrbit = part;
                    break;
                case PartCategory.Payload:
                    design.SelectedPayload = part;
                    break;
                case PartCategory.SolarPanel:
                    design.SelectedSolarPanel = part;
                    break;
                case PartCategory.Battery:
                    design.SelectedBattery = part;
                    break;
                case PartCategory.ADCS:
                    design.SelectedADCS = part;
                    break;
                case PartCategory.Communications:
                    design.SelectedComms = part;
                    break;
                case PartCategory.GroundStation:
                    design.SelectedGroundStation = part;
                    break;
            }
        }

        /// <summary>
        /// Extension helper: Returns all non-null parts currently equipped in SpacecraftDesign.
        /// </summary>
        public static List<ComponentPart> GetAllSelectedParts(this SpacecraftDesign design)
        {
            List<ComponentPart> list = new List<ComponentPart>();
            if (design == null) return list;

            if (design.SelectedChassis != null) list.Add(design.SelectedChassis);
            if (design.SelectedLaunchVehicle != null) list.Add(design.SelectedLaunchVehicle);
            if (design.SelectedOrbit != null) list.Add(design.SelectedOrbit);
            if (design.SelectedPayload != null) list.Add(design.SelectedPayload);
            if (design.SelectedSolarPanel != null) list.Add(design.SelectedSolarPanel);
            if (design.SelectedBattery != null) list.Add(design.SelectedBattery);
            if (design.SelectedADCS != null) list.Add(design.SelectedADCS);
            if (design.SelectedComms != null) list.Add(design.SelectedComms);
            if (design.SelectedGroundStation != null) list.Add(design.SelectedGroundStation);

            return list;
        }

        /// <summary>
        /// Extension helper: Checks whether all 9 core slots in SpacecraftDesign are populated.
        /// </summary>
        public static bool IsCoreDesignComplete(this SpacecraftDesign design)
        {
            if (design == null) return false;
            return design.SelectedChassis != null
                && design.SelectedLaunchVehicle != null
                && design.SelectedOrbit != null
                && design.SelectedPayload != null
                && design.SelectedSolarPanel != null
                && design.SelectedBattery != null
                && design.SelectedADCS != null
                && design.SelectedComms != null
                && design.SelectedGroundStation != null;
        }
    }
}
