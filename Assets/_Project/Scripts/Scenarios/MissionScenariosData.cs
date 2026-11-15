using System.Collections.Generic;
using SpaceMissionGame.Core;

namespace SpaceMissionGame.Scenarios
{
    /// <summary>
    /// Member 3 (HanMing): Static catalog of the 3 Playable NASA Mission Scenarios (CML 1)
    /// and the CubeSat 101 Pre-Launch Engineering Dilemma Cards (CML 4).
    /// All numbers are balanced directly against Member 1's PartsDatabase and MissionCalculator!
    /// </summary>
    public static class MissionScenariosData
    {
        private static List<MissionProfile> _missions;
        private static List<DilemmaCard> _dilemmaCards;

        /// <summary>
        /// Returns all 3 playable NASA Mission Scenarios (Easy, Medium, Hard).
        /// </summary>
        public static List<MissionProfile> GetAllMissions()
        {
            if (_missions == null || _missions.Count == 0)
            {
                InitializeMissions();
            }
            return new List<MissionProfile>(_missions);
        }

        /// <summary>
        /// Looks up a specific MissionProfile by its MissionId or Difficulty.
        /// </summary>
        public static MissionProfile GetMissionById(string missionId)
        {
            if (string.IsNullOrEmpty(missionId)) return GetAllMissions()[0];
            MissionProfile found = GetAllMissions().Find(m =>
                string.Equals(m.MissionId, missionId, System.StringComparison.OrdinalIgnoreCase));
            return found ?? GetAllMissions()[0];
        }

        /// <summary>
        /// Returns all Pre-Launch Review & Testing Dilemma Cards based on NASA CubeSat 101.
        /// </summary>
        public static List<DilemmaCard> GetAllDilemmaCards()
        {
            if (_dilemmaCards == null || _dilemmaCards.Count == 0)
            {
                InitializeDilemmaCards();
            }
            return new List<DilemmaCard>(_dilemmaCards);
        }

