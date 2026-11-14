using System.Collections.Generic;

namespace SpaceMissionGame.Core
{
    /// <summary>
    /// Member 1: Static catalog of all selectable spacecraft components, launch vehicles,
    /// GMAT orbit presets, solar panels, batteries, ADCS, comms, and ground stations.
    /// Uses the exact ComponentPart schema defined in GameEnumsAndModels.cs.
    /// </summary>
    public static class PartsDatabase
    {
        private static List<ComponentPart> _allParts;

        /// <summary>
        /// Returns the complete catalog of parts across all 4 NASA Mission Segments.
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
        /// </summary>
        public static List<ComponentPart> GetPartsBySegment(MissionSegment segment)
        {
            return GetAllParts().FindAll(p => p.Segment == segment);
        }

        /// <summary>
        /// Returns all parts matching a specific PartCategory (Chassis, Payload, SolarPanel, Battery, etc.).
        /// </summary>
        public static List<ComponentPart> GetPartsByCategory(PartCategory category)
        {
            return GetAllParts().FindAll(p => p.Category == category);
        }

        /// <summary>
        /// Looks up a single part by its unique Id (case-insensitive). Returns null if not found.
        /// </summary>
        public static ComponentPart GetPartById(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return null;
            return GetAllParts().Find(p => string.Equals(p.Id, partId, System.StringComparison.OrdinalIgnoreCase));
        }

