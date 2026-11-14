using System.Collections.Generic;
using SpaceMissionGame.Core;

namespace SpaceMissionGame.Core
{
    /// <summary>
    /// Static catalog of all selectable spacecraft components, launch vehicles, GMAT orbit presets,
    /// ground stations, and user data plans.
    /// Grounded in NASA CubeSat 101, JPL CML, NASA SmallSat SE Webinar, and GMAT orbital presets.
    /// </summary>
    public static class PartsDatabase
    {
        private static List<ComponentPart> _allParts;

        /// <summary>
        /// Returns the complete catalog of parts across all 4 NASA Mission Segments.
        /// Initializes the database lazily on first call.
        /// </summary>
        public static List<ComponentPart> GetAllParts()
        {
            if (_allParts == null || _allParts.Count == 0)
            {
                InitializeDatabase();
            }
            return new List<ComponentPart>(_allParts);
        }

        /// <summary>
        /// Returns all parts belonging to a specific NASA Mission Segment (Launch, Space, Ground, User).
        /// Ideal for Member 2's 4 segment tabs in the Builder UI.
        /// </summary>
        public static List<ComponentPart> GetPartsBySegment(MissionSegment segment)
        {
            return GetAllParts().FindAll(p => p.segment == segment);
        }

        /// <summary>
        /// Returns all parts matching a specific PartCategory (Chassis, Payload, ADCS, etc.).
        /// </summary>
        public static List<ComponentPart> GetPartsByCategory(PartCategory category)
        {
            return GetAllParts().FindAll(p => p.category == category);
        }

        /// <summary>
        /// Looks up a single part by its unique partId. Returns null if not found.
        /// </summary>
        public static ComponentPart GetPartById(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return null;
            return GetAllParts().Find(p => p.partId == partId);
        }