        private static void InitializeMissions()
        {
            _missions = new List<MissionProfile>
            {
                // =========================================================================
                // MISSION 1 (EASY): BioNutrients-2 ISS Microgravity Lab
                // Source: 250323-design-and-develop-science-mission.pdf (Pages 11-23)
                // Optimal Build: 1U ($15k) + ISS Launch ($45k) + ISS Orbit ($0k) + BioNutrients ($35k, 70pts)
                //   + 1U Body Solar ($15k, 7W*0.60=4.2W) + UL 12Wh Battery ($12k) + Passive Magnet ($8k, 5pts)
                //   + UHF Radio ($12k) + UHF Yagi Ground ($10k, 5pts) = $152k, 0.99kg/1.33kg (25.6% margin), 80 pts!
                // =========================================================================
                new MissionProfile
                {
                    MissionId = "MISSION_1_BIONUTRIENTS",
                    Title = "Mission 1: BioNutrients-2 (ISS Microgravity Lab)",
                    Difficulty = MissionDifficulty.Easy_ISSCubeSat,
                    NasaReferenceSource = "NASA SmallSat Systems Engineering Webinar: BioNutrients-2 (March 2025)",
                    BriefingDescription =
                        "Foods and vitamins stored for long-duration human missions to Mars degrade over time. " +
                        "Design a compact 1U CubeSat experiment deployed from the International Space Station (ISS) " +
                        "to hydrate and incubate genetically modified yeast spores that produce beta-carotene in microgravity.",
                    ScienceObjectiveSummary =
                        "CML 1 Objective: Demonstrate in-space growth of yeast organisms (<250g payload) in ISS Low Earth Orbit " +
                        "and downlink optical growth telemetry to the university ground station.",
                    BudgetCapThousands = 230f,
                    TargetScienceScore = 70,
                    RequiredOrbitId = "ORBIT_ISS",
                    RequiredPayloadTrait = "BioNutrients-2 Yeast Incubator (PAYLOAD_BIONUTRIENTS)",
                    RequiredMassMarginPct = 15f,
                    RequiredPowerMarginPct = 8f
                },

                // =========================================================================
                // MISSION 2 (MEDIUM): BisonSat 3U Earth Forest Imager
                // Source: nasa_csli_cubesat_101_508.pdf (Page 55 & 59 - NOAA Licensing & 3U P-POD)
                // Optimal Build: 3U ($35k) + Rideshare LEO ($85k) + SSO Orbit ($10k) + Earth Camera ($70k, 90pts)
                //   + 3U Deployable Solar ($40k, 22W*0.68=14.96W) + UL 12Wh/40Wh Battery ($12k) + Magnetorquers ($30k, 15pts)
                //   + S-Band Radio ($35k, 60Mbps) + S-Band Dish ($30k, 65Mbps, 15pts) = $347k, 2.54kg/4.0kg (36.5% margin), 120 pts!
                // =========================================================================
                new MissionProfile
                {
                    MissionId = "MISSION_2_BISONSAT",
                    Title = "Mission 2: BisonSat (3U Polar Earth Forest Imager)",
                    Difficulty = MissionDifficulty.Medium_EarthObserver,
                    NasaReferenceSource = "NASA CubeSat 101 (ELaNa XII BisonSat & Chapter 5 Licensing Procedures)",
                    BriefingDescription =
                        "Design a 3U Earth-observation CubeSat launched via a P-POD rideshare dispenser into a " +
                        "Sun-Synchronous Polar Orbit (~600 km). Capture multi-spectral visible/NIR imagery of forest " +
                        "canopy health and wildfire scars, while managing high data rates and regulatory camera licensing!",
                    ScienceObjectiveSummary =
                        "CML 1 Objective: Acquire <100m resolution Earth imagery in Sun-Synchronous Orbit, maintain >= 20% " +
                        "mass/power margins, and downlink >= 45 Mbps via S-Band.",
                    BudgetCapThousands = 450f,
                    TargetScienceScore = 95,
                    RequiredOrbitId = "ORBIT_SSO",
                    RequiredPayloadTrait = "BisonSat Visible/NIR Earth Camera (Requires NOAA License!)",
                    RequiredMassMarginPct = 20f,
                    RequiredPowerMarginPct = 20f
                },

                // =========================================================================
                // MISSION 3 (HARD): Artemis Lunar Ice Pathfinder (6U Deep Space)
                // Source: 13-3547_A1b.pdf (JPL CML 4 Checklist - 35% Mass & Power Margins)
                // Optimal Build: 6U ($75k) + Lunar 6U Rocket ($150k) + Lunar Orbit ($40k) + Lunar Spectrometer ($130k, 120pts)
                //   + 6U Gimbal Solar ($85k, 48W*0.85=40.8W vs 27.5W used = 32.6% margin) + UL 40Wh Battery ($35k)
                //   + Reaction Wheels ($75k, 30pts) + Iris X-Band ($90k, 120Mbps) + NASA DSN ($75k, 150Mbps, 25pts)
                //   = $755k, 7.35kg/12.0kg (38.7% mass margin), 175 pts!
                // =========================================================================
                new MissionProfile
                {
                    MissionId = "MISSION_3_LUNAR",
                    Title = "Mission 3: Artemis Lunar Ice Pathfinder (6U Deep Space)",
                    Difficulty = MissionDifficulty.Hard_LunarPathfinder,
                    NasaReferenceSource = "JPL Concept Maturity Level (CML 4) Matrix & NASA SmallSat Webinar",
                    BriefingDescription =
                        "Design a 6U deep-space CubeSat manifested on an Artemis cislunar launch to orbit 100 km above " +
                        "the Moon's South Pole. Map water-ice deposits inside permanently shadowed craters while closing " +
                        "a 384,400 km X-Band Deep Space Network link and meeting strict JPL CML 4 margins!",
                    ScienceObjectiveSummary =
                        "CML 1 Objective: Map Lunar South Pole water-ice volatiles using an IR Spectrometer, close the link " +
                        "via Iris X-Band + NASA DSN, and maintain >= 20% Mass & Power margins.",
                    BudgetCapThousands = 950f,
                    TargetScienceScore = 140,
                    RequiredOrbitId = "ORBIT_LUNAR",
                    RequiredPayloadTrait = "Lunar IR Water-Ice Spectrometer + X-Band + NASA DSN",
                    RequiredMassMarginPct = 20f,
                    RequiredPowerMarginPct = 20f
                }
            };
        }

