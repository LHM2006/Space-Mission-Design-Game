using System;
using System.Collections.Generic;

namespace SpaceMissionGame.Core
{
    // =========================================================================
    // 1. ENUMS (Aligned with JPL CMLs, NASA Mission Segments & CubeSat 101)
    // =========================================================================

    /// <summary>
    /// Maps directly to the 5-Phase Gameplay Loop & JPL Concept Maturity Levels (13-3547_A1b.pdf).
    /// Controlled by Member 3's GameManager.
    /// </summary>
    public enum GamePhase
    {
        MainMenu,
        CML1_MissionSelect,       // Phase 1 (Cocktail Napkin): Pick a NASA Science Mission
        CML2_3_SpacecraftBuilder, // Phase 2 (Initial Feasibility & Trade Space): Pick Parts & Segments
        CML4_ReviewAndTesting,    // Phase 3 (Point Design): Licensing, Environmental Tests & Dilemma Cards
        PhaseCD_OrbitSimulation,  // Phase 4 (Implementation): 2D Launch & On-Orbit Simulation
        PostMission_Scorecard     // Phase 5 (Debrief): Final Grade & NASA CML RYG Scorecard
    }

    /// <summary>
    /// Maps to the 4 NASA Science Mission Segments (250323-design-and-develop-science-mission.pdf).
    /// </summary>
    public enum MissionSegment
    {
        Launch, // Rocket / Deployer & CubeSat Form Factor (1U, 3U, 6U)
        Space,  // Spacecraft Bus (Solar, Battery, ADCS, Comms) & Science Payload
        Ground, // Ground Station Antenna & Mission Control
        User    // Science Data Processing & Research Output
    }

    /// <summary>
    /// Categories of selectable parts in the Spacecraft Builder.
    /// </summary>
    public enum PartCategory
    {
        Chassis,        // 1U (1.33kg max), 3U (4.0kg max), 6U (12.0kg max)
        LaunchVehicle,  // ISS Cargo Resupply, Rideshare LEO, Dedicated Lunar Rocket
        Orbit,          // ISS Orbit (400km), Sun-Synchronous Polar (600km), Lunar Orbit (GMAT presets)
        Payload,        // BioNutrients Incubator, Earth Camera, Lunar Spectrometer, etc.
        SolarPanel,     // Body-Mounted Solar vs. Deployable Solar Array
        Battery,        // Standard Li-Ion (UL Listed) vs. High-Capacity Solid-State vs. Uncertified DIY
        ADCS,           // Passive Magnets, Magnetorquers, 3-Axis Reaction Wheels + Star Tracker
        Communications, // Amateur UHF (437 MHz), S-Band High-Speed, X-Band Deep Space
        GroundStation   // Amateur Yagi Antenna, University Dish, NASA Near Space Network
    }

    /// <summary>
    /// Red / Yellow / Green Status from JPL CML 4 Checklist (13-3547_A1b.pdf, Page 11-14).
    /// Green = Completed/Compliant, Yellow = Marginal/Risky, Red = Deficient/Failed.
    /// </summary>
    public enum RYGStatus
    {
        Red,
        Yellow,
        Green
    }

    /// <summary>
    /// Mission Difficulty & NASA Mission Class.
    /// </summary>
    public enum MissionDifficulty
    {
        Easy_ISSCubeSat,       // Mission 1: BioNutrients-2 on ISS
        Medium_EarthObserver,  // Mission 2: BisonSat 3U Earth Imager
        Hard_LunarPathfinder   // Mission 3: 6U Artemis Lunar Ice CubeSat
    }

    // =========================================================================
    // 2. DATA MODELS (OOP Classes shared across Member 1, Member 2, Member 3)
    // =========================================================================

    /// <summary>
    /// Represents a single selectable spacecraft component, orbit, or launch option.
    /// Populated by Member 1 in PartsDatabase.cs, displayed by Member 2 in UI.
    /// </summary>
    [Serializable]
    public class ComponentPart
    {
        public string Id;
        public string Name;
        public PartCategory Category;
        public MissionSegment Segment;
        public string Description;

        // Engineering Trade-Off Stats
        public float MassKg;             // Adds to total mass (or sets MaxMassKg if Category == Chassis)
        public float MaxMassCapacityKg;  // Only used by Chassis/LaunchVehicle to set max weight limit
        public float PowerGeneratedW;    // Positive for Solar Panels
        public float PowerConsumedW;     // Positive for Payloads, ADCS, Comms
        public float BatteryCapacityWh;  // Energy storage for surviving orbital eclipse
        public float CostThousands;      // Cost in $k (e.g., 25 = $25,000)
        public float DataRateMbps;       // Positive = Downlink capacity (Comms/Ground), or Data generated (Payload)
        public int SciencePoints;        // Science return value added by this component
        public int ReliabilityScore;     // 0 to 100 baseline hardware flight heritage

        // Special CubeSat 101 Flags (Used by Member 3's Review & Testing Phase!)
        public bool RequiresNoaaCameraLicense; // True if payload has an Earth-imaging camera
        public bool UsesDeployableBurnWire;    // True if solar panel/antenna unfolds in orbit
        public bool IsULListedBattery;         // True if battery passes Range Safety UL 1642 check
        public bool IsHighMeltingPointMetal;   // True if titanium/tungsten (fails ODAR orbital debris check!)
    }