        private static void InitializeDatabase()
        {
            _allParts = new List<ComponentPart>
            {
                // =========================================================================
                // 1. CHASSIS (CubeSat 101 Standard Sizes & ODAR Material Rules)
                // =========================================================================
                new ComponentPart
                {
                    Id = "CHASSIS_1U",
                    Name = "1U Standard Aluminum Chassis (10x10x11.35 cm)",
                    Category = PartCategory.Chassis,
                    Segment = MissionSegment.Launch,
                    Description = "Standard 1U anodized aluminum CubeSat frame (Max Mass: 1.33 kg). Ideal for compact ISS biological experiments.",
                    MassKg = 0.20f,
                    MaxMassCapacityKg = 1.33f,
                    CostThousands = 15f,
                    ReliabilityScore = 98,
                    IsHighMeltingPointMetal = false
                },
                new ComponentPart
                {
                    Id = "CHASSIS_3U",
                    Name = "3U P-POD Aluminum Chassis (10x10x34.05 cm)",
                    Category = PartCategory.Chassis,
                    Segment = MissionSegment.Launch,
                    Description = "Standard 3U CubeSat frame compatible with P-POD deployers (Max Mass: 4.00 kg). Workhorse for LEO Earth imaging.",
                    MassKg = 0.50f,
                    MaxMassCapacityKg = 4.00f,
                    CostThousands = 35f,
                    ReliabilityScore = 97,
                    IsHighMeltingPointMetal = false
                },
                new ComponentPart
                {
                    Id = "CHASSIS_6U",
                    Name = "6U Deep-Space Shielded Aluminum Chassis (20x10x34 cm)",
                    Category = PartCategory.Chassis,
                    Segment = MissionSegment.Launch,
                    Description = "Heavy-duty 6U bus with radiation spot-shielding (Max Mass: 12.00 kg). Built for Lunar & deep-space missions.",
                    MassKg = 1.20f,
                    MaxMassCapacityKg = 12.00f,
                    CostThousands = 75f,
                    ReliabilityScore = 96,
                    IsHighMeltingPointMetal = false
                },
                new ComponentPart
                {
                    Id = "CHASSIS_3U_TITANIUM",
                    Name = "3U Experimental Titanium Frame (ODAR Risk!)",
                    Category = PartCategory.Chassis,
                    Segment = MissionSegment.Launch,
                    Description = "Ultra-strong titanium frame, but high melting point fails NASA Orbital Debris (ODAR) atmospheric burn-up rules!",
                    MassKg = 0.42f,
                    MaxMassCapacityKg = 4.00f,
                    CostThousands = 30f,
                    ReliabilityScore = 85,
                    IsHighMeltingPointMetal = true
                },

                // =========================================================================
                // 2. LAUNCH VEHICLES & DISPENSERS
                // =========================================================================
                new ComponentPart
                {
                    Id = "LAUNCH_ISS",
                    Name = "ISS Commercial Resupply & NanoRacks Deployer",
                    Category = PartCategory.LaunchVehicle,
                    Segment = MissionSegment.Launch,
                    Description = "Launches pressurized cargo (e.g., NG-18 Cygnus) to the International Space Station. Max payload: 4.00 kg.",
                    MassKg = 0f,
                    MaxMassCapacityKg = 4.00f,
                    CostThousands = 45f,
                    ReliabilityScore = 98
                },
                new ComponentPart
                {
                    Id = "LAUNCH_RIDESHARE_LEO",
                    Name = "Sun-Synchronous Rideshare Rocket (P-POD Dispenser)",
                    Category = PartCategory.LaunchVehicle,
                    Segment = MissionSegment.Launch,
                    Description = "Secondary rideshare slot inside a P-POD dispenser delivering up to 4.00 kg into Polar / Sun-Synchronous LEO.",
                    MassKg = 0f,
                    MaxMassCapacityKg = 4.00f,
                    CostThousands = 85f,
                    ReliabilityScore = 95
                },
                new ComponentPart
                {
                    Id = "LAUNCH_LUNAR_6U",
                    Name = "Artemis Cislunar Heavy Rocket & 6U Dispenser",
                    Category = PartCategory.LaunchVehicle,
                    Segment = MissionSegment.Launch,
                    Description = "Deep-space 6U dispenser injecting up to 12.00 kg onto a Trans-Lunar Injection (TLI) trajectory.",
                    MassKg = 0f,
                    MaxMassCapacityKg = 12.00f,
                    CostThousands = 150f,
                    ReliabilityScore = 94
                },

                // =========================================================================
                // 3. GMAT ORBIT PRESETS (Matches RequiredOrbitId: ORBIT_ISS, ORBIT_SSO, ORBIT_LUNAR)
                // =========================================================================
                new ComponentPart
                {
                    Id = "ORBIT_ISS",
                    Name = "ISS Low Earth Orbit (~400 km, 51.6° Inclination)",
                    Category = PartCategory.Orbit,
                    Segment = MissionSegment.Launch,
                    Description = "GMAT Preset: 92.5-min period (60% sunlight, 40% eclipse). Low radiation inside Earth's magnetosphere.",
                    MassKg = 0f,
                    CostThousands = 0f,
                    ReliabilityScore = 99
                },
                new ComponentPart
                {
                    Id = "ORBIT_SSO",
                    Name = "Sun-Synchronous Polar Orbit (~600 km, 97.8° Inc)",
                    Category = PartCategory.Orbit,
                    Segment = MissionSegment.Launch,
                    Description = "GMAT Preset: Passes over Earth at constant local solar time for global forest/climate imaging (68% sunlight).",
                    MassKg = 0f,
                    CostThousands = 10f,
                    ReliabilityScore = 97
                },
                new ComponentPart
                {
                    Id = "ORBIT_LUNAR",
                    Name = "Lunar Polar Orbit (100 km Altitude + Capture Thruster)",
                    Category = PartCategory.Orbit,
                    Segment = MissionSegment.Launch,
                    Description = "GMAT Preset: Orbits the Moon's South Pole craters (85% sunlight). Includes 0.80 kg propulsion Delta-V module.",
                    MassKg = 0.80f,
                    PowerConsumedW = 1.5f,
                    CostThousands = 40f,
                    ReliabilityScore = 93
                },

                // =========================================================================
                // 4. SCIENCE PAYLOADS (Space & User Segments)
                // =========================================================================
                new ComponentPart
                {
                    Id = "PAYLOAD_BIONUTRIENTS",
                    Name = "BioNutrients-2 Yeast Incubator & Optical Growth Sensor",
                    Category = PartCategory.Payload,
                    Segment = MissionSegment.Space,
                    Description = "NASA SmallSat Webinar Payload (<250g): Grows engineered yeast in microgravity to produce beta-carotene nutrients.",
                    MassKg = 0.24f,
                    PowerConsumedW = 2.0f,
                    CostThousands = 35f,
                    DataRateMbps = 5f,
                    SciencePoints = 70,
                    ReliabilityScore = 96,
                    RequiresNoaaCameraLicense = false
                },
                new ComponentPart
                {
                    Id = "PAYLOAD_EARTH_CAMERA",
                    Name = "BisonSat Visible/NIR Earth Forest Imager",
                    Category = PartCategory.Payload,
                    Segment = MissionSegment.Space,
                    Description = "Multi-spectral camera imaging forest canopy health and wildfire scars. Requires NOAA Remote Sensing License!",
                    MassKg = 0.85f,
                    PowerConsumedW = 5.5f,
                    CostThousands = 70f,
                    DataRateMbps = 45f,
                    SciencePoints = 90,
                    ReliabilityScore = 94,
                    RequiresNoaaCameraLicense = true
                },
                new ComponentPart
                {
                    Id = "PAYLOAD_LUNAR_SPECTROMETER",
                    Name = "Lunar Infrared Water-Ice Volatiles Spectrometer",
                    Category = PartCategory.Payload,
                    Segment = MissionSegment.Space,
                    Description = "Cryo-cooled IR spectrometer mapping water-ice deposits inside shadowed Lunar South Pole craters.",
                    MassKg = 2.10f,
                    PowerConsumedW = 12.0f,
                    CostThousands = 130f,
                    DataRateMbps = 80f,
                    SciencePoints = 120,
                    ReliabilityScore = 92,
                    RequiresNoaaCameraLicense = false
                },

                // =========================================================================
                // 5. SOLAR PANELS (Power Generation)
                // =========================================================================
                new ComponentPart
                {
                    Id = "SOLAR_1U_BODY",
                    Name = "1U Body-Mounted GaAs Solar Panels (7W Peak)",
                    Category = PartCategory.SolarPanel,
                    Segment = MissionSegment.Space,
                    Description = "Chassis-mounted solar cells with zero moving parts or burn wires. Generates 7.0 W in sunlight.",
                    MassKg = 0.15f,
                    PowerGeneratedW = 7.0f,
                    CostThousands = 15f,
                    ReliabilityScore = 99,
                    UsesDeployableBurnWire = false
                },
                new ComponentPart
                {
                    Id = "SOLAR_3U_DEPLOYABLE",
                    Name = "3U Deployable Dual-Wing Solar Array (22W Peak)",
                    Category = PartCategory.SolarPanel,
                    Segment = MissionSegment.Space,
                    Description = "Fold-out solar wings held by a fishing-line burn wire. Generates 22.0 W in sunlight.",
                    MassKg = 0.45f,
                    PowerGeneratedW = 22.0f,
                    CostThousands = 40f,
                    ReliabilityScore = 93,
                    UsesDeployableBurnWire = true
                },
                new ComponentPart
                {
                    Id = "SOLAR_6U_GIMBAL",
                    Name = "6U Sun-Tracking Gimbaled Solar Array (48W Peak)",
                    Category = PartCategory.SolarPanel,
                    Segment = MissionSegment.Space,
                    Description = "High-efficiency deployable wings delivering 48.0 W for power-hungry deep-space instruments.",
                    MassKg = 1.10f,
                    PowerGeneratedW = 48.0f,
                    CostThousands = 85f,
                    ReliabilityScore = 91,
                    UsesDeployableBurnWire = true
                },

                // =========================================================================
                // 6. BATTERIES (Eclipse Energy Storage & UL Safety Rules)
                // =========================================================================
                new ComponentPart
                {
                    Id = "BATTERY_UL_12WH",
                    Name = "Standard UL-Listed Li-Ion Battery Pack (12 Wh)",
                    Category = PartCategory.Battery,
                    Segment = MissionSegment.Space,
                    Description = "Range-safety certified UL 1642 battery with RBF pin and 3 deployment inhibits for LEO eclipses.",
                    MassKg = 0.16f,
                    PowerConsumedW = 0.2f,
                    BatteryCapacityWh = 12.0f,
                    CostThousands = 12f,
                    ReliabilityScore = 98,
                    IsULListedBattery = true
                },
                new ComponentPart
                {
                    Id = "BATTERY_UL_40WH",
                    Name = "High-Capacity Solid-State UL Battery Bank (40 Wh)",
                    Category = PartCategory.Battery,
                    Segment = MissionSegment.Space,
                    Description = "Deep-space heated battery bank (40 Wh) certified for high-power imaging and Lunar eclipse survival.",
                    MassKg = 0.45f,
                    PowerConsumedW = 0.5f,
                    BatteryCapacityWh = 40.0f,
                    CostThousands = 35f,
                    ReliabilityScore = 97,
                    IsULListedBattery = true
                },
                new ComponentPart
                {
                    Id = "BATTERY_DIY_UNCERTIFIED",
                    Name = "Cheap Uncertified Hobby LiPo Pack (15 Wh - Unsafe!)",
                    Category = PartCategory.Battery,
                    Segment = MissionSegment.Space,
                    Description = "Cheap COTS pouch cells lacking UL 1642 listing and thermal runaway protection. Violates NASA Range Safety!",
                    MassKg = 0.14f,
                    PowerConsumedW = 0.1f,
                    BatteryCapacityWh = 15.0f,
                    CostThousands = 4f,
                    ReliabilityScore = 65,
                    IsULListedBattery = false
                },

                // =========================================================================
                // 7. ATTITUDE DETERMINATION & CONTROL (ADCS)
                // =========================================================================
                new ComponentPart
                {
                    Id = "ADCS_PASSIVE_MAGNET",
                    Name = "Passive Permanent Magnet & Hysteresis Rods",
                    Category = PartCategory.ADCS,
                    Segment = MissionSegment.Space,
                    Description = "Aligns passively with Earth's magnetic field (~10° accuracy). Great for ISS biology, too blurry for cameras.",
                    MassKg = 0.08f,
                    PowerConsumedW = 0.1f,
                    CostThousands = 8f,
                    SciencePoints = 5,
                    ReliabilityScore = 99
                },
                new ComponentPart
                {
                    Id = "ADCS_MAGNETORQUERS",
                    Name = "3-Axis Magnetorquers & Sun Sensors",
                    Category = PartCategory.ADCS,
                    Segment = MissionSegment.Space,
                    Description = "Active magnetic torque coils (~1.5° nadir pointing) for LEO Earth observation cameras.",
                    MassKg = 0.30f,
                    PowerConsumedW = 1.8f,
                    CostThousands = 30f,
                    SciencePoints = 15,
                    ReliabilityScore = 96
                },
                new ComponentPart
                {
                    Id = "ADCS_REACTION_WHEELS",
                    Name = "Precision 3-Axis Reaction Wheels & Star Tracker",
                    Category = PartCategory.ADCS,
                    Segment = MissionSegment.Space,
                    Description = "Sub-degree (0.1°) optical star tracker and momentum wheels. Mandatory for Lunar/deep-space pointing!",
                    MassKg = 0.75f,
                    PowerConsumedW = 4.5f,
                    CostThousands = 75f,
                    SciencePoints = 30,
                    ReliabilityScore = 94
                },

                // =========================================================================
                // 8. COMMUNICATIONS TRANSCEIVERS
                // =========================================================================
                new ComponentPart
                {
                    Id = "COMMS_UHF",
                    Name = "Amateur UHF Tape-Measure Transceiver (437 MHz)",
                    Category = PartCategory.Communications,
                    Segment = MissionSegment.Space,
                    Description = "Omnidirectional UHF radio (10 Mbps effective pass rate). Uses burn-wire antenna release. Needs FCC license.",
                    MassKg = 0.10f,
                    PowerConsumedW = 1.5f,
                    CostThousands = 12f,
                    DataRateMbps = 10f,
                    ReliabilityScore = 95,
                    UsesDeployableBurnWire = true
                },
                new ComponentPart
                {
                    Id = "COMMS_SBAND",
                    Name = "High-Speed S-Band Patch Transceiver (2.2 GHz)",
                    Category = PartCategory.Communications,
                    Segment = MissionSegment.Space,
                    Description = "Body-mounted S-Band transmitter (60 Mbps) capable of downlinking multi-spectral LEO Earth imagery.",
                    MassKg = 0.28f,
                    PowerConsumedW = 4.0f,
                    CostThousands = 35f,
                    DataRateMbps = 60f,
                    ReliabilityScore = 96,
                    UsesDeployableBurnWire = false
                },
                new ComponentPart
                {
                    Id = "COMMS_XBAND_IRIS",
                    Name = "Iris X-Band Deep Space Transponder (8.4 GHz)",
                    Category = PartCategory.Communications,
                    Segment = MissionSegment.Space,
                    Description = "JPL-heritage radiation-hardened X-Band radio (120 Mbps) for closing 384,400 km Lunar telecom links.",
                    MassKg = 0.95f,
                    PowerConsumedW = 9.0f,
                    CostThousands = 90f,
                    DataRateMbps = 120f,
                    ReliabilityScore = 95,
                    UsesDeployableBurnWire = true
                },

                // =========================================================================
                // 9. GROUND SEGMENT: GROUND STATIONS
                // =========================================================================
                new ComponentPart
                {
                    Id = "GROUND_UHF_YAGI",
                    Name = "University Roof UHF Yagi Ground Station",
                    Category = PartCategory.GroundStation,
                    Segment = MissionSegment.Ground,
                    Description = "Student-operated UHF Yagi antenna receiving up to 15 Mbps during overhead LEO passes.",
                    MassKg = 0f,
                    CostThousands = 10f,
                    DataRateMbps = 15f,
                    SciencePoints = 5,
                    ReliabilityScore = 95
                },
                new ComponentPart
                {
                    Id = "GROUND_SBAND_DISH",
                    Name = "University 3-Meter S-Band Dish & Ops Center",
                    Category = PartCategory.GroundStation,
                    Segment = MissionSegment.Ground,
                    Description = "Automated 3m parabolic dish receiving up to 65 Mbps and archiving calibrated Earth imagery.",
                    MassKg = 0f,
                    CostThousands = 30f,
                    DataRateMbps = 65f,
                    SciencePoints = 15,
                    ReliabilityScore = 97
                },
                new ComponentPart
                {
                    Id = "GROUND_NASA_DSN",
                    Name = "NASA Near Space & Deep Space Network (NSN / DSN)",
                    Category = PartCategory.GroundStation,
                    Segment = MissionSegment.Ground,
                    Description = "Global 34-meter NASA antenna network (150 Mbps) with Planetary Data System (PDS) science archiving.",
                    MassKg = 0f,
                    CostThousands = 75f,
                    DataRateMbps = 150f,
                    SciencePoints = 25,
                    ReliabilityScore = 99
                }
            };
        }
    }
}
