namespace SpaceMissionGame.Core
{
    /// <summary>
    /// Maps to JPL Concept Maturity Levels (13-3547_A1b.pdf) and the 5-phase gameplay flow.
    /// Used by Member 3's GameManager to switch active UI panels.
    /// </summary>
    public enum GamePhase
    {
        MainMenu,
        CML1_MissionSelect,       // Cocktail Napkin: Pick Science Mission
        CML2_3_SpacecraftBuilder, // Initial Feasibility & Trade Space: Pick Parts & Segments
        CML4_ReviewAndTesting,    // Point Design: Checklist, Licensing & Dilemma Events
        PhaseCD_OrbitSimulation,  // Launch & Operations Simulation
        PostMission_Scorecard     // Final NASA Debrief & Grade
    }

    /// <summary>
    /// Maps to the 4 NASA Science Mission Segments (250323-design-and-develop-science-mission.pdf).
    /// Used by Member 2's Builder UI to group component cards into 4 tabs.
    /// </summary>
    public enum MissionSegment
    {
        Launch, // Chassis size (1U/3U/6U), Launch Vehicle / Dispenser, & Target Orbit
        Space,  // Payload, Power (Solar/Battery), ADCS, Comms
        Ground, // Ground Station Antenna & Operations
        User    // Science Data System & Traceability
    }

    /// <summary>
    /// Categories of selectable cards in the Spacecraft & Mission Builder.
    /// Includes all 7 core categories from PROJECT_INSTRUCTIONS.md plus Orbit and UserSegment.
    /// </summary>
    public enum PartCategory
    {
        Chassis,        // 1U, 3U, 6U CubeSat frames
        LaunchVehicle,  // ISS Cargo, Rideshare LEO, Lunar Trajectory
        Orbit,          // GMAT Orbit Presets (ISS ~400km, Sun-Sync ~600km, Lunar)
        Payload,        // Cameras, Spectrometers, Bio-Incubator
        PowerSystem,    // Body-mounted Solar, Deployable Solar, Batteries
        ADCS,           // Passive Magnet, Magnetorquers, 3-Axis Reaction Wheels
        Communications, // Amateur UHF, S-Band, X-Band Deep Space
        GroundStation,  // Amateur Yagi, University Dish, NASA Near Space Network
        UserSegment     // Science Data Processing & Traceability Plans
    }

    /// <summary>
    /// Red / Yellow / Green status from the JPL CML 4 Checklist (13-3547_A1b.pdf).
    /// Need >= 80% Green (and zero Red critical blockers) to pass comfortably.
    /// </summary>
    public enum RYGStatus
    {
        Red,    // Failed / Critical Deficiency
        Yellow, // Marginal / Risk of Failure
        Green   // Compliant with NASA Standards
    }

    /// <summary>
    /// Standard CubeSat form factors from NASA CubeSat 101.
    /// Integer values represent total "U" volume units (1U, 3U, 6U).
    /// </summary>
    public enum CubeSatSize
    {
        None = 0,
        OneU = 1,   // 10x10x11.35 cm, Max Mass <= 1.33 kg
        ThreeU = 3, // 10x10x34.05 cm, Max Mass <= 4.00 kg
        SixU = 6    // 20x10x34.05 cm, Max Mass <= 12.00 kg
    }

    /// <summary>
    /// Pre-calculated GMAT orbital regimes for mission target matching and sunlight/eclipse math.
    /// </summary>
    public enum OrbitType
    {
        None,
        ISS_LEO_400km,       // Low Earth Orbit (~400 km, ~60% sunlight, low radiation, ISS resupply)
        SunSync_Polar_600km, // Sun-Synchronous Polar (~600 km, ~65-75% sunlight, global Earth imaging)
        Lunar_Orbit          // Deep Space / Lunar (~90% sunlight, high radiation, requires high Delta-V & X-Band)
    }

    /// <summary>
    /// Final evaluation grades for Phase 5 (Post-Mission Scorecard).
    /// </summary>
    public enum MissionGrade
    {
        S,                 // Perfect NASA CML 4 compliance & high science return
        A,                 // Mission Success with healthy margins
        B,                 // Partial / Degraded Success (minor margin warnings)
        C,                 // Barely Survived (high risk or low science return)
        MissionScrubbed,   // Demanifested before launch (e.g., missing FCC/NOAA license or overweight)
        LostInSpace        // Failed in orbit (e.g., power starvation in eclipse, vibration failure, outgassing)
    }
}
