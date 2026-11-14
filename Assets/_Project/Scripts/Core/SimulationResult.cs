using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMissionGame.Core
{
    /// <summary>
    /// Represents a single row in the JPL CML 4 Red / Yellow / Green (RYG) Readiness Checklist
    /// across Science, Technical, Management, and Cost dimensions.
    /// </summary>
    [Serializable]
    public class ChecklistItem
    {
        public string category;        // "Science", "Technical", "Management", or "Cost"
        public string requirementName; // e.g., "Mass & Dispenser Margin", "FCC / NOAA Licensing"
        public RYGStatus status;       // Red, Yellow, or Green
        public string statusDetail;    // Educational explanation of why this item is Red/Yellow/Green

        public ChecklistItem() { }

        public ChecklistItem(string cat, string name, RYGStatus ryg, string detail)
        {
            category = cat;
            requirementName = name;
            status = ryg;
            statusDetail = detail;
        }
    }

    /// <summary>
    /// Complete evaluation output produced by Member 1's MissionCalculator.cs.
    /// Read by Member 2 (Live Resource Bars & 2D Orbit Visualizer) and Member 3 (Review Gate & Final Scorecard).
    /// </summary>
    [Serializable]
    public class SimulationResult
    {
        [Header("1. Mass & Volume Metrics (Launch & Chassis Limits)")]
        public float totalMassKg;
        public float maxAllowedMassKg;
        public float massMarginPercent;
        public RYGStatus massStatus;

        public float totalVolumeU;
        public float maxAllowedVolumeU;
        public RYGStatus volumeStatus;

        [Header("2. Power & Orbital Eclipse Metrics")]
        public float peakSolarGeneratedWatts;
        public float orbitAverageGeneratedWatts;
        public float totalPowerConsumedWatts;
        public float powerMarginPercent;

        public float totalBatteryCapacityWh;
        public float eclipseEnergyRequiredWh;
        public bool survivesEclipseOnBattery;
        public RYGStatus powerStatus;

        [Header("3. Budget & Cost Reserve Metrics")]
        public float hardwareCostThousands;
        public float testingCostThousands;
        public float totalCostThousands;
        public float budgetCapThousands;
        public float costReservePercent;
        public RYGStatus budgetStatus;

        [Header("4. Data Link, Science & Reliability Metrics")]
        public float totalDataGeneratedMBPerDay;
        public float effectiveDataDownlinkMBPerDay;
        public float dataDownlinkCoveragePercent;
        public RYGStatus dataStatus;

        public int totalScienceScore;
        public int targetScienceScore;
        public RYGStatus scienceStatus;

        public float overallReliabilityPercent;
        public RYGStatus reliabilityStatus;

        [Header("5. Phase 3: JPL CML 4 RYG Checklist Summary")]
        public List<ChecklistItem> cml4Checklist = new List<ChecklistItem>();
        public int greenCount;
        public int yellowCount;
        public int redCount;

        [Tooltip("Percentage of CML 4 checklist items marked Green (NASA CML 4 targets >= 80% Green).")]
        public float greenChecklistPercent;

        [Tooltip("True if critical pre-launch blockers exist (e.g., missing FCC/NOAA license, overweight, over budget).")]
        public bool isScrubbedBeforeLaunch;

        [Tooltip("True if the design has zero Red blockers and meets minimum CML 4 readiness.")]
        public bool passesCml4ReviewGate;

        [Header("6. Phase 4: 2D Launch & On-Orbit Simulation Sequence")]
        public bool stage1_SurvivedLaunchAscent;       // 1. Rocket Ascent & Vibration check
        public bool stage2_SurvivedEjectionAndDitl;    // 2. Dispenser Ejection & 45-min DITL Timer
        public bool stage3_SurvivedBurnWireDeployment; // 3. Solar Panel / Antenna Burn-Wire Deployment
        public bool stage4_SurvivedOrbitAndDownlink;   // 4. Orbiting, Eclipse Battery check & Ground Station Downlink

        [Tooltip("Step-by-step flight log messages for Member 2's 2D Orbit Simulation ticker.")]
        public List<string> flightLogTicker = new List<string>();

        [Header("7. Phase 5: Final NASA Debrief & Scorecard")]
        public MissionGrade finalGrade;
        public string headlineOutcome;

        [TextArea(2, 4)]
        public string debriefSummary;

        [Tooltip("Educational breakdown explaining WHY each subsystem succeeded or failed using NASA SE rules.")]
        public List<string> educationalDebriefNotes = new List<string>();
    }
}
