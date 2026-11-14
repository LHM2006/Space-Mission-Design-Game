using System;
using UnityEngine;

namespace SpaceMissionGame.Core
{
    /// <summary>
    /// Represents any selectable mission component or preset card across the 4 NASA Mission Segments
    /// (Chassis, Launch Vehicle, Orbit, Payload, Power, ADCS, Comms, Ground Station, User Segment).
    /// Marked [Serializable] so it can be inspected directly in the Unity Inspector.
    /// </summary>
    [Serializable]
    public class ComponentPart
    {
        [Header("1. Identity & Classification")]
        public string partId;
        public string partName;
        public MissionSegment segment;
        public PartCategory category;

        [TextArea(2, 4)]
        public string description;

        [Tooltip("Educational note from NASA CubeSat 101 / GMAT shown on the UI card or debrief.")]
        public string nasaTechnicalNote;

        [Header("2. Core Trade-Off Metrics (Mass, Volume, Cost)")]
        [Tooltip("Mass of the component in kilograms (kg).")]
        public float massKg;

        [Tooltip("Internal volume occupied inside the CubeSat in 'U' units (e.g., 0.5U, 1.0U).")]
        public float volumeUnitsU;

        [Tooltip("Cost in thousands of USD ($k). Example: 25 = $25,000.")]
        public float costThousands;

        [Header("3. Power Metrics (Generation, Consumption, Storage)")]
        [Tooltip("Average electrical power consumed by this part in Watts (W).")]
        public float powerConsumedWatts;

        [Tooltip("Peak electrical power generated during orbital sunlight in Watts (W).")]
        public float powerGeneratedWatts;

        [Tooltip("Battery energy storage capacity in Watt-hours (Wh) for surviving orbital eclipse.")]
        public float batteryCapacityWh;

        [Header("4. Data, Science & Reliability Metrics")]
        [Tooltip("Science data volume produced per day in Megabytes (MB/day).")]
        public float dataGeneratedMBPerDay;

        [Tooltip("Maximum data downlink/reception capacity in Megabytes per day (MB/day) for Comms & Ground Stations.")]
        public float dataDownlinkMBPerDay;

        [Tooltip("Science Return points contributed toward the mission's target science score.")]
        public int scienceScoreBonus;

        [Tooltip("Base hardware reliability percentage (0 to 100%).")]
        [Range(0f, 100f)]
        public float baseReliabilityPercent = 95f;

        [Header("5. Chassis, Launch & GMAT Orbit Constraints")]
        [Tooltip("For Chassis: the CubeSat size (1U, 3U, 6U). For other parts: minimum required CubeSat size.")]
        public CubeSatSize cubeSatSize = CubeSatSize.None;

        [Tooltip("For Chassis or Launch Vehicles: maximum allowed total spacecraft mass (kg).")]
        public float maxMassLimitKg;

        [Tooltip("For Chassis: maximum internal payload/subsystem volume in U (1U = 1, 3U = 3, 6U = 6).")]
        public float maxVolumeU;

        [Tooltip("Target or supported GMAT orbital regime (ISS 400km, Sun-Sync 600km, Lunar).")]
        public OrbitType orbitType = OrbitType.None;

        [Tooltip("For GMAT Orbit cards: fraction of each orbit spent in sunlight (0.0 to 1.0, e.g., 0.60 = 60% sun / 40% eclipse).")]
        [Range(0f, 1f)]
        public float sunlightFraction;

        [Tooltip("For GMAT Orbit cards: duration of one full orbit in minutes (e.g., 92.5 mins for ISS LEO).")]
        public float orbitalPeriodMinutes;

        [Header("6. NASA CubeSat 101 Verification & Licensing Triggers")]
        [Tooltip("True if this part is an Earth-imaging camera requiring a NOAA Remote Sensing License.")]
        public bool requiresNoaaCameraLicense;

        [Tooltip("True if this part transmits radio frequencies requiring an FCC / IARU RF License.")]
        public bool requiresFccRfLicense;

        [Tooltip("True if this part uses fishing-line burn-wire deployment (e.g., deployable solar array or antenna).")]
        public bool usesDeployableBurnWire;

        [Tooltip("True if this part has sensitive optics/sensors vulnerable to TVAC outgassing contamination.")]
        public bool sensitiveToOutgassing;

        [Tooltip("For ADCS: pointing accuracy provided (smaller = better). For Payloads: required pointing accuracy in degrees (0 = no strict requirement).")]
        public float pointingAccuracyDeg;

        public ComponentPart() { }

        /// <summary>
        /// Convenience constructor for quickly defining parts in PartsDatabase.cs.
        /// </summary>
        public ComponentPart(
            string id,
            string name,
            MissionSegment missionSegment,
            PartCategory partCategory,
            float mass,
            float cost,
            float powerDraw,
            string desc)
        {
            partId = id;
            partName = name;
            segment = missionSegment;
            category = partCategory;
            massKg = mass;
            costThousands = cost;
            powerConsumedWatts = powerDraw;
            description = desc;
            baseReliabilityPercent = 95f;
        }
    }
}
