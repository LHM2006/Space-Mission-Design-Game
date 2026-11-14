using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceMissionGame.Core;

namespace SpaceMissionGame.UI
{
    /// <summary>
    /// [Member 2 - UI] Main controller for Phase 2 (CML 2 & 3): Spacecraft & Segment Builder (`BuilderUI.cs`).
    /// 
    /// Directly integrates with Member 1's `PartsDatabase`, `SpacecraftDesign`, and `MissionCalculator`:
    /// 1. 4 NASA Mission Segment Tabs: Launch, Space, Ground, User (`MissionSegment`).
    /// 2. Spawns clickable `PartCardUI` items populated from `PartsDatabase`.
    /// 3. Updates the 2D visual preview of the 1U / 3U / 6U CubeSat slots.
    /// 4. Immediately calls `MissionCalculator.EvaluateMission(CurrentDesign)` and refreshes `ResourceBarsUI`.
    /// </summary>
    public class BuilderUI : MonoBehaviour
    {
        [Header("1. NASA 4-Segment Tab Buttons")]
        [SerializeField] private Button launchSegmentTabButton;
        [SerializeField] private Button spaceSegmentTabButton;
        [SerializeField] private Button groundSegmentTabButton;
        [SerializeField] private Button userSegmentTabButton;
        [SerializeField] private TMP_Text activeSegmentHeaderText;

        [Header("2. Part Cards Scroll List")]
        [SerializeField] private Transform partCardListContainer;
        [SerializeField] private PartCardUI partCardPrefab;

        [Header("3. 2D CubeSat Slot Blueprint Preview (1U / 3U / 6U)")]
        [SerializeField] private TMP_Text chassisTitleText;
        [SerializeField] private TMP_Text selectedOrbitAndRocketText;
        [SerializeField] private TMP_Text selectedGroundStationText;
        [Tooltip("Assign up to 6 2D UI Box Images representing the 1U-6U CubeSat internal bays")]
        [SerializeField] private Image[] cubeSatSlotBoxes;
        [Tooltip("Labels inside each of the 6 2D CubeSat slot boxes")]
        [SerializeField] private TMP_Text[] cubeSatSlotLabels;

        [Header("4. Connected Live Resource Panel & Navigation")]
        [SerializeField] private ResourceBarsUI resourceBarsUI;
        [SerializeField] private Button proceedToReviewButton;

        private readonly List<PartCardUI> _spawnedCards = new List<PartCardUI>();
        private MissionSegment _currentSegment = MissionSegment.Launch;

        /// <summary>
        /// The active `SpacecraftDesign` shared with Member 1's calculator and Member 3's GameManager.
        /// </summary>
        public SpacecraftDesign CurrentDesign { get; private set; }

        /// <summary>
        /// Most recent evaluation output from `MissionCalculator.EvaluateMission(CurrentDesign)`.
        /// </summary>
        public MissionEvaluationResult LastEvaluation { get; private set; }

        // Events for Member 3's GameManager
        public event Action<SpacecraftDesign, MissionEvaluationResult> OnDesignUpdated;
        public event Action OnProceedToCML4ReviewRequested;

        private void Awake()
        {
            BindSegmentTabButtons();

            if (proceedToReviewButton != null)
            {
                proceedToReviewButton.onClick.AddListener(() => OnProceedToCML4ReviewRequested?.Invoke());
            }
        }

        private void Start()
        {
            if (CurrentDesign == null)
            {
                InitializeWithDefaultDesign();
            }

            SwitchSegmentTab(MissionSegment.Launch);
            RecalculateAndRefreshUI();
        }

        /// <summary>
        /// Called by Member 3's GameManager when entering Phase 2 (`CML2_3_SpacecraftBuilder`).
        /// Binds the active `SpacecraftDesign` (and its `SelectedMission`) to the Builder UI.
        /// </summary>
        public void BindSpacecraftDesign(SpacecraftDesign design)
        {
            CurrentDesign = design ?? new SpacecraftDesign();

            // Ensure default starter parts are populated if slots are empty
            EnsureDefaultPartsSelected(CurrentDesign);

            SwitchSegmentTab(MissionSegment.Launch);
            RecalculateAndRefreshUI();
        }

        private void BindSegmentTabButtons()
        {
            if (launchSegmentTabButton != null)
                launchSegmentTabButton.onClick.AddListener(() => SwitchSegmentTab(MissionSegment.Launch));

            if (spaceSegmentTabButton != null)
                spaceSegmentTabButton.onClick.AddListener(() => SwitchSegmentTab(MissionSegment.Space));

            if (groundSegmentTabButton != null)
                groundSegmentTabButton.onClick.AddListener(() => SwitchSegmentTab(MissionSegment.Ground));

            if (userSegmentTabButton != null)
                userSegmentTabButton.onClick.AddListener(() => SwitchSegmentTab(MissionSegment.User));
        }

