using System.Collections.Generic;
using UnityEngine;

namespace SpaceMissionGame.Core
{
    /// <summary>
    /// The "Brain" of the Space Mission Design Game (Member 1).
    /// Evaluates a player's SpacecraftDesign against a NASA MissionProfile using deterministic
    /// Systems Engineering trade-off formulas, GMAT orbital sunlight/eclipse ratios,
    /// NASA CubeSat 101 verification rules, and the JPL CML 4 Red/Yellow/Green (RYG) checklist.
    /// </summary>
    public static class MissionCalculator
    {
        /// <summary>
        /// Evaluates the current spacecraft design and returns a complete SimulationResult
        /// for Phase 2 (Live UI Bars), Phase 3 (CML 4 RYG Checklist), Phase 4 (2D Orbit Sim),
        /// and Phase 5 (Final NASA Debrief Scorecard).
        /// Safe to call at any time, even when parts are still being selected (null-safe).
        /// </summary>
        public static SimulationResult EvaluateMission(MissionProfile mission, SpacecraftDesign design)
        {
            SimulationResult result = new SimulationResult();

            if (mission == null) mission = new MissionProfile();
            if (design == null) design = new SpacecraftDesign();

            List<ComponentPart> equippedParts = design.GetAllEquippedParts();

            // =========================================================================
            // 1. MASS & VOLUME CALCULATIONS (Launch & Chassis Constraints)
            // =========================================================================
            float totalMass = 0f;
            float totalVolume = 0f;

            foreach (ComponentPart part in equippedParts)
            {
                totalMass += part.massKg;
                totalVolume += part.volumeUnitsU;
            }

            result.totalMassKg = totalMass;
            result.totalVolumeU = totalVolume;

            // Determine Max Allowed Mass from Chassis and Launch Dispenser
            float chassisMaxMass = design.selectedChassis != null ? design.selectedChassis.maxMassLimitKg : 1.33f;
            float launcherMaxMass = design.selectedLaunchVehicle != null ? design.selectedLaunchVehicle.maxMassLimitKg : chassisMaxMass;

            if (design.selectedChassis != null && design.selectedLaunchVehicle != null)
            {
                result.maxAllowedMassKg = Mathf.Min(chassisMaxMass, launcherMaxMass);
            }
            else if (design.selectedChassis != null)
            {
                result.maxAllowedMassKg = chassisMaxMass;
            }
            else if (design.selectedLaunchVehicle != null)
            {
                result.maxAllowedMassKg = launcherMaxMass;
            }
            else
            {
                result.maxAllowedMassKg = 1.33f;
            }

            result.maxAllowedVolumeU = design.selectedChassis != null ? design.selectedChassis.maxVolumeU : 1.0f;

            // Mass Margin (%) = ((Max - Actual) / Max) * 100
            result.massMarginPercent = result.maxAllowedMassKg > 0f
                ? ((result.maxAllowedMassKg - result.totalMassKg) / result.maxAllowedMassKg) * 100f
                : -100f;

            bool chassisFitsDispenser = true;
            if (design.selectedChassis != null && design.selectedLaunchVehicle != null)
            {
                chassisFitsDispenser = (int)design.selectedChassis.cubeSatSize <= (int)design.selectedLaunchVehicle.cubeSatSize;
            }

            if (design.selectedChassis == null || result.massMarginPercent < 0f || !chassisFitsDispenser)
            {
                result.massStatus = RYGStatus.Red;
            }
            else if (result.massMarginPercent < mission.requiredMassMarginPercent)
            {
                result.massStatus = RYGStatus.Yellow;
            }
            else
            {
                result.massStatus = RYGStatus.Green;
            }

            // Volume Status (U capacity)
            if (design.selectedChassis == null || result.totalVolumeU > result.maxAllowedVolumeU)
            {
                result.volumeStatus = RYGStatus.Red;
            }
            else if (result.totalVolumeU > result.maxAllowedVolumeU * 0.90f)
            {
                result.volumeStatus = RYGStatus.Yellow;
            }
            else
            {
                result.volumeStatus = RYGStatus.Green;
            }

            // =========================================================================
            // 2. POWER & ORBITAL ECLIPSE CALCULATIONS (GMAT Sunlight Ratio)
            // =========================================================================
            float peakSolarW = 0f;
            float powerConsumedW = 0f;
            float batteryWh = 0f;

            foreach (ComponentPart part in equippedParts)
            {
                peakSolarW += part.powerGeneratedWatts;
                powerConsumedW += part.powerConsumedWatts;
                batteryWh += part.batteryCapacityWh;
            }

            float sunlightFraction = design.selectedOrbit != null && design.selectedOrbit.sunlightFraction > 0f
                ? design.selectedOrbit.sunlightFraction
                : 0.60f;
            float orbitPeriodMins = design.selectedOrbit != null && design.selectedOrbit.orbitalPeriodMinutes > 0f
                ? design.selectedOrbit.orbitalPeriodMinutes
                : 92.5f;

            result.peakSolarGeneratedWatts = peakSolarW;
            result.orbitAverageGeneratedWatts = peakSolarW * sunlightFraction;
            result.totalPowerConsumedWatts = powerConsumedW;
            result.totalBatteryCapacityWh = batteryWh;

            // Power Margin (%) = ((OrbitAvgGenerated - Consumed) / OrbitAvgGenerated) * 100
            if (result.orbitAverageGeneratedWatts > 0f)
            {
                result.powerMarginPercent = ((result.orbitAverageGeneratedWatts - result.totalPowerConsumedWatts) / result.orbitAverageGeneratedWatts) * 100f;
            }
            else
            {
                result.powerMarginPercent = -100f;
            }

            // Eclipse Energy Required (Wh) = Power Consumed (W) * Eclipse Hours
            float eclipseHours = (orbitPeriodMins * (1f - sunlightFraction)) / 60f;
            result.eclipseEnergyRequiredWh = result.totalPowerConsumedWatts * eclipseHours;

            // Standard CubeSat Li-Ion safe depth of discharge check (batteryWh >= eclipseEnergyRequiredWh)
            result.survivesEclipseOnBattery = result.totalBatteryCapacityWh > 0f
                && result.totalBatteryCapacityWh >= result.eclipseEnergyRequiredWh;

            if (result.orbitAverageGeneratedWatts <= 0f || result.powerMarginPercent < 0f || !result.survivesEclipseOnBattery)
            {
                result.powerStatus = RYGStatus.Red;
            }
            else if (result.powerMarginPercent < mission.requiredPowerMarginPercent
                     || result.totalBatteryCapacityWh < result.eclipseEnergyRequiredWh * 1.25f)
            {
                result.powerStatus = RYGStatus.Yellow;
            }
            else
            {
                result.powerStatus = RYGStatus.Green;
            }

            // =========================================================================
            // 3. BUDGET & COST RESERVE CALCULATIONS
            // =========================================================================
            float hardwareCost = 0f;
            foreach (ComponentPart part in equippedParts)
            {
                hardwareCost += part.costThousands;
            }

            result.hardwareCostThousands = hardwareCost;
            result.testingCostThousands = design.additionalTestingCostThousands;
            result.totalCostThousands = hardwareCost + design.additionalTestingCostThousands;
            result.budgetCapThousands = mission.budgetCapThousands;

            result.costReservePercent = mission.budgetCapThousands > 0f
                ? ((mission.budgetCapThousands - result.totalCostThousands) / mission.budgetCapThousands) * 100f
                : -100f;

            if (result.totalCostThousands > mission.budgetCapThousands)
            {
                result.budgetStatus = RYGStatus.Red;
            }
            else if (result.costReservePercent < mission.requiredCostReservePercent)
            {
                result.budgetStatus = RYGStatus.Yellow;
            }
            else
            {
                result.budgetStatus = RYGStatus.Green;
            }

            // =========================================================================
            // 4. TELECOM DATA LINK, ADCS POINTING, SCIENCE & RELIABILITY
            // =========================================================================
            float dataGenMB = 0f;
            int rawScienceScore = 0;
            bool requiresNoaaLicense = false;
            bool requiresFccLicense = false;
            bool hasBurnWireParts = false;
            bool hasOutgassingSensitiveOptics = false;
            float strictestPayloadPointingReq = 0f;

            foreach (ComponentPart part in equippedParts)
            {
                dataGenMB += part.dataGeneratedMBPerDay;
                rawScienceScore += part.scienceScoreBonus;

                if (part.requiresNoaaCameraLicense) requiresNoaaLicense = true;
                if (part.requiresFccRfLicense) requiresFccLicense = true;
                if (part.usesDeployableBurnWire) hasBurnWireParts = true;
                if (part.sensitiveToOutgassing) hasOutgassingSensitiveOptics = true;

                if (part.category == PartCategory.Payload && part.pointingAccuracyDeg > 0f)
                {
                    if (strictestPayloadPointingReq <= 0f || part.pointingAccuracyDeg < strictestPayloadPointingReq)
                    {
                        strictestPayloadPointingReq = part.pointingAccuracyDeg;
                    }
                }
            }

            float requiredPointingDeg = mission.maxAllowedPointingErrorDeg > 0f
                ? mission.maxAllowedPointingErrorDeg
                : strictestPayloadPointingReq;

            float commsCap = design.selectedComms != null ? design.selectedComms.dataDownlinkMBPerDay : 0f;
            float groundCap = design.selectedGroundStation != null ? design.selectedGroundStation.dataDownlinkMBPerDay : 0f;

            result.totalDataGeneratedMBPerDay = dataGenMB;
            result.effectiveDataDownlinkMBPerDay = Mathf.Min(commsCap, groundCap);

            if (result.totalDataGeneratedMBPerDay > 0f)
            {
                result.dataDownlinkCoveragePercent = Mathf.Clamp(
                    (result.effectiveDataDownlinkMBPerDay / result.totalDataGeneratedMBPerDay) * 100f,
                    0f, 100f);
            }
            else
            {
                result.dataDownlinkCoveragePercent = 0f;
            }

            // Deep Space / Lunar Comms Compatibility Check
            bool deepSpaceCommsValid = true;
            if (mission.requiresDeepSpaceCommsAndRadProtection || mission.requiredOrbit == OrbitType.Lunar_Orbit)
            {
                bool hasXBand = design.selectedComms != null && design.selectedComms.partId == "comms_xband_iris";
                bool hasDsn = design.selectedGroundStation != null && design.selectedGroundStation.partId == "ground_nasa_dsn";
                deepSpaceCommsValid = hasXBand && hasDsn;
            }

            if (design.selectedComms == null || design.selectedGroundStation == null
                || result.dataDownlinkCoveragePercent < 70f || !deepSpaceCommsValid)
            {
                result.dataStatus = RYGStatus.Red;
            }
            else if (result.dataDownlinkCoveragePercent < 100f)
            {
                result.dataStatus = RYGStatus.Yellow;
            }
            else
            {
                result.dataStatus = RYGStatus.Green;
            }

            // Check ADCS Pointing Accuracy & Lunar Magnetic Field limitation
            bool adcsPointingValid = true;
            bool magnetorquerInLunarOrbitError = false;
            if (design.selectedAdcs == null)
            {
                adcsPointingValid = false;
            }
            else
            {
                if (requiredPointingDeg > 0f && design.selectedAdcs.pointingAccuracyDeg > requiredPointingDeg)
                {
                    adcsPointingValid = false;
                }
                if (mission.requiredOrbit == OrbitType.Lunar_Orbit && design.selectedAdcs.partId != "adcs_reaction_wheels")
                {
                    magnetorquerInLunarOrbitError = true;
                    adcsPointingValid = false;
                }
            }

            // Calculate Effective Science Score (penalized if data bottlenecked or pointing blurry)
            float scienceMultiplier = 1.0f;
            if (result.totalDataGeneratedMBPerDay > 0f && result.dataDownlinkCoveragePercent < 100f)
            {
                scienceMultiplier *= (result.dataDownlinkCoveragePercent / 100f);
            }
            if (!adcsPointingValid)
            {
                scienceMultiplier *= 0.65f; // Blurry images / off-target spectrometer reduces science return
            }

            result.totalScienceScore = Mathf.Max(0, Mathf.RoundToInt(rawScienceScore * scienceMultiplier) + design.dilemmaScienceModifier);
            result.targetScienceScore = mission.targetScienceScore;

            if (design.selectedPayloads.Count == 0 || result.totalScienceScore < mission.targetScienceScore * 0.75f)
            {
                result.scienceStatus = RYGStatus.Red;
            }
            else if (result.totalScienceScore < mission.targetScienceScore)
            {
                result.scienceStatus = RYGStatus.Yellow;
            }
            else
            {
                result.scienceStatus = RYGStatus.Green;
            }

            // Calculate Overall Reliability (%)
            float sumReliability = 0f;
            if (equippedParts.Count > 0)
            {
                foreach (ComponentPart part in equippedParts)
                {
                    sumReliability += part.baseReliabilityPercent;
                }
                sumReliability /= equippedParts.Count;
            }
            else
            {
                sumReliability = 0f;
            }

            // Phase 3 Testing & Redundancy Modifiers (CubeSat 101 rules)
            float testBonus = 0f;
            testBonus += design.completedVibrationTest ? 3.0f : -8.0f;
            testBonus += design.completedDitlSoftwareTest ? 2.5f : -7.0f;

            if (hasOutgassingSensitiveOptics)
            {
                testBonus += design.completedTvacBakeoutTest ? 3.0f : -10.0f;
            }
            else if (design.completedTvacBakeoutTest)
            {
                testBonus += 1.5f;
            }

            if (hasBurnWireParts)
            {
                testBonus += design.hasDoubleBurnWireRedundancy ? 3.0f : -6.0f;
            }

            if (!design.hasCertifiedBatteriesAndInhibits)
            {
                testBonus -= 15.0f;
            }

            result.overallReliabilityPercent = equippedParts.Count > 0
                ? Mathf.Clamp(sumReliability + testBonus + design.dilemmaReliabilityModifier, 0f, 99.5f)
                : 0f;

            if (result.overallReliabilityPercent < mission.minReliabilityPercent - 10f)
            {
                result.reliabilityStatus = RYGStatus.Red;
            }
            else if (result.overallReliabilityPercent < mission.minReliabilityPercent)
            {
                result.reliabilityStatus = RYGStatus.Yellow;
            }
            else
            {
                result.reliabilityStatus = RYGStatus.Green;
            }

            // =========================================================================
            // 5. PHASE 3: JPL CML 4 RED / YELLOW / GREEN (RYG) CHECKLIST
            // =========================================================================
            BuildCml4Checklist(
                mission,
                design,
                result,
                chassisFitsDispenser,
                adcsPointingValid,
                magnetorquerInLunarOrbitError,
                deepSpaceCommsValid,
                requiresFccLicense,
                requiresNoaaLicense,
                hasBurnWireParts,
                hasOutgassingSensitiveOptics,
                requiredPointingDeg);

            // =========================================================================
            // 6. PHASE 4 & 5: FLIGHT SIMULATION SEQUENCE & FINAL NASA SCORECARD
            // =========================================================================
            SimulateFlightAndDebrief(
                mission,
                design,
                result,
                chassisFitsDispenser,
                adcsPointingValid,
                magnetorquerInLunarOrbitError,
                deepSpaceCommsValid,
                requiresFccLicense,
                requiresNoaaLicense,
                hasBurnWireParts,
                hasOutgassingSensitiveOptics);

            return result;
        }