        private static void InitializeDatabase()
        {
            _allParts = new List<ComponentPart>
            {
                // =========================================================================
                // 1. LAUNCH SEGMENT: CHASSIS (NASA CubeSat 101 Standard Sizes)
                // =========================================================================
                new ComponentPart
                {
                    partId = "chassis_1u",
                    partName = "1U Standard CubeSat Chassis",
                    segment = MissionSegment.Launch,
                    category = PartCategory.Chassis,
                    description = "10x10x11.35 cm anodized aluminum CubeSat frame. Ideal for compact ISS biological or tech-demo experiments.",
                    nasaTechnicalNote = "CubeSat 101 Rule: 1U max mass is 1.33 kg. Uses 6061-T6 aluminum for safe 25-year orbital re-entry burnup (ODAR compliance).",
                    massKg = 0.20f,
                    volumeUnitsU = 0f,
                    costThousands = 15f,
                    cubeSatSize = CubeSatSize.OneU,
                    maxMassLimitKg = 1.33f,
                    maxVolumeU = 1.0f,
                    baseReliabilityPercent = 98f
                },
                new ComponentPart
                {
                    partId = "chassis_3u",
                    partName = "3U P-POD CubeSat Chassis",
                    segment = MissionSegment.Launch,
                    category = PartCategory.Chassis,
                    description = "10x10x34.05 cm 3U frame compatible with standard P-POD deployers. Fits medium optical Earth-imaging payloads.",
                    nasaTechnicalNote = "CubeSat 101 Rule: 3U max mass is 4.00 kg. Standard workhorse form factor for NASA CSLI university missions.",
                    massKg = 0.50f,
                    volumeUnitsU = 0f,
                    costThousands = 35f,
                    cubeSatSize = CubeSatSize.ThreeU,
                    maxMassLimitKg = 4.00f,
                    maxVolumeU = 3.0f,
                    baseReliabilityPercent = 97f
                },
                new ComponentPart
                {
                    partId = "chassis_6u",
                    partName = "6U Deep-Space Rad-Shielded Chassis",
                    segment = MissionSegment.Launch,
                    category = PartCategory.Chassis,
                    description = "20x10x34.05 cm heavy-duty 6U bus with spot radiation shielding for deep-space & Lunar exploration.",
                    nasaTechnicalNote = "CubeSat 101 Rule: 6U max mass is 12.00 kg. Required for missions carrying propulsion, star trackers, and large spectrometers.",
                    massKg = 1.20f,
                    volumeUnitsU = 0f,
                    costThousands = 80f,
                    cubeSatSize = CubeSatSize.SixU,
                    maxMassLimitKg = 12.00f,
                    maxVolumeU = 6.0f,
                    baseReliabilityPercent = 96f
                },

                // =========================================================================
                // 2. LAUNCH SEGMENT: LAUNCH VEHICLES & DISPENSERS
                // =========================================================================
                new ComponentPart
                {
                    partId = "launch_iss_nanoracks",
                    partName = "ISS Cargo Resupply & NanoRacks Deployer",
                    segment = MissionSegment.Launch,
                    category = PartCategory.LaunchVehicle,
                    description = "Launches aboard pressurized ISS commercial cargo (e.g., Cygnus NG-18) for deployment from the International Space Station.",
                    nasaTechnicalNote = "Supports 1U to 3U CubeSats (up to 4.00 kg) into ~400 km ISS Low Earth Orbit. High crew-safety scrutiny.",
                    massKg = 0f,
                    costThousands = 50f,
                    maxMassLimitKg = 4.00f,
                    cubeSatSize = CubeSatSize.ThreeU,
                    orbitType = OrbitType.ISS_LEO_400km,
                    baseReliabilityPercent = 98f
                },
                new ComponentPart
                {
                    partId = "launch_rideshare_ppod",
                    partName = "LEO Rideshare Rocket & P-POD Dispenser",
                    segment = MissionSegment.Launch,
                    category = PartCategory.LaunchVehicle,
                    description = "Secondary payload slot inside a Poly Picosatellite Orbital Deployer (P-POD) heading to Sun-Synchronous Polar Orbit.",
                    nasaTechnicalNote = "Supports 1U to 3U CubeSats (up to 4.00 kg) into ~600 km Polar / Sun-Synchronous Orbit for global Earth coverage.",
                    massKg = 0f,
                    costThousands = 95f,
                    maxMassLimitKg = 4.00f,
                    cubeSatSize = CubeSatSize.ThreeU,
                    orbitType = OrbitType.SunSync_Polar_600km,
                    baseReliabilityPercent = 95f
                },
                new ComponentPart
                {
                    partId = "launch_artemis_cislunar",
                    partName = "Artemis Cislunar Rideshare & 6U Dispenser",
                    segment = MissionSegment.Launch,
                    category = PartCategory.LaunchVehicle,
                    description = "Heavy-lift deep-space dispenser slot injecting 6U spacecraft onto a Trans-Lunar Injection (TLI) trajectory.",
                    nasaTechnicalNote = "Supports up to 6U CubeSats (12.00 kg max) for Lunar Orbit missions. Subject to strict JPL CML 4 margin checks.",
                    massKg = 0f,
                    costThousands = 180f,
                    maxMassLimitKg = 12.00f,
                    cubeSatSize = CubeSatSize.SixU,
                    orbitType = OrbitType.Lunar_Orbit,
                    baseReliabilityPercent = 93f
                },

                // =========================================================================
                // 3. LAUNCH SEGMENT: GMAT ORBIT PRESETS
                // =========================================================================
                new ComponentPart
                {
                    partId = "orbit_iss_400km",
                    partName = "ISS Low Earth Orbit (~400 km, 51.6° Inc)",
                    segment = MissionSegment.Launch,
                    category = PartCategory.Orbit,
                    description = "GMAT Preset: Low altitude microgravity orbit matching the ISS. 92.5-minute period with 60% sunlight and 40% eclipse.",
                    nasaTechnicalNote = "GMAT Analysis: Protected by Earth's magnetosphere (low radiation). Frequent 37-minute eclipses require reliable battery cycling.",
                    massKg = 0f,
                    volumeUnitsU = 0f,
                    costThousands = 0f,
                    orbitType = OrbitType.ISS_LEO_400km,
                    sunlightFraction = 0.60f,
                    orbitalPeriodMinutes = 92.5f,
                    baseReliabilityPercent = 99f
                },
                new ComponentPart
                {
                    partId = "orbit_sunsync_600km",
                    partName = "Sun-Synchronous Polar Orbit (~600 km, 97.8° Inc)",
                    segment = MissionSegment.Launch,
                    category = PartCategory.Orbit,
                    description = "GMAT Preset: Near-polar orbit that passes over Earth at the same local solar time daily. 68% sunlight, 32% eclipse.",
                    nasaTechnicalNote = "GMAT Analysis: Ideal for consistent lighting in Earth forest/climate imaging (BisonSat). 96.7-minute orbital period.",
                    massKg = 0f,
                    volumeUnitsU = 0f,
                    costThousands = 10f,
                    orbitType = OrbitType.SunSync_Polar_600km,
                    sunlightFraction = 0.68f,
                    orbitalPeriodMinutes = 96.7f,
                    baseReliabilityPercent = 97f
                },
                new ComponentPart
                {
                    partId = "orbit_lunar_100km",
                    partName = "Lunar Polar Orbit (100 km Altitude + Thruster Module)",
                    segment = MissionSegment.Launch,
                    category = PartCategory.Orbit,
                    description = "GMAT Preset: Low Lunar polar orbit over the South Pole craters. Includes a 1U green-monopropellant capture thruster module.",
                    nasaTechnicalNote = "GMAT Analysis: 85% sunlight, 118-min period. Requires 0.80 kg / 1.0U Delta-V propulsion module for Lunar Orbit Insertion (LOI).",
                    massKg = 0.80f,
                    volumeUnitsU = 1.0f,
                    costThousands = 45f,
                    powerConsumedWatts = 1.5f,
                    orbitType = OrbitType.Lunar_Orbit,
                    sunlightFraction = 0.85f,
                    orbitalPeriodMinutes = 118.0f,
                    baseReliabilityPercent = 92f
                },

                // =========================================================================
                // 4. SPACE SEGMENT: SCIENCE PAYLOADS
                // =========================================================================
                new ComponentPart
                {
                    partId = "payload_bionutrients",
                    partName = "BioNutrients-2 Yeast Incubator & Optical Sensor",
                    segment = MissionSegment.Space,
                    category = PartCategory.Payload,
                    description = "Heated micro-bioreactor growing engineered yeast in microgravity to produce beta-carotene nutrients for astronauts.",
                    nasaTechnicalNote = "NASA SmallSat Webinar Spec: Ultra-compact (<250g, 0.3U). Does not image Earth, so no NOAA camera license is required.",
                    massKg = 0.24f,
                    volumeUnitsU = 0.3f,
                    costThousands = 45f,
                    powerConsumedWatts = 2.0f,
                    dataGeneratedMBPerDay = 15f,
                    scienceScoreBonus = 65,
                    requiresNoaaCameraLicense = false,
                    sensitiveToOutgassing = false,
                    pointingAccuracyDeg = 0f,
                    baseReliabilityPercent = 96f
                },
                new ComponentPart
                {
                    partId = "payload_bisonsat_camera",
                    partName = "BisonSat Visible/NIR Earth Forest Imager",
                    segment = MissionSegment.Space,
                    category = PartCategory.Payload,
                    description = "Multi-spectral Earth observation camera measuring forest canopy health, wildfire scars, and agricultural snowpack.",
                    nasaTechnicalNote = "CubeSat 101 Rule: Earth cameras require a NOAA Remote Sensing License, <= 2.0° ADCS pointing, and TVAC bakeout to prevent lens fogging!",
                    massKg = 0.85f,
                    volumeUnitsU = 1.0f,
                    costThousands = 85f,
                    powerConsumedWatts = 5.5f,
                    dataGeneratedMBPerDay = 180f,
                    scienceScoreBonus = 80,
                    requiresNoaaCameraLicense = true,
                    sensitiveToOutgassing = true,
                    pointingAccuracyDeg = 2.0f,
                    baseReliabilityPercent = 94f
                },
                new ComponentPart
                {
                    partId = "payload_lunar_spectrometer",
                    partName = "Lunar Infrared Volatiles & Water-Ice Spectrometer",
                    segment = MissionSegment.Space,
                    category = PartCategory.Payload,
                    description = "Cryogenic-cooled infrared spectrometer mapping water-ice deposits inside permanently shadowed Lunar South Pole craters.",
                    nasaTechnicalNote = "High-power 2U deep-space instrument. Demands <= 0.5° precision ADCS pointing, high data downlink, and TVAC outgassing bakeout.",
                    massKg = 2.10f,
                    volumeUnitsU = 2.0f,
                    costThousands = 160f,
                    powerConsumedWatts = 12.0f,
                    dataGeneratedMBPerDay = 320f,
                    scienceScoreBonus = 95,
                    requiresNoaaCameraLicense = false,
                    sensitiveToOutgassing = true,
                    pointingAccuracyDeg = 0.5f,
                    baseReliabilityPercent = 92f
                },
                new ComponentPart
                {
                    partId = "payload_rad_dosimeter",
                    partName = "Compact Radiation & Space Weather Dosimeter",
                    segment = MissionSegment.Space,
                    category = PartCategory.Payload,
                    description = "Secondary 0.2U solid-state particle detector measuring ionizing radiation dose and solar proton events.",
                    nasaTechnicalNote = "Great secondary payload to boost Science Return if your CubeSat has spare mass, volume, and power margins!",
                    massKg = 0.18f,
                    volumeUnitsU = 0.2f,
                    costThousands = 20f,
                    powerConsumedWatts = 0.8f,
                    dataGeneratedMBPerDay = 10f,
                    scienceScoreBonus = 20,
                    requiresNoaaCameraLicense = false,
                    sensitiveToOutgassing = false,
                    pointingAccuracyDeg = 0f,
                    baseReliabilityPercent = 98f
                },

                // =========================================================================
                // 5. SPACE SEGMENT: POWER SYSTEMS (SOLAR ARRAYS & BATTERIES)
                // =========================================================================
                new ComponentPart
                {
                    partId = "power_1u_body_solar",
                    partName = "1U Body-Mounted Solar Panels + 8Wh Li-Ion",
                    segment = MissionSegment.Space,
                    category = PartCategory.PowerSystem,
                    description = "Surface-mounted triple-junction GaAs solar cells with an integrated 8 Wh rechargeable Li-Ion battery.",
                    nasaTechnicalNote = "No moving parts or burn wires (high reliability), but limited to 7.0 W peak sunlight generation.",
                    massKg = 0.15f,
                    volumeUnitsU = 0.15f,
                    costThousands = 20f,
                    powerGeneratedWatts = 7.0f,
                    batteryCapacityWh = 8.0f,
                    usesDeployableBurnWire = false,
                    baseReliabilityPercent = 98f
                },
                new ComponentPart
                {
                    partId = "power_3u_deployable_solar",
                    partName = "3U Deployable Dual-Wing Solar Array + 18Wh Battery",
                    segment = MissionSegment.Space,
                    category = PartCategory.PowerSystem,
                    description = "Spring-hinged solar wings held stowed during launch by a fishing-line burn wire. Generates 22.0 W in sunlight.",
                    nasaTechnicalNote = "CubeSat 101 Rule: Uses a burn-wire release mechanism. Adding Double Burn Wire redundancy in Phase 3 prevents deployment failure!",
                    massKg = 0.45f,
                    volumeUnitsU = 0.35f,
                    costThousands = 50f,
                    powerGeneratedWatts = 22.0f,
                    batteryCapacityWh = 18.0f,
                    usesDeployableBurnWire = true,
                    baseReliabilityPercent = 93f
                },
                new ComponentPart
                {
                    partId = "power_6u_gimbal_solar",
                    partName = "6U High-Power Gimbaled Solar Array + 35Wh Battery",
                    segment = MissionSegment.Space,
                    category = PartCategory.PowerSystem,
                    description = "Sun-tracking deployable solar wings delivering 48.0 W peak power and 35 Wh energy storage for deep-space payloads.",
                    nasaTechnicalNote = "CubeSat 101 Rule: Uses burn-wire deployment and sun-tracking hinges. Requires Vibration testing and Double Burn Wire protection.",
                    massKg = 1.10f,
                    volumeUnitsU = 0.60f,
                    costThousands = 110f,
                    powerGeneratedWatts = 48.0f,
                    batteryCapacityWh = 35.0f,
                    usesDeployableBurnWire = true,
                    baseReliabilityPercent = 91f
                },
                new ComponentPart
                {
                    partId = "power_aux_battery_15wh",
                    partName = "Auxiliary UL-Listed Li-Ion Battery Pack (+15 Wh)",
                    segment = MissionSegment.Space,
                    category = PartCategory.PowerSystem,
                    description = "Optional secondary battery pack with built-in heater and 3 deployment inhibits to survive long orbital eclipses.",
                    nasaTechnicalNote = "Automatically equips into the secondary Battery slot without replacing your Solar Array! Boosts eclipse survival.",
                    massKg = 0.20f,
                    volumeUnitsU = 0.20f,
                    costThousands = 18f,
                    powerConsumedWatts = 0.3f,
                    powerGeneratedWatts = 0f,
                    batteryCapacityWh = 15.0f,
                    usesDeployableBurnWire = false,
                    baseReliabilityPercent = 98f
                },

                // =========================================================================
                // 6. SPACE SEGMENT: ATTITUDE DETERMINATION & CONTROL (ADCS)
                // =========================================================================
                new ComponentPart
                {
                    partId = "adcs_passive_magnet",
                    partName = "Passive Permanent Magnet & Hysteresis Rods",
                    segment = MissionSegment.Space,
                    category = PartCategory.ADCS,
                    description = "Aligns the CubeSat passively with Earth's magnetic field lines like a compass needle. Zero moving parts.",
                    nasaTechnicalNote = "Provides coarse ~10.0° pointing. Great for non-pointing ISS biology (BioNutrients-2), but too blurry for Earth/Lunar cameras!",
                    massKg = 0.08f,
                    volumeUnitsU = 0.1f,
                    costThousands = 10f,
                    powerConsumedWatts = 0.1f,
                    pointingAccuracyDeg = 10.0f,
                    scienceScoreBonus = 5,
                    baseReliabilityPercent = 99f
                },
                new ComponentPart
                {
                    partId = "adcs_magnetorquers",
                    partName = "3-Axis Magnetorquers & Coarse Sun Sensors",
                    segment = MissionSegment.Space,
                    category = PartCategory.ADCS,
                    description = "Electromagnetic coils that push against Earth's magnetic field for active 3-axis detumbling and nadir pointing.",
                    nasaTechnicalNote = "Provides 1.5° pointing accuracy — ideal for LEO Earth observation cameras (BisonSat). Ineffective in deep space / Lunar orbit!",
                    massKg = 0.30f,
                    volumeUnitsU = 0.3f,
                    costThousands = 40f,
                    powerConsumedWatts = 1.8f,
                    pointingAccuracyDeg = 1.5f,
                    scienceScoreBonus = 15,
                    baseReliabilityPercent = 96f
                },
                new ComponentPart
                {
                    partId = "adcs_reaction_wheels",
                    partName = "Precision 3-Axis Reaction Wheels & Star Tracker",
                    segment = MissionSegment.Space,
                    category = PartCategory.ADCS,
                    description = "High-precision momentum wheels paired with an optical star tracker for sub-degree deep-space pointing stability.",
                    nasaTechnicalNote = "Provides 0.1° pointing accuracy. Essential for Lunar spectroscopy and works anywhere in space without needing a planetary magnetic field.",
                    massKg = 0.75f,
                    volumeUnitsU = 0.6f,
                    costThousands = 95f,
                    powerConsumedWatts = 4.5f,
                    pointingAccuracyDeg = 0.1f,
                    scienceScoreBonus = 25,
                    baseReliabilityPercent = 94f
                },

                // =========================================================================
                // 7. SPACE SEGMENT: COMMUNICATIONS TRANSCEIVERS
                // =========================================================================
                new ComponentPart
                {
                    partId = "comms_uhf_dipole",
                    partName = "Amateur UHF Whip Transceiver (437 MHz)",
                    segment = MissionSegment.Space,
                    category = PartCategory.Communications,
                    description = "Low-power omnidirectional UHF radio using a spring-steel tape-measure antenna. Downlinks up to 35 MB/day.",
                    nasaTechnicalNote = "CubeSat 101 Rule: Requires FCC/IARU RF License and burn-wire antenna deployment. Good for low-data telemetry, too slow for cameras!",
                    massKg = 0.10f,
                    volumeUnitsU = 0.15f,
                    costThousands = 15f,
                    powerConsumedWatts = 1.5f,
                    dataDownlinkMBPerDay = 35f,
                    requiresFccRfLicense = true,
                    usesDeployableBurnWire = true,
                    baseReliabilityPercent = 95f
                },
                new ComponentPart
                {
                    partId = "comms_sband_patch",
                    partName = "High-Speed S-Band Patch Transceiver (2.2 GHz)",
                    segment = MissionSegment.Space,
                    category = PartCategory.Communications,
                    description = "Body-mounted S-Band patch antenna and transmitter capable of downlinking 250 MB/day of high-res Earth imagery.",
                    nasaTechnicalNote = "CubeSat 101 Rule: Requires FCC RF License and an S-Band or NASA ground dish. Sufficient bandwidth for LEO imaging missions.",
                    massKg = 0.28f,
                    volumeUnitsU = 0.30f,
                    costThousands = 45f,
                    powerConsumedWatts = 4.0f,
                    dataDownlinkMBPerDay = 250f,
                    requiresFccRfLicense = true,
                    usesDeployableBurnWire = false,
                    baseReliabilityPercent = 96f
                },
                new ComponentPart
                {
                    partId = "comms_xband_iris",
                    partName = "Iris X-Band Deep Space Transponder (8.4 GHz)",
                    segment = MissionSegment.Space,
                    category = PartCategory.Communications,
                    description = "JPL-heritage radiation-hardened X-Band transponder and high-gain reflector for Lunar and deep-space telecommunications.",
                    nasaTechnicalNote = "Delivers 500 MB/day downlink and closes the 384,400 km Lunar telecom link when paired with the NASA Deep Space Network.",
                    massKg = 0.95f,
                    volumeUnitsU = 0.60f,
                    costThousands = 120f,
                    powerConsumedWatts = 9.0f,
                    dataDownlinkMBPerDay = 500f,
                    requiresFccRfLicense = true,
                    usesDeployableBurnWire = true,
                    baseReliabilityPercent = 95f
                },

                // =========================================================================
                // 8. GROUND SEGMENT: GROUND STATIONS
                // =========================================================================
                new ComponentPart
                {
                    partId = "ground_uhf_yagi",
                    partName = "University Roof UHF Yagi Ground Station",
                    segment = MissionSegment.Ground,
                    category = PartCategory.GroundStation,
                    description = "Student-operated UHF Yagi antenna on campus. Receives up to 40 MB/day during short LEO overhead passes.",
                    nasaTechnicalNote = "Low cost, but bottlenecked at 40 MB/day and cannot track spacecraft at Lunar distances.",
                    massKg = 0f,
                    volumeUnitsU = 0f,
                    costThousands = 10f,
                    dataDownlinkMBPerDay = 40f,
                    baseReliabilityPercent = 95f
                },
                new ComponentPart
                {
                    partId = "ground_sband_dish",
                    partName = "University 3-Meter S-Band Dish Network",
                    segment = MissionSegment.Ground,
                    category = PartCategory.GroundStation,
                    description = "Automated 3-meter parabolic dish tracking LEO satellites across multiple daily ground passes (up to 260 MB/day).",
                    nasaTechnicalNote = "Matches S-Band onboard radios for Earth imaging missions like BisonSat.",
                    massKg = 0f,
                    volumeUnitsU = 0f,
                    costThousands = 35f,
                    dataDownlinkMBPerDay = 260f,
                    baseReliabilityPercent = 97f
                },
                new ComponentPart
                {
                    partId = "ground_nasa_dsn",
                    partName = "NASA Near Space & Deep Space Network (NSN / DSN)",
                    segment = MissionSegment.Ground,
                    category = PartCategory.GroundStation,
                    description = "Global 34-meter NASA antenna array in Goldstone, Madrid, and Canberra providing continuous 24/7 deep-space coverage.",
                    nasaTechnicalNote = "Supports up to 600 MB/day and is mandatory for closing high-margin Lunar communications links.",
                    massKg = 0f,
                    volumeUnitsU = 0f,
                    costThousands = 90f,
                    dataDownlinkMBPerDay = 600f,
                    scienceScoreBonus = 10,
                    baseReliabilityPercent = 99f
                },

                // =========================================================================
                // 9. USER SEGMENT: SCIENCE DATA SYSTEM & TRACEABILITY
                // =========================================================================
                new ComponentPart
                {
                    partId = "user_student_lab",
                    partName = "Student Lab Telemetry & Manual Analysis Plan",
                    segment = MissionSegment.User,
                    category = PartCategory.UserSegment,
                    description = "Undergraduate science team decodes packets manually and logs experiment growth curves in spreadsheet reports.",
                    nasaTechnicalNote = "NASA Science Mission Rule: 'Begin with the End in Mind' — defines how raw packets become usable science (+10 Science).",
                    massKg = 0f,
                    volumeUnitsU = 0f,
                    costThousands = 10f,
                    scienceScoreBonus = 10,
                    baseReliabilityPercent = 95f
                },
                new ComponentPart
                {
                    partId = "user_cloud_pipeline",
                    partName = "Automated Cloud Calibration & GIS Mapping Pipeline",
                    segment = MissionSegment.User,
                    category = PartCategory.UserSegment,
                    description = "Automated radiometric calibration and cloud-removal pipeline turning raw Earth imagery into public GIS maps.",
                    nasaTechnicalNote = "Increases science return (+20 Science) and meets JPL CML 4 Science Traceability Matrix standards.",
                    massKg = 0f,
                    volumeUnitsU = 0f,
                    costThousands = 25f,
                    scienceScoreBonus = 20,
                    baseReliabilityPercent = 97f
                },
                new ComponentPart
                {
                    partId = "user_nasa_pds_archive",
                    partName = "NASA Planetary Data System (PDS) Open-Science Suite",
                    segment = MissionSegment.User,
                    category = PartCategory.UserSegment,
                    description = "Full Level-0 to Level-3 spectral processing pipeline archived directly into NASA's Planetary Data System for global researchers.",
                    nasaTechnicalNote = "Gold-standard NASA User Segment architecture (+30 Science) required for flagship and Artemis pathfinder missions.",
                    massKg = 0f,
                    volumeUnitsU = 0f,
                    costThousands = 50f,
                    scienceScoreBonus = 30,
                    baseReliabilityPercent = 99f
                }
            };
        }
    }
}