    /// <summary>
    /// Represents one of the 3 playable NASA Mission Scenarios.
    /// Created by Member 3 (HanMing) in MissionScenariosData.cs.
    /// </summary>
    [Serializable]
    public class MissionProfile
    {
        public string MissionId;
        public string Title;
        public MissionDifficulty Difficulty;
        public string NasaReferenceSource;     // e.g., "NASA SmallSat Webinar: BioNutrients-2"
        public string BriefingDescription;     // Shown on Mission Select Screen
        public string ScienceObjectiveSummary; // CML 1 "Cocktail Napkin" objective

        // Mission Requirements & Constraints (CML 2 / CML 4 targets)
        public float BudgetCapThousands;       // Max budget in $k (e.g., $150k, $300k, $500k)
        public int TargetScienceScore;         // Minimum science points to succeed
        public string RequiredOrbitId;         // ID of the target orbit (e.g., "ORBIT_ISS", "ORBIT_SSO", "ORBIT_LUNAR")
        public string RequiredPayloadTrait;    // Hint for player on what payload type is needed
        public float RequiredMassMarginPct;    // e.g., 15% for Easy, 25% for Medium, 35% for Hard (JPL CML 4 standard!)
        public float RequiredPowerMarginPct;   // e.g., 15% to 35% power margin
    }

    /// <summary>
    /// Represents a pre-launch Review / Testing / Dilemma choice from CubeSat 101.
    /// Created & managed by Member 3 (HanMing).
    /// </summary>
    [Serializable]
    public class DilemmaCard
    {
        public string CardId;
        public string Title;
        public string NasaRuleReference; // Explains the real CubeSat 101 / CML 4 lesson!
        public string SituationText;

        // Option A (Usually the proper NASA Engineering choice — costs budget/mass, lowers risk)
        public string OptionAText;
        public float OptionACostThousands;
        public float OptionAMassKg;
        public int OptionAReliabilityBonus;
        public string OptionAOutcomeLog;

        // Option B (Usually the shortcut / risky choice — saves money, risks mission failure)
        public string OptionBText;
        public float OptionBCostThousands;
        public float OptionBMassKg;
        public int OptionBReliabilityPenalty;
        public string OptionBOutcomeLog;
    }

    /// <summary>
    /// Holds the player's current spacecraft design choices & testing decisions across phases.
    /// Stored in GameManager so Member 1, Member 2, and Member 3 can read/update it.
    /// </summary>
    [Serializable]
    public class SpacecraftDesign
    {
        public MissionProfile SelectedMission;

        // Selected Components (Phase 2 Builder)
        public ComponentPart SelectedChassis;
        public ComponentPart SelectedLaunchVehicle;
        public ComponentPart SelectedOrbit;
        public ComponentPart SelectedPayload;
        public ComponentPart SelectedSolarPanel;
        public ComponentPart SelectedBattery;
        public ComponentPart SelectedADCS;
        public ComponentPart SelectedComms;
        public ComponentPart SelectedGroundStation;

        // Phase 3 Pre-Launch Verification & Licensing Checkboxes (CubeSat 101)
        public bool HasFccIaruRfLicense;         // Required for ALL CubeSats with radios!
        public bool HasNoaaRemoteSensingLicense; // Required if Payload has a camera!
        public bool CompletedVibrationTest;      // Prevents structural failure during rocket launch
        public bool CompletedTvacBakeoutTest;    // Prevents outgassing from ruining camera/sensors
        public bool CompletedDitlSoftwareTest;   // Verifies 45-min deployment timer & RBF pin inhibits
        public bool UsedDoubleBurnWire;          // Redundant fishing-line burn wire for deployables

        // Extra modifiers from Phase 3 Dilemma Cards
        public float ExtraTestingCostThousands;
        public float ExtraMassKg;
        public int ExtraReliabilityModifier;
        public List<string> DecisionLogs = new List<string>();
    }

    /// <summary>
    /// Output of Member 1's MissionCalculator & Member 3's Final Scorecard Evaluator.
    /// Read by Member 2's UI to draw resource bars and the final Debrief screen.
    /// </summary>
    [Serializable]
    public class MissionEvaluationResult
    {
        // Totals
        public float TotalMassKg;
        public float MaxAllowedMassKg;
        public float MassMarginPercent;

        public float TotalPowerGeneratedW;
        public float TotalPowerConsumedW;
        public float PowerMarginPercent;

        public float TotalCostThousands;
        public float BudgetCapThousands;
        public float BudgetReservePercent;

        public float DataGeneratedPerOrbitMb;
        public float DataDownlinkedPerOrbitMb;

        public int TotalScienceScore;
        public int TargetScienceScore;
        public int FinalReliabilityPercent;

        // JPL CML 4 Checklist RYG Statuses (Page 12-14 of 13-3547_A1b.pdf)
        public RYGStatus ScienceStatus;     // Meets science objective & traceability?
        public RYGStatus MassMarginStatus;  // Within envelope & margin?
        public RYGStatus PowerMarginStatus; // Positive power balance + margin?
        public RYGStatus TelecomLinkStatus; // Ground station & radio can downlink payload data?
        public RYGStatus LicensingStatus;   // FCC + NOAA licenses secured?
        public RYGStatus TestingAndVnvStatus; // Vibe, TVAC, DITL completed?
        public RYGStatus CostStatus;        // Within budget cap?

        public float CmlChecklistGreenPercentage; // JPL standard: >= 80% Green = Ready for Flight!
        public bool IsLaunchApproved;             // False if demanifested (e.g., missing FCC license or overweight)
        public bool IsMissionSuccess;
        public string FinalGrade;                 // "S", "A", "B", "C", "SCRUBBED", or "MISSION FAILED"
        public List<string> FlightSimulationLogs = new List<string>(); // Step-by-step events for Phase 4 & 5
    }
}