        private static void BuildCml4Checklist(
            MissionProfile mission,
            SpacecraftDesign design,
            SimulationResult result,
            bool chassisFitsDispenser,
            bool adcsPointingValid,
            bool magnetorquerInLunarOrbitError,
            bool deepSpaceCommsValid,
            bool requiresFccLicense,
            bool requiresNoaaLicense,
            bool hasBurnWireParts,
            bool hasOutgassingSensitiveOptics,
            float requiredPointingDeg)
        {
            result.cml4Checklist.Clear();

            // 1. [Science] Science Traceability & Score
            string sciDetail = $"Projected Science Return: {result.totalScienceScore} / {result.targetScienceScore} pts.";
            if (design.selectedUserSegment == null)
            {
                sciDetail += " (Missing User Segment data plan!)";
            }
            result.cml4Checklist.Add(new ChecklistItem(
                "Science",
                "Science Traceability & Return",
                 design.selectedUserSegment == null && result.scienceStatus == RYGStatus.Green ? RYGStatus.Yellow : result.scienceStatus,
                sciDetail));

            // 2. [Science] Orbit & Launch Trajectory Match
            bool orbitMatches = design.selectedOrbit != null && design.selectedOrbit.orbitType == mission.requiredOrbit;
            bool launcherMatchesOrbit = design.selectedLaunchVehicle != null
                && design.selectedOrbit != null
                && design.selectedLaunchVehicle.orbitType == design.selectedOrbit.orbitType;

            RYGStatus orbitRyg = (orbitMatches && launcherMatchesOrbit) ? RYGStatus.Green : RYGStatus.Red;
            string orbitDetail = (orbitMatches && launcherMatchesOrbit)
                ? $"Matched target {mission.requiredOrbit} with compatible Launch Vehicle."
                : "Mismatch between required Mission Orbit, selected GMAT Orbit, or Launch Vehicle trajectory!";

            result.cml4Checklist.Add(new ChecklistItem("Science", "Orbit & Launch Trajectory Match", orbitRyg, orbitDetail));

            // 3. [Technical] Mass, Volume & Dispenser Margin
            RYGStatus massVolRyg = (result.massStatus == RYGStatus.Red || result.volumeStatus == RYGStatus.Red)
                ? RYGStatus.Red
                : (result.massStatus == RYGStatus.Yellow || result.volumeStatus == RYGStatus.Yellow ? RYGStatus.Yellow : RYGStatus.Green);

            string massDetail = !chassisFitsDispenser
                ? "Chassis form factor is too large for the selected Launch Dispenser!"
                : $"Mass: {result.totalMassKg:F2}/{result.maxAllowedMassKg:F2} kg ({result.massMarginPercent:F1}% margin, req >= {mission.requiredMassMarginPercent:F0}%). Volume: {result.totalVolumeU:F1}/{result.maxAllowedVolumeU:F1}U.";

            result.cml4Checklist.Add(new ChecklistItem("Technical", "Mass, Volume & Dispenser Margin", massVolRyg, massDetail));

            // 4. [Technical] Orbital Power & Eclipse Battery Balance
            string powerDetail = $"Orbit-Avg Power: {result.orbitAverageGeneratedWatts:F1}W vs {result.totalPowerConsumedWatts:F1}W draw ({result.powerMarginPercent:F1}% margin). "
                               + $"Eclipse Battery: {result.totalBatteryCapacityWh:F1}Wh vs {result.eclipseEnergyRequiredWh:F1}Wh needed.";
            result.cml4Checklist.Add(new ChecklistItem("Technical", "Orbital Power & Eclipse Balance", result.powerStatus, powerDetail));

            // 5. [Technical] ADCS Pointing & Telecom Data Link
            RYGStatus adcsCommsRyg = result.dataStatus;
            string adcsCommsDetail = $"Downlink: {result.effectiveDataDownlinkMBPerDay:F0} MB/day vs {result.totalDataGeneratedMBPerDay:F0} MB/day generated ({result.dataDownlinkCoveragePercent:F0}%).";

            if (!adcsPointingValid)
            {
                adcsCommsRyg = RYGStatus.Red;
                adcsCommsDetail = magnetorquerInLunarOrbitError
                    ? "Magnetorquers/Magnets cannot orient a spacecraft in Lunar Orbit (no strong magnetic field)! Use 3-Axis Reaction Wheels."
                    : $"ADCS pointing ({design.selectedAdcs?.pointingAccuracyDeg ?? 99f:F1}°) fails mission/camera requirement (<= {requiredPointingDeg:F1}°)!";
            }
            else if (!deepSpaceCommsValid)
            {
                adcsCommsRyg = RYGStatus.Red;
                adcsCommsDetail = "Lunar distance requires Iris X-Band Transponder paired with the NASA Deep Space Network (DSN)!";
            }

            result.cml4Checklist.Add(new ChecklistItem("Technical", "ADCS Pointing & Telecom Link", adcsCommsRyg, adcsCommsDetail));

            // 6. [Management] Regulatory Licensing (FCC/IARU & NOAA)
            bool fccOk = !requiresFccLicense || design.hasFccRfLicense;
            bool noaaOk = !requiresNoaaLicense || design.hasNoaaCameraLicense;
            RYGStatus licenseRyg = (fccOk && noaaOk) ? RYGStatus.Green : RYGStatus.Red;

            string licenseDetail;
            if (!fccOk && !noaaOk)
                licenseDetail = "CRITICAL: Missing both FCC RF License and NOAA Remote Sensing Camera License! Spacecraft will be demanifested.";
            else if (!fccOk)
                licenseDetail = "CRITICAL: Missing FCC / IARU Radio Frequency License! Spacecraft cannot legally transmit.";
            else if (!noaaOk)
                licenseDetail = "CRITICAL: Earth-imaging camera requires a NOAA Remote Sensing License before launch!";
            else
                licenseDetail = "All required FCC RF and NOAA Remote Sensing licenses approved.";

            result.cml4Checklist.Add(new ChecklistItem("Management", "Regulatory Licensing (FCC / NOAA)", licenseRyg, licenseDetail));

            // 7. [Management] Environmental Verification & Hardware Safety (CubeSat 101)
            bool vibeOk = design.completedVibrationTest;
            bool ditlOk = design.completedDitlSoftwareTest;
            bool tvacOk = !hasOutgassingSensitiveOptics || design.completedTvacBakeoutTest;
            bool burnWireOk = !hasBurnWireParts || design.hasDoubleBurnWireRedundancy;

            RYGStatus testRyg;
            string testDetail;
            if (vibeOk && ditlOk && tvacOk && burnWireOk && design.hasCertifiedBatteriesAndInhibits)
            {
                testRyg = RYGStatus.Green;
                testDetail = "All CubeSat 101 verification tests (Vibration, TVAC, DITL) and burn-wire redundancies verified.";
            }
            else if (!vibeOk || !ditlOk || !design.hasCertifiedBatteriesAndInhibits)
            {
                testRyg = RYGStatus.Red;
                testDetail = "Missing critical Vibration or Day-In-The-Life (DITL) software testing (high risk of launch/deployment failure)!";
            }
            else
            {
                testRyg = RYGStatus.Yellow;
                testDetail = !tvacOk
                    ? "Warning: Optical payload lacks TVAC Bakeout (outgassing will fog camera lens in vacuum!)."
                    : "Warning: Deployable solar/antenna uses a single burn wire without redundancy.";
            }

            result.cml4Checklist.Add(new ChecklistItem("Management", "CubeSat 101 Verification & Testing", testRyg, testDetail));

            // 8. [Cost] Budget Cap & JPL CML 4 Cost Reserve
            string costDetail = $"Total Cost: ${result.totalCostThousands:F0}k / ${result.budgetCapThousands:F0}k Cap ({result.costReservePercent:F1}% reserve, req >= {mission.requiredCostReservePercent:F0}%).";
            result.cml4Checklist.Add(new ChecklistItem("Cost", "Budget Cap & Cost Reserve Margin", result.budgetStatus, costDetail));

            // Tally Green / Yellow / Red counts
            int green = 0, yellow = 0, red = 0;
            foreach (ChecklistItem item in result.cml4Checklist)
            {
                if (item.status == RYGStatus.Green) green++;
                else if (item.status == RYGStatus.Yellow) yellow++;
                else red++;
            }

            result.greenCount = green;
            result.yellowCount = yellow;
            result.redCount = red;
            result.greenChecklistPercent = (green / (float)result.cml4Checklist.Count) * 100f;

            // Pre-launch scrub check: missing licenses, overweight, over volume, wrong launcher/orbit, or over budget
            result.isScrubbedBeforeLaunch = !design.IsCoreDesignComplete()
                || !fccOk
                || !noaaOk
                || result.totalMassKg > result.maxAllowedMassKg
                || result.totalVolumeU > result.maxAllowedVolumeU
                || !chassisFitsDispenser
                || result.totalCostThousands > result.budgetCapThousands
                || !orbitMatches
                || !launcherMatchesOrbit;

            result.passesCml4ReviewGate = !result.isScrubbedBeforeLaunch && result.redCount == 0;
        }