        private static void InitializeDilemmaCards()
        {
            _dilemmaCards = new List<DilemmaCard>
            {
                // =========================================================================
                // DILEMMA CARD 1: Burn-Wire Deployment Redundancy (CubeSat 101 Page 24)
                // =========================================================================
                new DilemmaCard
                {
                    CardId = "DILEMMA_BURN_WIRE",
                    Title = "Deployable Constraint: Burn-Wire Mechanism",
                    NasaRuleReference = "NASA CubeSat 101 (Ch 2.4 Free Advice: 'Double Up on That Burn Wire')",
                    SituationText =
                        "Your deployable solar panels and antennas are held stowed inside the dispenser using a fishing line " +
                        "wrapped around a heating resistor ('burn wire'). During vibration testing, a single fishing line can " +
                        "snap prematurely—or fail to melt in cold vacuum. How will your team wire the release mechanism?",
                    OptionAText = "Install Redundant Double Burn-Wires (+ $4k, +0.04 kg, +5% Reliability)",
                    OptionACostThousands = 4f,
                    OptionAMassKg = 0.04f,
                    OptionAReliabilityBonus = 5,
                    OptionAOutcomeLog = "[CML 4 REVIEW] Installed dual redundant burn-wires per NASA CubeSat 101 best practices.",

                    OptionBText = "Keep Single Burn-Wire to Save Mass & Budget ($0k, -8% Reliability Risk)",
                    OptionBCostThousands = 0f,
                    OptionBMassKg = 0f,
                    OptionBReliabilityPenalty = -8,
                    OptionBOutcomeLog = "[CML 4 RISK] Kept single non-redundant burn-wire; elevated risk of deployment jam in orbit."
                },

                // =========================================================================
                // DILEMMA CARD 2: Outgassing & Epoxies (CubeSat 101 Page 64 & 70)
                // =========================================================================
                new DilemmaCard
                {
                    CardId = "DILEMMA_OUTGASSING",
                    Title = "Materials Selection: Outgassing & Optical Contamination",
                    NasaRuleReference = "NASA CubeSat 101 (Ch 6.3 Materials List: TML < 1.0%, CVCM < 0.1%)",
                    SituationText =
                        "While assembling the payload electronics, a teammate notices the standard hardware-store epoxy " +
                        "exceeds NASA's Collected Volatile Condensible Material (CVCM) outgassing limit. In space vacuum, " +
                        "outgassed fumes can condense onto optical lenses and solar cells.",
                    OptionAText = "Replace with NASA-Approved Low-Outgassing Epoxy & Bakeout (+ $6k, +4% Reliability)",
                    OptionACostThousands = 6f,
                    OptionAMassKg = 0.01f,
                    OptionAReliabilityBonus = 4,
                    OptionAOutcomeLog = "[CML 4 REVIEW] Verified all adhesives meet NASA low-outgassing standards (http://outgassing.nasa.gov).",

                    OptionBText = "Use Standard Hardware Epoxy to Stay on Schedule ($0k, -10% Reliability Risk)",
                    OptionBCostThousands = 0f,
                    OptionBMassKg = 0f,
                    OptionBReliabilityPenalty = -10,
                    OptionBOutcomeLog = "[CML 4 RISK] Used non-compliant adhesive; high risk of outgassing contamination in vacuum."
                },

                // =========================================================================
                // DILEMMA CARD 3: DITL Software Freeze Dilemma (CubeSat 101 Page 68)
                // =========================================================================
                new DilemmaCard
                {
                    CardId = "DILEMMA_DITL_FREEZE",
                    Title = "Flight Software: Last-Minute Code Patch Before Delivery",
                    NasaRuleReference = "NASA CubeSat 101 (Ch 6.9.1: 'After DITL testing, do not change any piece of code!')",
                    SituationText =
                        "Two days before shipping the CubeSat to the launch integrator, a developer wants to push a " +
                        "'quick 5-line C code optimization' to the telemetry packet formatter without re-running the " +
                        "full Day-In-The-Life (DITL) 45-minute deployment timer test.",
                    OptionAText = "Enforce Strict Code Freeze & Re-Verify DITL Timers (+ $5k, +6% Reliability)",
                    OptionACostThousands = 5f,
                    OptionAMassKg = 0f,
                    OptionAReliabilityBonus = 6,
                    OptionAOutcomeLog = "[CML 4 REVIEW] Enforced strict flight software freeze after Day-In-The-Life (DITL) verification.",

                    OptionBText = "Push Unverified Code Patch Without Re-Testing ($0k, -12% Reliability Risk)",
                    OptionBCostThousands = 0f,
                    OptionBMassKg = 0f,
                    OptionBReliabilityPenalty = -12,
                    OptionBOutcomeLog = "[CML 4 RISK] Modified flight software after DITL testing without re-verification!"
                }
            };
        }
    }
}
