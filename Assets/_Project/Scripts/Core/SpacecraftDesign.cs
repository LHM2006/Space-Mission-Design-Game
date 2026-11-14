using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMissionGame.Core
{
    /// <summary>
    /// Stores the player's current spacecraft configuration across all 4 NASA Mission Segments (Phase 2)
    /// as well as their Pre-Launch Verification, Licensing, and Dilemma choices (Phase 3).
    /// </summary>
    [Serializable]
    public class SpacecraftDesign
    {
        [Header("1. Launch Segment")]
        public ComponentPart selectedChassis;
        public ComponentPart selectedLaunchVehicle;
        public ComponentPart selectedOrbit;

        [Header("2. Space Segment")]
        [Tooltip("Supports 1 or more science instruments depending on available CubeSat U-volume, mass, and power.")]
        public List<ComponentPart> selectedPayloads = new List<ComponentPart>();
        public ComponentPart selectedPowerSystem;
        public ComponentPart selectedBattery;
        public ComponentPart selectedAdcs;
        public ComponentPart selectedComms;

        [Header("3. Ground Segment")]
        public ComponentPart selectedGroundStation;

        [Header("4. User Segment")]
        public ComponentPart selectedUserSegment;

        [Header("5. Phase 3 (CML 4) Regulatory Licensing (CubeSat 101)")]
        [Tooltip("Required for all radio transmitters (UHF, S-Band, X-Band). Without it, mission is scrubbed!")]
        public bool hasFccRfLicense;

        [Tooltip("Required if any equipped payload is an Earth-imaging camera. Without it, mission is scrubbed!")]
        public bool hasNoaaCameraLicense;

        [Header("6. Phase 3 (CML 4) Environmental & Verification Testing")]
        [Tooltip("Verifies spacecraft survives rocket launch vibration and shock loads.")]
        public bool completedVibrationTest;

        [Tooltip("Thermal Vacuum (TVAC) bakeout prevents material outgassing from fogging camera optics.")]
        public bool completedTvacBakeoutTest;

        [Tooltip("Day-In-The-Life (DITL) software test verifies 45-min post-deployment timer and operations.")]
        public bool completedDitlSoftwareTest;

        [Header("7. Phase 3 (CML 4) Hardware Safety & Dilemma Modifiers")]
        [Tooltip("Redundant fishing-line burn wire for deployable solar panels and antennas.")]
        public bool hasDoubleBurnWireRedundancy;

        [Tooltip("UL-listed batteries with Remove-Before-Flight (RBF) pin and 3 deployment inhibits.")]
        public bool hasCertifiedBatteriesAndInhibits = true;

        [Tooltip("Additional budget ($k) spent on Phase 3 verification tests and engineering dilemma cards.")]
        public float additionalTestingCostThousands;

        [Tooltip("Net reliability modifier (%) from Phase 3 engineering dilemma choices.")]
        public float dilemmaReliabilityModifier;

        [Tooltip("Net science score modifier from Phase 3 engineering dilemma choices.")]
        public int dilemmaScienceModifier;

        /// <summary>
        /// Equips a component into its matching slot based on PartCategory.
        /// For Payloads, toggles the payload in selectedPayloads (up to maxPayloads).
        /// For PowerSystem, places pure battery packs into selectedBattery and solar/combined units into selectedPowerSystem.
        /// </summary>
        public void EquipPart(ComponentPart part, int maxPayloads = 2)
        {
            if (part == null) return;

            switch (part.category)
            {
                case PartCategory.Chassis:
                    selectedChassis = part;
                    break;

                case PartCategory.LaunchVehicle:
                    selectedLaunchVehicle = part;
                    break;

                case PartCategory.Orbit:
                    selectedOrbit = part;
                    break;

                case PartCategory.Payload:
                    AddOrTogglePayload(part, maxPayloads);
                    break;

                case PartCategory.PowerSystem:
                    // If the part only stores energy (no solar generation), place it in the Battery sub-slot
                    if (part.powerGeneratedWatts <= 0f && part.batteryCapacityWh > 0f)
                    {
                        selectedBattery = part;
                    }
                    else
                    {
                        selectedPowerSystem = part;
                    }
                    break;

                case PartCategory.ADCS:
                    selectedAdcs = part;
                    break;

                case PartCategory.Communications:
                    selectedComms = part;
                    break;

                case PartCategory.GroundStation:
                    selectedGroundStation = part;
                    break;

                case PartCategory.UserSegment:
                    selectedUserSegment = part;
                    break;
            }
        }

        /// <summary>
        /// Adds a payload to the spacecraft (or removes it if already equipped).
        /// If already at maxPayloads, replaces the oldest payload.
        /// </summary>
        public void AddOrTogglePayload(ComponentPart payloadPart, int maxPayloads = 2)
        {
            if (payloadPart == null) return;

            int existingIndex = selectedPayloads.FindIndex(p => p != null && p.partId == payloadPart.partId);
            if (existingIndex >= 0)
            {
                selectedPayloads.RemoveAt(existingIndex);
                return;
            }

            if (selectedPayloads.Count >= maxPayloads && maxPayloads > 0)
            {
                selectedPayloads.RemoveAt(0);
            }

            selectedPayloads.Add(payloadPart);
        }

        /// <summary>
        /// Removes a specific payload from the spacecraft design.
        /// </summary>
        public bool RemovePayload(ComponentPart payloadPart)
        {
            if (payloadPart == null) return false;
            return selectedPayloads.RemoveAll(p => p != null && p.partId == payloadPart.partId) > 0;
        }

        /// <summary>
        /// Returns a flat list of every non-null component currently equipped across all 4 segments.
        /// Used by MissionCalculator to sum Mass, Cost, Power, Volume, and Data metrics cleanly.
        /// </summary>
        public List<ComponentPart> GetAllEquippedParts()
        {
            List<ComponentPart> parts = new List<ComponentPart>();

            if (selectedChassis != null) parts.Add(selectedChassis);
            if (selectedLaunchVehicle != null) parts.Add(selectedLaunchVehicle);
            if (selectedOrbit != null) parts.Add(selectedOrbit);

            if (selectedPayloads != null)
            {
                foreach (ComponentPart payload in selectedPayloads)
                {
                    if (payload != null) parts.Add(payload);
                }
            }

            if (selectedPowerSystem != null) parts.Add(selectedPowerSystem);
            if (selectedBattery != null) parts.Add(selectedBattery);
            if (selectedAdcs != null) parts.Add(selectedAdcs);
            if (selectedComms != null) parts.Add(selectedComms);
            if (selectedGroundStation != null) parts.Add(selectedGroundStation);
            if (selectedUserSegment != null) parts.Add(selectedUserSegment);

            return parts;
        }

        /// <summary>
        /// Checks whether the player has selected all mandatory core parts across the 4 NASA segments.
        /// </summary>
        public bool IsCoreDesignComplete()
        {
            bool hasPayload = selectedPayloads != null && selectedPayloads.Count > 0;
            bool hasPower = selectedPowerSystem != null || selectedBattery != null;

            return selectedChassis != null
                && selectedLaunchVehicle != null
                && selectedOrbit != null
                && hasPayload
                && hasPower
                && selectedAdcs != null
                && selectedComms != null
                && selectedGroundStation != null;
        }

        /// <summary>
        /// Clears all selected parts and resets Phase 3 testing/licensing flags for a fresh mission.
        /// </summary>
        public void Reset()
        {
            selectedChassis = null;
            selectedLaunchVehicle = null;
            selectedOrbit = null;
            selectedPayloads.Clear();
            selectedPowerSystem = null;
            selectedBattery = null;
            selectedAdcs = null;
            selectedComms = null;
            selectedGroundStation = null;
            selectedUserSegment = null;

            hasFccRfLicense = false;
            hasNoaaCameraLicense = false;
            completedVibrationTest = false;
            completedTvacBakeoutTest = false;
            completedDitlSoftwareTest = false;
            hasDoubleBurnWireRedundancy = false;
            hasCertifiedBatteriesAndInhibits = true;

            additionalTestingCostThousands = 0f;
            dilemmaReliabilityModifier = 0f;
            dilemmaScienceModifier = 0;
        }
    }
}