        /// <summary>
        /// Switches the active NASA Mission Segment tab (Launch, Space, Ground, User)
        /// and rebuilds the clickable part cards list from `PartsDatabase`.
        /// </summary>
        public void SwitchSegmentTab(MissionSegment segment)
        {
            _currentSegment = segment;

            if (activeSegmentHeaderText != null)
            {
                activeSegmentHeaderText.text = GetSegmentDescription(segment);
            }

            RebuildPartCardList(segment);
        }

        private void RebuildPartCardList(MissionSegment segment)
        {
            foreach (PartCardUI card in _spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _spawnedCards.Clear();

            if (partCardListContainer == null || partCardPrefab == null) return;

            // Fetch parts for this segment from Member 1's PartsDatabase
            List<ComponentPart> segmentParts = PartsDatabase.GetPartsBySegment(segment);

            // In the User Segment tab, show Science Payloads & Ground Stations so players can review science traceability
            if (segment == MissionSegment.User && segmentParts.Count == 0)
            {
                segmentParts = PartsDatabase.GetPartsByCategory(PartCategory.Payload);
            }

            foreach (ComponentPart part in segmentParts)
            {
                PartCardUI cardInstance = Instantiate(partCardPrefab, partCardListContainer);
                bool isEquipped = IsPartEquippedInDesign(part);
                cardInstance.SetupCard(part, isEquipped, HandlePartCardClicked);
                _spawnedCards.Add(cardInstance);
            }
        }

        private void HandlePartCardClicked(ComponentPart selectedPart)
        {
            if (CurrentDesign == null) InitializeWithDefaultDesign();

            // Use Member 1's extension method to equip the part into the matching slot!
            CurrentDesign.EquipPart(selectedPart);

            // Refresh card highlight states in the current tab
            RebuildPartCardList(_currentSegment);

            // Immediately recalculate via Member 1's MissionCalculator and update UI
            RecalculateAndRefreshUI();
        }

        private bool IsPartEquippedInDesign(ComponentPart part)
        {
            if (CurrentDesign == null || part == null) return false;

            List<ComponentPart> equipped = CurrentDesign.GetAllSelectedParts();
            foreach (ComponentPart p in equipped)
            {
                if (p != null && string.Equals(p.Id, part.Id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Evaluates `CurrentDesign` using Member 1's `MissionCalculator.EvaluateMission(...)`,
        /// updates the 2D CubeSat Slot Blueprint (1U/3U/6U), and refreshes `ResourceBarsUI`.
        /// </summary>
        public void RecalculateAndRefreshUI()
        {
            if (CurrentDesign == null) InitializeWithDefaultDesign();

            LastEvaluation = MissionCalculator.EvaluateMission(CurrentDesign);

            Update2DCubeSatBlueprint();

            if (resourceBarsUI != null)
            {
                resourceBarsUI.RefreshDashboard(LastEvaluation);
            }

            OnDesignUpdated?.Invoke(CurrentDesign, LastEvaluation);
        }

        private void Update2DCubeSatBlueprint()
        {
            ComponentPart chassis = CurrentDesign.SelectedChassis;
            int totalSlots = GetSlotCountForChassis(chassis);

            string missionTitle = CurrentDesign.SelectedMission != null
                ? CurrentDesign.SelectedMission.Title
                : "NASA CubeSat Mission Architect";

            if (chassisTitleText != null)
            {
                string chassisLabel = chassis != null
                    ? $"{chassis.Name} ({totalSlots}U)"
                    : "No Chassis Selected";
                chassisTitleText.text = $"{missionTitle}\nBlueprint: {chassisLabel}";
            }

            if (selectedOrbitAndRocketText != null)
            {
                string lvName = CurrentDesign.SelectedLaunchVehicle != null ? CurrentDesign.SelectedLaunchVehicle.Name : "None";
                string orbName = CurrentDesign.SelectedOrbit != null ? CurrentDesign.SelectedOrbit.Name : "None";
                selectedOrbitAndRocketText.text = $"Launch: {lvName}\nOrbit: {orbName}";
            }

            if (selectedGroundStationText != null)
            {
                string gsName = CurrentDesign.SelectedGroundStation != null ? CurrentDesign.SelectedGroundStation.Name : "None";
                selectedGroundStationText.text = $"Ground Station: {gsName}";
            }

            // Collect installed Space Segment subsystems to display inside the 2D CubeSat bays
            List<string> installedSubsystems = new List<string>();
            if (CurrentDesign.SelectedPayload != null)
                installedSubsystems.Add($"[Payload]\n{CurrentDesign.SelectedPayload.Name}");
            if (CurrentDesign.SelectedSolarPanel != null)
                installedSubsystems.Add($"[Solar]\n{CurrentDesign.SelectedSolarPanel.Name}");
            if (CurrentDesign.SelectedBattery != null)
                installedSubsystems.Add($"[Battery]\n{CurrentDesign.SelectedBattery.Name}");
            if (CurrentDesign.SelectedADCS != null)
                installedSubsystems.Add($"[ADCS]\n{CurrentDesign.SelectedADCS.Name}");
            if (CurrentDesign.SelectedComms != null)
                installedSubsystems.Add($"[Comms]\n{CurrentDesign.SelectedComms.Name}");

            if (cubeSatSlotBoxes == null) return;

            for (int i = 0; i < cubeSatSlotBoxes.Length; i++)
            {
                if (cubeSatSlotBoxes[i] == null) continue;

                bool slotExistsOnChassis = (i < totalSlots);
                cubeSatSlotBoxes[i].gameObject.SetActive(slotExistsOnChassis);

                if (!slotExistsOnChassis) continue;

                bool hasModuleInSlot = (i < installedSubsystems.Count);
                cubeSatSlotBoxes[i].color = hasModuleInSlot
                    ? new Color(0.16f, 0.45f, 0.65f, 0.9f)
                    : new Color(0.20f, 0.24f, 0.30f, 0.6f);

                if (cubeSatSlotLabels != null && i < cubeSatSlotLabels.Length && cubeSatSlotLabels[i] != null)
                {
                    cubeSatSlotLabels[i].text = hasModuleInSlot
                        ? $"Bay {i + 1}U:\n{installedSubsystems[i]}"
                        : $"Bay {i + 1}U:\n[Structural Margin / Bus]";
                }
            }
        }

        private int GetSlotCountForChassis(ComponentPart chassis)
        {
            if (chassis == null || string.IsNullOrEmpty(chassis.Id)) return 3;
            string id = chassis.Id.ToUpperInvariant();
            if (id.Contains("1U")) return 1;
            if (id.Contains("6U")) return 6;
            return 3;
        }

        private string GetSegmentDescription(MissionSegment segment)
        {
            switch (segment)
            {
                case MissionSegment.Launch:
                    return "1. LAUNCH SEGMENT — Select CubeSat Chassis (1U/3U/6U), Rocket Dispenser & GMAT Orbit";
                case MissionSegment.Space:
                    return "2. SPACE SEGMENT — Select Science Payload, Solar Array, UL Battery, ADCS & Radio";
                case MissionSegment.Ground:
                    return "3. GROUND SEGMENT — Select Ground Station Network to Close the Telecom Link";
                case MissionSegment.User:
                    return "4. USER SEGMENT — Verify Science Payload Traceability to Mission Objectives";
                default:
                    return "SPACECRAFT BUILDER";
            }
        }

        private void InitializeWithDefaultDesign()
        {
            CurrentDesign = new SpacecraftDesign
            {
                SelectedMission = new MissionProfile
                {
                    MissionId = "MISSION_2_BISONSAT",
                    Title = "Mission 2: BisonSat / Earth Forest Imager",
                    Difficulty = MissionDifficulty.Medium_EarthObserver,
                    BudgetCapThousands = 300f,
                    TargetScienceScore = 85,
                    RequiredOrbitId = "ORBIT_SSO",
                    RequiredMassMarginPct = 20f,
                    RequiredPowerMarginPct = 20f
                },
                HasFccIaruRfLicense = true,
                HasNoaaRemoteSensingLicense = true,
                CompletedVibrationTest = true,
                CompletedTvacBakeoutTest = true,
                CompletedDitlSoftwareTest = true,
                UsedDoubleBurnWire = true
            };

            EnsureDefaultPartsSelected(CurrentDesign);
        }

        private void EnsureDefaultPartsSelected(SpacecraftDesign design)
        {
            if (design.SelectedChassis == null)
                design.SelectedChassis = PartsDatabase.GetPartById("CHASSIS_3U");
            if (design.SelectedLaunchVehicle == null)
                design.SelectedLaunchVehicle = PartsDatabase.GetPartById("LAUNCH_RIDESHARE_LEO");
            if (design.SelectedOrbit == null)
                design.SelectedOrbit = PartsDatabase.GetPartById("ORBIT_SSO");
            if (design.SelectedPayload == null)
                design.SelectedPayload = PartsDatabase.GetPartById("PAYLOAD_EARTH_CAMERA");
            if (design.SelectedSolarPanel == null)
                design.SelectedSolarPanel = PartsDatabase.GetPartById("SOLAR_3U_DEPLOYABLE");
            if (design.SelectedBattery == null)
                design.SelectedBattery = PartsDatabase.GetPartById("BATTERY_UL_12WH");
            if (design.SelectedADCS == null)
                design.SelectedADCS = PartsDatabase.GetPartById("ADCS_MAGNETORQUERS");
            if (design.SelectedComms == null)
                design.SelectedComms = PartsDatabase.GetPartById("COMMS_SBAND");
            if (design.SelectedGroundStation == null)
                design.SelectedGroundStation = PartsDatabase.GetPartById("GROUND_SBAND_DISH");
        }
    }
}