        private static void SimulateFlightAndDebrief(
            MissionProfile mission,
            SpacecraftDesign design,
            SimulationResult result,
            bool chassisFitsDispenser,
            bool adcsPointingValid,
            bool magnetorquerInLunarOrbitError,
            bool deepSpaceCommsValid,
            bool requiresFccLicense,
            bool requiresNoaaLicense,
            bool hasBurnWireParts,
            bool hasOutgassingSensitiveOptics)
        {
            result.flightLogTicker.Clear();
            result.educationalDebriefNotes.Clear();

            // Check Pre-Launch Scrub (Demanifested at Launch Pad)
            if (result.isScrubbedBeforeLaunch)
            {
                result.stage1_SurvivedLaunchAscent = false;
                result.stage2_SurvivedEjectionAndDitl = false;
                result.stage3_SurvivedBurnWireDeployment = false;
                result.stage4_SurvivedOrbitAndDownlink = false;

                result.finalGrade = MissionGrade.MissionScrubbed;
                result.headlineOutcome = "MISSION SCRUBBED — DEMANIFESTED AT PRE-LAUNCH REVIEW";
                result.flightLogTicker.Add("[T-00:10:00] NASA Launch Integration Authority halted spacecraft integration.");

                if (requiresFccLicense && !design.hasFccRfLicense)
                {
                    result.flightLogTicker.Add("[SCRUB] Missing FCC / IARU RF License. Unlicensed radio transmitters are barred from launch.");
                    result.educationalDebriefNotes.Add("CubeSat 101 Rule: Every CubeSat with a radio transmitter must secure an FCC/IARU frequency allocation license before integration, or it will be demanifested.");
                }
                if (requiresNoaaLicense && !design.hasNoaaCameraLicense)
                {
                    result.flightLogTicker.Add("[SCRUB] Missing NOAA Remote Sensing License for Earth-imaging camera payload.");
                    result.educationalDebriefNotes.Add("CubeSat 101 Rule: Any spacecraft carrying a camera capable of imaging Earth requires a NOAA license prior to launch.");
                }
                if (result.totalMassKg > result.maxAllowedMassKg || result.totalVolumeU > result.maxAllowedVolumeU || !chassisFitsDispenser)
                {
                    result.flightLogTicker.Add($"[SCRUB] Physical dispenser violation: {result.totalMassKg:F2}/{result.maxAllowedMassKg:F2} kg, {result.totalVolumeU:F1}/{result.maxAllowedVolumeU:F1}U.");
                    result.educationalDebriefNotes.Add("CubeSat 101 Rule: CubeSats must strictly fit within P-POD / NanoRacks / 6U dispenser mass and volume limits to protect the primary rocket payload.");
                }
                if (result.totalCostThousands > result.budgetCapThousands)
                {
                    result.flightLogTicker.Add($"[SCRUB] Cost overrun: Spent ${result.totalCostThousands:F0}k against ${result.budgetCapThousands:F0}k grant cap.");
                    result.educationalDebriefNotes.Add("JPL CML 4 Rule: Exceeding the NASA mission budget cap results in cancellation prior to Phase C/D.");
                }
                if (design.selectedOrbit == null || design.selectedOrbit.orbitType != mission.requiredOrbit
                    || design.selectedLaunchVehicle == null || design.selectedLaunchVehicle.orbitType != design.selectedOrbit.orbitType)
                {
                    result.flightLogTicker.Add("[SCRUB] Selected Launch Vehicle or GMAT Orbit does not match the target science mission trajectory.");
                    result.educationalDebriefNotes.Add("Systems Engineering Rule: Your Launch Segment and Orbit must deliver the Space Segment to the regime required by the science objectives.");
                }

                result.debriefSummary = "Your spacecraft was scrubbed before liftoff due to regulatory, budget, or physical integration violations.";
                return;
            }

            // =========================================================================
            // STAGE 1: Rocket Ascent & Vibration Survival
            // =========================================================================
            if (!design.completedVibrationTest || !design.hasCertifiedBatteriesAndInhibits)
            {
                result.stage1_SurvivedLaunchAscent = false;
                result.stage2_SurvivedEjectionAndDitl = false;
                result.stage3_SurvivedBurnWireDeployment = false;
                result.stage4_SurvivedOrbitAndDownlink = false;

                result.finalGrade = MissionGrade.LostInSpace;
                result.headlineOutcome = "LOST IN SPACE — STRUCTURAL / ELECTRICAL FAILURE DURING LAUNCH";
                result.flightLogTicker.Add("[T+00:02:15] Max-Q acoustic and vibration loads reached during rocket ascent.");
                result.flightLogTicker.Add("[FAILURE] Unverified solder joints fractured under launch vibration (Vibration Test was skipped!).");
                result.educationalDebriefNotes.Add("CubeSat 101 Rule: Environmental Vibration & Shock testing is essential before flight to ensure connectors and batteries survive rocket ascent loads.");
                result.debriefSummary = "Your spacecraft launched, but suffered an internal electrical failure during rocket ascent because Vibration Testing was skipped.";
                return;
            }

            result.stage1_SurvivedLaunchAscent = true;
            result.flightLogTicker.Add("[T+00:08:30] Stage 1 Passed: Spacecraft survived rocket ascent vibration and reached target insertion altitude.");

            // =========================================================================
            // STAGE 2: Dispenser Ejection & 45-Minute DITL Timer
            // =========================================================================
            if (!design.completedDitlSoftwareTest)
            {
                result.stage2_SurvivedEjectionAndDitl = false;
                result.stage3_SurvivedBurnWireDeployment = false;
                result.stage4_SurvivedOrbitAndDownlink = false;

                result.finalGrade = MissionGrade.LostInSpace;
                result.headlineOutcome = "LOST IN SPACE — FLIGHT SOFTWARE & DEPLOYMENT TIMER BUG";
                result.flightLogTicker.Add("[T+00:45:00] CubeSat ejected from dispenser. Separation switches released.");
                result.flightLogTicker.Add("[FAILURE] Flight software entered an infinite boot-loop during the 45-minute post-ejection timer (DITL Test skipped!).");
                result.educationalDebriefNotes.Add("CubeSat 101 Rule: A Day-In-The-Life (DITL) software test verifies that the 30-to-45-minute post-ejection wait timer and beacon sequence work autonomously.");
                result.debriefSummary = "After ejection from the dispenser, an untested flight software timer bug prevented the spacecraft from waking up.";
                return;
            }

            result.stage2_SurvivedEjectionAndDitl = true;
            result.flightLogTicker.Add("[T+00:45:00] Stage 2 Passed: Ejected smoothly from dispenser; 45-minute DITL inhibit timer completed.");

            // =========================================================================
            // STAGE 3: Solar Panel & Antenna Burn-Wire Deployment
            // =========================================================================
            if (hasBurnWireParts && !design.hasDoubleBurnWireRedundancy && result.massMarginPercent < mission.requiredMassMarginPercent)
            {
                result.stage3_SurvivedBurnWireDeployment = false;
                result.stage4_SurvivedOrbitAndDownlink = false;

                result.finalGrade = MissionGrade.LostInSpace;
                result.headlineOutcome = "LOST IN SPACE — BURN-WIRE DEPLOYMENT FAILURE";
                result.flightLogTicker.Add("[T+00:46:00] Commanding Nichrome resistor burn wire to release stowed mechanisms...");
                result.flightLogTicker.Add("[FAILURE] Single burn wire failed to sever the Vectran tie-down line in cold vacuum!");
                result.educationalDebriefNotes.Add("CubeSat 101 Rule: Deployable solar panels and antennas held by fishing line should use a Double Burn Wire redundancy to prevent single-point deployment failures.");
                result.debriefSummary = "Your deployable mechanism jammed in orbit because a single non-redundant burn wire failed to cut the tie-down cord.";
                return;
            }

            result.stage3_SurvivedBurnWireDeployment = true;
            result.flightLogTicker.Add("[T+00:47:00] Stage 3 Passed: Burn-wire release succeeded; solar arrays and antennas deployed.");

            // =========================================================================
            // STAGE 4: On-Orbit Science, Eclipse Survival & Ground Station Downlink
            // =========================================================================
            if (result.powerStatus == RYGStatus.Red)
            {
                result.stage4_SurvivedOrbitAndDownlink = false;
                result.finalGrade = MissionGrade.LostInSpace;
                result.headlineOutcome = "LOST IN SPACE — ORBITAL ECLIPSE BATTERY BROWN-OUT";
                result.flightLogTicker.Add("[ORBIT 1 - ECLIPSE] Entering planetary shadow. Switching to Li-Ion battery bus...");
                result.flightLogTicker.Add($"[FAILURE] Power deficit! Consuming {result.totalPowerConsumedWatts:F1}W with only {result.orbitAverageGeneratedWatts:F1}W orbit-avg generation and {result.totalBatteryCapacityWh:F1}Wh battery ({result.eclipseEnergyRequiredWh:F1}Wh needed for eclipse).");
                result.educationalDebriefNotes.Add("GMAT & Power Rule: Solar panels only generate power during the sunlit fraction of the orbit. Your orbit-average solar generation and battery capacity (Wh) must cover continuous power draw through eclipse!");
                result.debriefSummary = "Your spacecraft operated briefly in sunlight, but its batteries drained completely during the first orbital eclipse.";
                return;
            }

            if (!deepSpaceCommsValid || result.effectiveDataDownlinkMBPerDay <= 0f)
            {
                result.stage4_SurvivedOrbitAndDownlink = false;
                result.finalGrade = MissionGrade.LostInSpace;
                result.headlineOutcome = "LOST IN SPACE — TELECOM LINK FAILURE (NO SIGNAL RECEIVED)";
                result.flightLogTicker.Add("[ORBIT 2 - PASS] Attempting telemetry downlink to Ground Segment...");
                result.flightLogTicker.Add("[FAILURE] Ground station could not close the RF link at orbital distance!");
                result.educationalDebriefNotes.Add("NASA Telecom Rule: Deep-space Lunar missions require high-gain X-Band transponders paired with 34-meter NASA Deep Space Network dishes to close the link budget.");
                result.debriefSummary = "Your spacecraft survived orbit insertion, but its radio signal was too weak to reach the selected Ground Station.";
                return;
            }

            // Survived all 4 stages! Now evaluate science quality & engineering margins for S / A / B / C grade.
            result.stage4_SurvivedOrbitAndDownlink = true;
            result.flightLogTicker.Add($"[ORBIT 3 - SUNLIGHT] Collecting science payload data ({result.totalDataGeneratedMBPerDay:F0} MB/day) and charging batteries.");
            result.flightLogTicker.Add($"[ORBIT 4 - ECLIPSE] Survived eclipse on battery ({result.eclipseEnergyRequiredWh:F1} / {result.totalBatteryCapacityWh:F1} Wh used).");
            result.flightLogTicker.Add($"[ORBIT 5 - DOWNLINK] Beamed {result.effectiveDataDownlinkMBPerDay:F0} MB/day to {design.selectedGroundStation.partName}.");

            bool outgassingPenalty = hasOutgassingSensitiveOptics && !design.completedTvacBakeoutTest;
            if (outgassingPenalty)
            {
                result.flightLogTicker.Add("[WARNING] Camera lens fogged by adhesive outgassing in vacuum (TVAC Bakeout was skipped!). Science reduced by 30%.");
                result.totalScienceScore = Mathf.RoundToInt(result.totalScienceScore * 0.70f);
                result.educationalDebriefNotes.Add("CubeSat 101 Rule: Without a Thermal Vacuum (TVAC) bakeout test, volatile gases from plastics/epoxies condense onto cold camera lenses in space ('outgassing'), degrading optical science!");
            }

            if (!adcsPointingValid)
            {
                result.flightLogTicker.Add("[WARNING] ADCS pointing jitter caused blurry imagery / off-target spectroscopy. Science reduced.");
                result.educationalDebriefNotes.Add(magnetorquerInLunarOrbitError
                    ? "ADCS Rule: Magnetorquers rely on Earth's magnetosphere and cannot stabilize a spacecraft at the Moon! Use 3-Axis Reaction Wheels & Star Tracker."
                    : "ADCS Rule: High-resolution cameras and spectrometers require active 3-axis attitude control (Magnetorquers or Reaction Wheels) to stay pointed at their target.");
            }

            if (result.dataDownlinkCoveragePercent < 100f)
            {
                result.flightLogTicker.Add($"[WARNING] Telecom bottleneck: Only {result.dataDownlinkCoveragePercent:F0}% of generated science data could be downlinked daily.");
                result.educationalDebriefNotes.Add("Ground & Comms Segment Rule: Your onboard transceiver and ground station dish must match the daily data volume (MB/day) produced by your science payload.");
            }

            if (result.massStatus == RYGStatus.Green && result.powerStatus == RYGStatus.Green && result.budgetStatus == RYGStatus.Green)
            {
                result.educationalDebriefNotes.Add($"JPL CML 4 Excellence: Maintained healthy technical margins (+{result.massMarginPercent:F1}% Mass, +{result.powerMarginPercent:F1}% Power, +{result.costReservePercent:F1}% Cost Reserve).");
            }
            else
            {
                result.educationalDebriefNotes.Add($"JPL CML 4 Margin Note: Some technical margins were tighter than NASA's recommended thresholds (Mass: {result.massMarginPercent:F1}%, Power: {result.powerMarginPercent:F1}%, Cost Reserve: {result.costReservePercent:F1}%).");
            }

            // Assign Final Grade (S / A / B / C)
            if (result.greenChecklistPercent >= 87.5f
                && result.redCount == 0
                && !outgassingPenalty
                && adcsPointingValid
                && result.totalScienceScore >= result.targetScienceScore)
            {
                if (result.greenChecklistPercent >= 99f && result.totalScienceScore >= result.targetScienceScore + 10)
                {
                    result.finalGrade = MissionGrade.S;
                    result.headlineOutcome = "GRADE S — FLAWLESS NASA JPL CML 4 MISSION ARCHITECTURE!";
                    result.debriefSummary = "Outstanding systems engineering! Your CubeSat met every NASA CML 4 margin, passed all environmental verification tests, and exceeded science goals.";
                }
                else
                {
                    result.finalGrade = MissionGrade.A;
                    result.headlineOutcome = "GRADE A — FULL MISSION SUCCESS (CML 4 COMPLIANT)";
                    result.debriefSummary = "Great job! Your mission achieved primary science objectives with compliant NASA technical margins and >= 80% Green CML 4 readiness.";
                }
            }
            else if (result.totalScienceScore >= result.targetScienceScore * 0.80f && !outgassingPenalty && adcsPointingValid)
            {
                result.finalGrade = MissionGrade.B;
                result.headlineOutcome = "GRADE B — PARTIAL MISSION SUCCESS (TIGHT MARGINS)";
                result.debriefSummary = "Your spacecraft survived orbit and returned useful science data, though tight technical margins or downlink bottlenecks limited full potential.";
            }
            else
            {
                result.finalGrade = MissionGrade.C;
                result.headlineOutcome = "GRADE C — DEGRADED SCIENCE RETURN (ENGINEERING DEFICIENCIES)";
                result.debriefSummary = "Your spacecraft survived in orbit, but outgassing contamination, pointing errors, or telecom bottlenecks severely reduced science return.";
            }
        }
    }
}
