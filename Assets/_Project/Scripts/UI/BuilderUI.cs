using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceMissionGame.Core;

namespace SpaceMissionGame.UI
{
    /// <summary>
    /// [Member 2 - UI] Main controller for Phase 2 & 3 (CML 1-4): Spacecraft & Segment Builder (`BuilderUI.cs`).
    /// 
    /// Full Interactive Features:
    /// 1. Destroys any static Edit-Mode preview cards on Start() so 100% of spawned part cards are live and clickable.
    /// 2. Clicking the 4 Top Segment Tabs visibly highlights the active tab, switches the left part list,
    ///    and updates the 2D Visual Stage (Launch Rocket/Planet, CubeSat Assembly, Ground Dish, or Pre-Launch Tests).
    /// 3. Clicking 'EQUIP PART' on any card swaps the component on `CurrentDesign`, animates the 2D Spacecraft
    ///    visual parts (Chassis 1U/3U/6U, Solar Wings, Payload Lens, Antenna, Planet, Ground Dish), updates the
    ///    9 clickable loadout slots, and refreshes the 6 live resource bars on the right!
    /// </summary>
    public class BuilderUI : MonoBehaviour
    {
        [Header("1. NASA 4-Segment Tab Buttons")]
        [SerializeField] private Button launchSegmentTabButton;
        [SerializeField] private Button spaceSegmentTabButton;
        [SerializeField] private Button groundSegmentTabButton;
        [SerializeField] private Button userSegmentTabButton;
        [SerializeField] private TMP_Text activeSegmentHeaderText;

        [Header("2. Mission Preset Selector Buttons (CML 1)")]
        [SerializeField] private Button mission1EasyButton;
        [SerializeField] private Button mission2MediumButton;
        [SerializeField] private Button mission3HardButton;

        [Header("3. Part Cards Scroll List")]
        [SerializeField] private Transform partCardListContainer;
        [SerializeField] private PartCardUI partCardPrefab;

        [Header("4. Interactive 2D Visual Stage (Changes with Parts & Tabs!)")]
        [SerializeField] private TMP_Text chassisTitleText;
        [SerializeField] private TMP_Text visualStageModeText;
        [SerializeField] private TMP_Text selectedOrbitAndRocketText;
        [SerializeField] private TMP_Text selectedGroundStationText;

        [Tooltip("2D Orbiting Planet Backdrop (changes color/size for ISS LEO, Polar SSO, or Moon)")]
        [SerializeField] private RectTransform visualPlanetBackdrop;
        [SerializeField] private TMP_Text visualPlanetLabel;

        [Tooltip("Visual 2D Satellite Hull that resizes for 1U / 3U / 6U")]
        [SerializeField] private RectTransform visualSatelliteHull;
        [SerializeField] private TMP_Text visualHullSpecLabel;
        [SerializeField] private RectTransform visualLeftSolarWing;
        [SerializeField] private RectTransform visualRightSolarWing;
        [SerializeField] private Image visualPayloadLens;
        [SerializeField] private TMP_Text visualPayloadLabel;
        [SerializeField] private RectTransform visualTopAntenna;
        [SerializeField] private TMP_Text visualAntennaLabel;
        [SerializeField] private RectTransform visualGroundDish;
        [SerializeField] private TMP_Text visualGroundDishLabel;

        [Tooltip("9 Clickable Loadout Slot Boxes (Chassis, Rocket, Orbit, Payload, Solar, Battery, ADCS, Comms, Ground)")]
        [SerializeField] private Image[] cubeSatSlotBoxes;
        [SerializeField] private TMP_Text[] cubeSatSlotLabels;
        [SerializeField] private Button[] cubeSatSlotButtons;

        [Header("5. Pre-Launch Licensing & Environmental Test Toggles (CML 4)")]
        [SerializeField] private Button toggleFccLicenseButton;
        [SerializeField] private Button toggleNoaaLicenseButton;
        [SerializeField] private Button toggleVibrationTestButton;
        [SerializeField] private Button toggleTvacTestButton;
        [SerializeField] private Button toggleDitlTestButton;
        [SerializeField] private Button toggleDoubleBurnWireButton;

        [Header("6. Connected Live Resource Panel & Navigation")]
        [SerializeField] private ResourceBarsUI resourceBarsUI;
        [SerializeField] private Button proceedToReviewButton;

        private readonly List<PartCardUI> _spawnedCards = new List<PartCardUI>();
        private MissionSegment _currentSegment = MissionSegment.Launch;
        private PartCategory? _activeCategoryFilter = null;
        private int _currentMissionIndex = 1;

        private static readonly PartCategory[] SlotOrder = new PartCategory[]
        {
            PartCategory.Chassis,
            PartCategory.LaunchVehicle,
            PartCategory.Orbit,
            PartCategory.Payload,
            PartCategory.SolarPanel,
            PartCategory.Battery,
            PartCategory.ADCS,
            PartCategory.Communications,
            PartCategory.GroundStation
        };

        public SpacecraftDesign CurrentDesign { get; private set; }
        public MissionEvaluationResult LastEvaluation { get; private set; }

        public event Action<SpacecraftDesign, MissionEvaluationResult> OnDesignUpdated;
        public event Action OnProceedToCML4ReviewRequested;

        private void Awake()
        {
            BindSegmentTabButtons();
            BindMissionPresetButtons();
            BindSlotFilterButtons();
            BindVerificationToggleButtons();

            if (proceedToReviewButton != null)
            {
                proceedToReviewButton.onClick.RemoveAllListeners();
                proceedToReviewButton.onClick.AddListener(() => OnProceedToCML4ReviewRequested?.Invoke());
            }
        }

        private void Start()
        {
            if (CurrentDesign == null)
            {
                SelectMissionPreset(1);
            }
            else
            {
                SwitchSegmentTab(MissionSegment.Launch);
                RecalculateAndRefreshUI();
            }
        }

        public void BindSpacecraftDesign(SpacecraftDesign design)
        {
            CurrentDesign = design ?? new SpacecraftDesign();
            EnsureDefaultPartsSelected(CurrentDesign);
            SwitchSegmentTab(MissionSegment.Launch);
            RecalculateAndRefreshUI();
        }

        private void BindSegmentTabButtons()
        {
            if (launchSegmentTabButton != null)
            {
                launchSegmentTabButton.onClick.RemoveAllListeners();
                launchSegmentTabButton.onClick.AddListener(() => SwitchSegmentTab(MissionSegment.Launch));
            }

            if (spaceSegmentTabButton != null)
            {
                spaceSegmentTabButton.onClick.RemoveAllListeners();
                spaceSegmentTabButton.onClick.AddListener(() => SwitchSegmentTab(MissionSegment.Space));
            }

            if (groundSegmentTabButton != null)
            {
                groundSegmentTabButton.onClick.RemoveAllListeners();
                groundSegmentTabButton.onClick.AddListener(() => SwitchSegmentTab(MissionSegment.Ground));
            }

            if (userSegmentTabButton != null)
            {
                userSegmentTabButton.onClick.RemoveAllListeners();
                userSegmentTabButton.onClick.AddListener(() => SwitchSegmentTab(MissionSegment.User));
            }
        }

        private void BindMissionPresetButtons()
        {
            if (mission1EasyButton != null)
            {
                mission1EasyButton.onClick.RemoveAllListeners();
                mission1EasyButton.onClick.AddListener(() => SelectMissionPreset(0));
            }
            if (mission2MediumButton != null)
            {
                mission2MediumButton.onClick.RemoveAllListeners();
                mission2MediumButton.onClick.AddListener(() => SelectMissionPreset(1));
            }
            if (mission3HardButton != null)
            {
                mission3HardButton.onClick.RemoveAllListeners();
                mission3HardButton.onClick.AddListener(() => SelectMissionPreset(2));
            }
        }

        private void BindSlotFilterButtons()
        {
            if (cubeSatSlotButtons == null) return;

            for (int i = 0; i < cubeSatSlotButtons.Length && i < SlotOrder.Length; i++)
            {
                PartCategory cat = SlotOrder[i];
                if (cubeSatSlotButtons[i] != null)
                {
                    cubeSatSlotButtons[i].onClick.RemoveAllListeners();
                    cubeSatSlotButtons[i].onClick.AddListener(() => FilterByPartCategory(cat));
                }
            }
        }

        private void BindVerificationToggleButtons()
        {
            if (toggleFccLicenseButton != null)
            {
                toggleFccLicenseButton.onClick.RemoveAllListeners();
                toggleFccLicenseButton.onClick.AddListener(() =>
                {
                    CurrentDesign.HasFccIaruRfLicense = !CurrentDesign.HasFccIaruRfLicense;
                    RecalculateAndRefreshUI();
                });
            }

            if (toggleNoaaLicenseButton != null)
            {
                toggleNoaaLicenseButton.onClick.RemoveAllListeners();
                toggleNoaaLicenseButton.onClick.AddListener(() =>
                {
                    CurrentDesign.HasNoaaRemoteSensingLicense = !CurrentDesign.HasNoaaRemoteSensingLicense;
                    RecalculateAndRefreshUI();
                });
            }

            if (toggleVibrationTestButton != null)
            {
                toggleVibrationTestButton.onClick.RemoveAllListeners();
                toggleVibrationTestButton.onClick.AddListener(() =>
                {
                    CurrentDesign.CompletedVibrationTest = !CurrentDesign.CompletedVibrationTest;
                    RecalculateExtraTestingCost();
                    RecalculateAndRefreshUI();
                });
            }

            if (toggleTvacTestButton != null)
            {
                toggleTvacTestButton.onClick.RemoveAllListeners();
                toggleTvacTestButton.onClick.AddListener(() =>
                {
                    CurrentDesign.CompletedTvacBakeoutTest = !CurrentDesign.CompletedTvacBakeoutTest;
                    RecalculateExtraTestingCost();
                    RecalculateAndRefreshUI();
                });
            }

            if (toggleDitlTestButton != null)
            {
                toggleDitlTestButton.onClick.RemoveAllListeners();
                toggleDitlTestButton.onClick.AddListener(() =>
                {
                    CurrentDesign.CompletedDitlSoftwareTest = !CurrentDesign.CompletedDitlSoftwareTest;
                    RecalculateExtraTestingCost();
                    RecalculateAndRefreshUI();
                });
            }

            if (toggleDoubleBurnWireButton != null)
            {
                toggleDoubleBurnWireButton.onClick.RemoveAllListeners();
                toggleDoubleBurnWireButton.onClick.AddListener(() =>
                {
                    CurrentDesign.UsedDoubleBurnWire = !CurrentDesign.UsedDoubleBurnWire;
                    RecalculateExtraTestingCost();
                    RecalculateAndRefreshUI();
                });
            }
        }

        private void RecalculateExtraTestingCost()
        {
            if (CurrentDesign == null) return;
            float extraCost = 0f;
            if (CurrentDesign.CompletedVibrationTest) extraCost += 8f;
            if (CurrentDesign.CompletedTvacBakeoutTest) extraCost += 6f;
            if (CurrentDesign.CompletedDitlSoftwareTest) extraCost += 4f;
            if (CurrentDesign.UsedDoubleBurnWire) extraCost += 3f;
            CurrentDesign.ExtraTestingCostThousands = extraCost;
        }

        public void SelectMissionPreset(int index)
        {
            _currentMissionIndex = index;

            CurrentDesign = new SpacecraftDesign
            {
                HasFccIaruRfLicense = true,
                HasNoaaRemoteSensingLicense = true,
                CompletedVibrationTest = true,
                CompletedTvacBakeoutTest = true,
                CompletedDitlSoftwareTest = true,
                UsedDoubleBurnWire = true
            };

            if (index == 0)
            {
                CurrentDesign.SelectedMission = new MissionProfile
                {
                    MissionId = "MISSION_1_BIONUTRIENTS",
                    Title = "Mission 1: BioNutrients-2 (ISS Biology 1U)",
                    Difficulty = MissionDifficulty.Easy_ISSCubeSat,
                    BudgetCapThousands = 230f,
                    TargetScienceScore = 70,
                    RequiredOrbitId = "ORBIT_ISS",
                    RequiredMassMarginPct = 15f,
                    RequiredPowerMarginPct = 8f
                };
                CurrentDesign.SelectedChassis = PartsDatabase.GetPartById("CHASSIS_1U");
                CurrentDesign.SelectedLaunchVehicle = PartsDatabase.GetPartById("LAUNCH_ISS");
                CurrentDesign.SelectedOrbit = PartsDatabase.GetPartById("ORBIT_ISS");
                CurrentDesign.SelectedPayload = PartsDatabase.GetPartById("PAYLOAD_BIONUTRIENTS");
                CurrentDesign.SelectedSolarPanel = PartsDatabase.GetPartById("SOLAR_1U_BODY");
                CurrentDesign.SelectedBattery = PartsDatabase.GetPartById("BATTERY_UL_12WH");
                CurrentDesign.SelectedADCS = PartsDatabase.GetPartById("ADCS_PASSIVE_MAGNET");
                CurrentDesign.SelectedComms = PartsDatabase.GetPartById("COMMS_UHF");
                CurrentDesign.SelectedGroundStation = PartsDatabase.GetPartById("GROUND_UHF_YAGI");
            }
            else if (index == 2)
            {
                CurrentDesign.SelectedMission = new MissionProfile
                {
                    MissionId = "MISSION_3_LUNAR",
                    Title = "Mission 3: Artemis Lunar Ice Pathfinder (6U Deep Space)",
                    Difficulty = MissionDifficulty.Hard_LunarPathfinder,
                    BudgetCapThousands = 950f,
                    TargetScienceScore = 140,
                    RequiredOrbitId = "ORBIT_LUNAR",
                    RequiredMassMarginPct = 20f,
                    RequiredPowerMarginPct = 20f
                };
                CurrentDesign.SelectedChassis = PartsDatabase.GetPartById("CHASSIS_6U");
                CurrentDesign.SelectedLaunchVehicle = PartsDatabase.GetPartById("LAUNCH_LUNAR_6U");
                CurrentDesign.SelectedOrbit = PartsDatabase.GetPartById("ORBIT_LUNAR");
                CurrentDesign.SelectedPayload = PartsDatabase.GetPartById("PAYLOAD_LUNAR_SPECTROMETER");
                CurrentDesign.SelectedSolarPanel = PartsDatabase.GetPartById("SOLAR_6U_GIMBAL");
                CurrentDesign.SelectedBattery = PartsDatabase.GetPartById("BATTERY_UL_40WH");
                CurrentDesign.SelectedADCS = PartsDatabase.GetPartById("ADCS_REACTION_WHEELS");
                CurrentDesign.SelectedComms = PartsDatabase.GetPartById("COMMS_XBAND_IRIS");
                CurrentDesign.SelectedGroundStation = PartsDatabase.GetPartById("GROUND_NASA_DSN");
            }
            else
            {
                CurrentDesign.SelectedMission = new MissionProfile
                {
                    MissionId = "MISSION_2_BISONSAT",
                    Title = "Mission 2: BisonSat / Earth Forest Imager (3U SSO)",
                    Difficulty = MissionDifficulty.Medium_EarthObserver,
                    BudgetCapThousands = 450f,
                    TargetScienceScore = 95,
                    RequiredOrbitId = "ORBIT_SSO",
                    RequiredMassMarginPct = 20f,
                    RequiredPowerMarginPct = 20f
                };
                CurrentDesign.SelectedChassis = PartsDatabase.GetPartById("CHASSIS_3U");
                CurrentDesign.SelectedLaunchVehicle = PartsDatabase.GetPartById("LAUNCH_RIDESHARE_LEO");
                CurrentDesign.SelectedOrbit = PartsDatabase.GetPartById("ORBIT_SSO");
                CurrentDesign.SelectedPayload = PartsDatabase.GetPartById("PAYLOAD_EARTH_CAMERA");
                CurrentDesign.SelectedSolarPanel = PartsDatabase.GetPartById("SOLAR_3U_DEPLOYABLE");
                CurrentDesign.SelectedBattery = PartsDatabase.GetPartById("BATTERY_UL_12WH");
                CurrentDesign.SelectedADCS = PartsDatabase.GetPartById("ADCS_MAGNETORQUERS");
                CurrentDesign.SelectedComms = PartsDatabase.GetPartById("COMMS_SBAND");
                CurrentDesign.SelectedGroundStation = PartsDatabase.GetPartById("GROUND_SBAND_DISH");
            }

            UpdateMissionButtonHighlights();
            RecalculateExtraTestingCost();
            RebuildPartCardList();
            RecalculateAndRefreshUI();
            TriggerVisualPulse();
        }

        public void SwitchSegmentTab(MissionSegment segment)
        {
            _currentSegment = segment;
            _activeCategoryFilter = null;

            if (activeSegmentHeaderText != null)
            {
                activeSegmentHeaderText.text = GetSegmentDescription(segment);
            }

            UpdateSegmentTabButtonHighlights();
            RebuildPartCardList();
            Update2DCubeSatBlueprint();
        }

        public void FilterByPartCategory(PartCategory category)
        {
            _activeCategoryFilter = category;

            if (activeSegmentHeaderText != null)
            {
                activeSegmentHeaderText.text = $"SLOT SELECTED: [{category.ToString().ToUpper()}] — Click 'EQUIP PART' on any card below to swap!";
            }

            UpdateSegmentTabButtonHighlights();
            RebuildPartCardList();
            Update2DCubeSatBlueprint();
        }

        private void RebuildPartCardList()
        {
            _spawnedCards.Clear();

            if (partCardListContainer == null || partCardPrefab == null) return;

            // CRITICAL FIX: Destroy ALL child objects inside partCardListContainer
            // (including any static Edit-Mode preview cards saved in the prefab/scene!)
            for (int i = partCardListContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = partCardListContainer.GetChild(i);
                if (child != null && child != partCardPrefab.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            List<ComponentPart> partsToShow;

            if (_activeCategoryFilter.HasValue)
            {
                partsToShow = PartsDatabase.GetPartsByCategory(_activeCategoryFilter.Value);
            }
            else
            {
                partsToShow = PartsDatabase.GetPartsBySegment(_currentSegment);
                if (_currentSegment == MissionSegment.User && partsToShow.Count == 0)
                {
                    partsToShow = new List<ComponentPart>();
                    partsToShow.AddRange(PartsDatabase.GetPartsByCategory(PartCategory.Payload));
                    partsToShow.AddRange(PartsDatabase.GetPartsByCategory(PartCategory.Orbit));
                    partsToShow.AddRange(PartsDatabase.GetPartsByCategory(PartCategory.GroundStation));
                }
            }

            foreach (ComponentPart part in partsToShow)
            {
                PartCardUI cardInstance = Instantiate(partCardPrefab, partCardListContainer);
                cardInstance.gameObject.SetActive(true);

                bool isEquipped = IsPartEquippedInDesign(part);
                cardInstance.SetupCard(part, isEquipped, HandlePartCardClicked);
                _spawnedCards.Add(cardInstance);
            }
        }

        private void HandlePartCardClicked(ComponentPart selectedPart)
        {
            if (CurrentDesign == null) SelectMissionPreset(1);

            CurrentDesign.EquipPart(selectedPart);

            if (activeSegmentHeaderText != null)
            {
                activeSegmentHeaderText.text = $"EQUIPPED: {selectedPart.Name}! Watch the 2D Spacecraft & Resource Bars update.";
            }

            RebuildPartCardList();
            RecalculateAndRefreshUI();
            TriggerVisualPulse();
        }

        private void TriggerVisualPulse()
        {
            if (visualSatelliteHull != null && gameObject.activeInHierarchy)
            {
                StopAllCoroutines();
                StartCoroutine(AnimateHullPulse());
            }
        }

        private IEnumerator AnimateHullPulse()
        {
            if (visualSatelliteHull == null) yield break;
            visualSatelliteHull.localScale = new Vector3(1.14f, 1.14f, 1f);
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                visualSatelliteHull.localScale = Vector3.Lerp(new Vector3(1.14f, 1.14f, 1f), Vector3.one, t / 0.18f);
                yield return null;
            }
            visualSatelliteHull.localScale = Vector3.one;
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

        public void RecalculateAndRefreshUI()
        {
            if (CurrentDesign == null) SelectMissionPreset(1);

            LastEvaluation = MissionCalculator.EvaluateMission(CurrentDesign);

            Update2DCubeSatBlueprint();
            UpdateVerificationButtonVisuals();

            if (resourceBarsUI != null)
            {
                resourceBarsUI.RefreshDashboard(LastEvaluation);
            }

            OnDesignUpdated?.Invoke(CurrentDesign, LastEvaluation);
        }

        private void UpdateSegmentTabButtonHighlights()
        {
            Color activeTab = new Color(0.18f, 0.56f, 0.88f, 1f);
            Color idleTab = new Color(0.12f, 0.22f, 0.34f, 1f);

            SetButtonColor(launchSegmentTabButton, !_activeCategoryFilter.HasValue && _currentSegment == MissionSegment.Launch ? activeTab : idleTab);
            SetButtonColor(spaceSegmentTabButton, !_activeCategoryFilter.HasValue && _currentSegment == MissionSegment.Space ? activeTab : idleTab);
            SetButtonColor(groundSegmentTabButton, !_activeCategoryFilter.HasValue && _currentSegment == MissionSegment.Ground ? activeTab : idleTab);
            SetButtonColor(userSegmentTabButton, !_activeCategoryFilter.HasValue && _currentSegment == MissionSegment.User ? activeTab : idleTab);
        }

        private void UpdateMissionButtonHighlights()
        {
            Color activeMission = new Color(0.18f, 0.65f, 0.42f, 1f);
            Color idleMission = new Color(0.14f, 0.20f, 0.30f, 1f);

            SetButtonColor(mission1EasyButton, _currentMissionIndex == 0 ? activeMission : idleMission);
            SetButtonColor(mission2MediumButton, _currentMissionIndex == 1 ? activeMission : idleMission);
            SetButtonColor(mission3HardButton, _currentMissionIndex == 2 ? activeMission : idleMission);
        }

        private void SetButtonColor(Button btn, Color c)
        {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img != null) img.color = c;
        }

        private void Update2DCubeSatBlueprint()
        {
            ComponentPart chassis = CurrentDesign.SelectedChassis;
            ComponentPart orbit = CurrentDesign.SelectedOrbit;
            ComponentPart rocket = CurrentDesign.SelectedLaunchVehicle;
            ComponentPart payload = CurrentDesign.SelectedPayload;
            ComponentPart solar = CurrentDesign.SelectedSolarPanel;
            ComponentPart comms = CurrentDesign.SelectedComms;
            ComponentPart ground = CurrentDesign.SelectedGroundStation;

            int totalUnits = GetSlotCountForChassis(chassis);

            string missionTitle = CurrentDesign.SelectedMission != null
                ? CurrentDesign.SelectedMission.Title
                : "NASA CubeSat Mission Architect";

            if (chassisTitleText != null)
            {
                string reqOrbit = CurrentDesign.SelectedMission != null ? CurrentDesign.SelectedMission.RequiredOrbitId : "ANY";
                int reqSci = CurrentDesign.SelectedMission != null ? CurrentDesign.SelectedMission.TargetScienceScore : 70;
                float cap = CurrentDesign.SelectedMission != null ? CurrentDesign.SelectedMission.BudgetCapThousands : 300f;
                chassisTitleText.text = $"{missionTitle}\nTarget: {reqOrbit} | Min Science: {reqSci} pts | Grant Cap: ${cap:F0}k";
            }

            if (visualStageModeText != null)
            {
                string viewTitle = _activeCategoryFilter.HasValue
                    ? $"2D LIVE PREVIEW — INSPECTING [{_activeCategoryFilter.Value.ToString().ToUpper()}]"
                    : $"2D LIVE PREVIEW — {_currentSegment.ToString().ToUpper()} SEGMENT VIEW";
                visualStageModeText.text = viewTitle;
            }

            // 1. Update 2D Planet/Orbit Backdrop (Blue ISS LEO, Cyan Polar SSO, Gray Moon)
            if (visualPlanetBackdrop != null)
            {
                Image planetImg = visualPlanetBackdrop.GetComponent<Image>();
                string orbId = orbit != null ? orbit.Id.ToUpperInvariant() : "ORBIT_SSO";
                if (planetImg != null)
                {
                    if (orbId.Contains("LUNAR"))
                        planetImg.color = new Color(0.55f, 0.58f, 0.64f, 0.45f); // Lunar gray
                    else if (orbId.Contains("SSO"))
                        planetImg.color = new Color(0.12f, 0.55f, 0.72f, 0.40f); // Polar cyan
                    else
                        planetImg.color = new Color(0.15f, 0.38f, 0.78f, 0.40f); // ISS Earth blue
                }
                if (visualPlanetLabel != null)
                {
                    string rktShort = rocket != null ? rocket.Name.Split(' ')[0] : "Rocket";
                    string orbShort = orbit != null ? orbit.Name.Split('(')[0].Trim() : "Orbit";
                    visualPlanetLabel.text = $"{orbShort}\nLauncher: {rktShort}";
                }
            }

            // 2. Update 2D CubeSat Hull (1U small square, 3U tall tower, 6U wide double bus)
            if (visualSatelliteHull != null)
            {
                if (totalUnits == 1)
                    visualSatelliteHull.sizeDelta = new Vector2(86f, 86f);
                else if (totalUnits == 6)
                    visualSatelliteHull.sizeDelta = new Vector2(165f, 150f);
                else
                    visualSatelliteHull.sizeDelta = new Vector2(96f, 150f);

                Image hullImg = visualSatelliteHull.GetComponent<Image>();
                if (hullImg != null)
                {
                    hullImg.color = (chassis != null && chassis.IsHighMeltingPointMetal)
                        ? new Color(0.85f, 0.32f, 0.26f, 1f) // Titanium ODAR warning red-bronze
                        : new Color(0.80f, 0.84f, 0.90f, 1f); // Aluminum silver
                }

                if (visualHullSpecLabel != null)
                {
                    string mat = (chassis != null && chassis.IsHighMeltingPointMetal) ? "TITANIUM!" : "ALUMINUM";
                    visualHullSpecLabel.text = $"{totalUnits}U BUS\n({mat})";
                }
            }

            // 3. Update 2D Solar Wings (Flush 1U cells vs 3U Blue Wings vs 6U Gold Gimbal Wings)
            bool hasDeployableWings = solar != null && solar.UsesDeployableBurnWire;
            bool is6UGimbal = solar != null && solar.PowerGeneratedW >= 40f;
            float wingWidth = is6UGimbal ? 150f : (hasDeployableWings ? 105f : 18f);
            float wingHeight = is6UGimbal ? 54f : (hasDeployableWings ? 40f : 72f);
            Color wingColor = is6UGimbal
                ? new Color(0.92f, 0.72f, 0.15f, 1f) // Gold-trimmed 6U gimbal array
                : new Color(0.14f, 0.48f, 0.90f, 1f); // GaAs solar blue

            if (visualLeftSolarWing != null)
            {
                visualLeftSolarWing.gameObject.SetActive(solar != null);
                visualLeftSolarWing.sizeDelta = new Vector2(wingWidth, wingHeight);
                visualLeftSolarWing.anchoredPosition = new Vector2(-(visualSatelliteHull.sizeDelta.x * 0.5f + wingWidth * 0.5f + 4f), 0f);
                Image wImg = visualLeftSolarWing.GetComponent<Image>();
                if (wImg != null) wImg.color = wingColor;
            }
            if (visualRightSolarWing != null)
            {
                visualRightSolarWing.gameObject.SetActive(solar != null);
                visualRightSolarWing.sizeDelta = new Vector2(wingWidth, wingHeight);
                visualRightSolarWing.anchoredPosition = new Vector2(visualSatelliteHull.sizeDelta.x * 0.5f + wingWidth * 0.5f + 4f, 0f);
                Image wImg = visualRightSolarWing.GetComponent<Image>();
                if (wImg != null) wImg.color = wingColor;
            }

            // 4. Update 2D Payload Core Visual inside the CubeSat
            if (visualPayloadLens != null && payload != null)
            {
                if (payload.Id.Contains("BIONUTRIENTS"))
                    visualPayloadLens.color = new Color(0.20f, 0.86f, 0.42f, 1f); // Bio-green incubator
                else if (payload.Id.Contains("LUNAR"))
                    visualPayloadLens.color = new Color(0.72f, 0.35f, 0.95f, 1f); // IR Spectrometer violet
                else
                    visualPayloadLens.color = new Color(0.12f, 0.76f, 0.96f, 1f); // Earth optical camera cyan

                if (visualPayloadLabel != null)
                {
                    string shortPay = payload.Id.Contains("BIONUTRIENTS") ? "BIO\nLAB"
                        : (payload.Id.Contains("LUNAR") ? "IR\nSPEC" : "OPTICAL\nCAM");
                    visualPayloadLabel.text = shortPay;
                }
            }

            // 5. Update 2D Top Comms Antenna Visual
            if (visualTopAntenna != null && comms != null)
            {
                Image antImg = visualTopAntenna.GetComponent<Image>();
                if (comms.Id.Contains("XBAND"))
                {
                    visualTopAntenna.sizeDelta = new Vector2(58f, 24f);
                    if (antImg != null) antImg.color = new Color(0.95f, 0.76f, 0.18f, 1f);
                }
                else if (comms.Id.Contains("SBAND"))
                {
                    visualTopAntenna.sizeDelta = new Vector2(42f, 14f);
                    if (antImg != null) antImg.color = new Color(0.92f, 0.95f, 0.98f, 1f);
                }
                else
                {
                    visualTopAntenna.sizeDelta = new Vector2(10f, 38f);
                    if (antImg != null) antImg.color = new Color(0.45f, 0.88f, 0.55f, 1f);
                }
                if (visualAntennaLabel != null)
                {
                    visualAntennaLabel.text = comms.Id.Replace("COMMS_", "");
                }
            }

            // 6. Update 2D Ground Station Dish Visual in Corner
            if (visualGroundDish != null && ground != null)
            {
                Image gdImg = visualGroundDish.GetComponent<Image>();
                if (ground.Id.Contains("DSN"))
                {
                    visualGroundDish.sizeDelta = new Vector2(115f, 52f);
                    if (gdImg != null) gdImg.color = new Color(0.95f, 0.78f, 0.22f, 0.9f);
                }
                else if (ground.Id.Contains("SBAND"))
                {
                    visualGroundDish.sizeDelta = new Vector2(95f, 44f);
                    if (gdImg != null) gdImg.color = new Color(0.25f, 0.72f, 0.95f, 0.9f);
                }
                else
                {
                    visualGroundDish.sizeDelta = new Vector2(75f, 36f);
                    if (gdImg != null) gdImg.color = new Color(0.35f, 0.80f, 0.50f, 0.9f);
                }

                if (visualGroundDishLabel != null)
                {
                    string gsShort = ground.Id.Contains("DSN") ? "34m NASA DSN"
                        : (ground.Id.Contains("SBAND") ? "3m S-Band Dish" : "Roof UHF Yagi");
                    visualGroundDishLabel.text = $"Ground:\n{gsShort}";
                }
            }

            // 7. Update the 9 Interactive Subsystem Slot Buttons
            ComponentPart[] orderedEquipped = new ComponentPart[]
            {
                CurrentDesign.SelectedChassis,
                CurrentDesign.SelectedLaunchVehicle,
                CurrentDesign.SelectedOrbit,
                CurrentDesign.SelectedPayload,
                CurrentDesign.SelectedSolarPanel,
                CurrentDesign.SelectedBattery,
                CurrentDesign.SelectedADCS,
                CurrentDesign.SelectedComms,
                CurrentDesign.SelectedGroundStation
            };

            string[] slotHeaders = new string[]
            {
                "1. CHASSIS",
                "2. ROCKET",
                "3. ORBIT",
                "4. PAYLOAD",
                "5. SOLAR",
                "6. BATTERY",
                "7. ADCS",
                "8. RADIO",
                "9. GROUND"
            };

            if (cubeSatSlotBoxes == null) return;

            for (int i = 0; i < cubeSatSlotBoxes.Length && i < orderedEquipped.Length; i++)
            {
                if (cubeSatSlotBoxes[i] == null) continue;
                cubeSatSlotBoxes[i].gameObject.SetActive(true);

                ComponentPart part = orderedEquipped[i];
                bool isFilteredSlot = (_activeCategoryFilter.HasValue && _activeCategoryFilter.Value == SlotOrder[i]);

                cubeSatSlotBoxes[i].color = isFilteredSlot
                    ? new Color(0.22f, 0.62f, 0.92f, 1f)
                    : new Color(0.13f, 0.22f, 0.34f, 0.95f);

                if (cubeSatSlotLabels != null && i < cubeSatSlotLabels.Length && cubeSatSlotLabels[i] != null)
                {
                    string partShort = part != null ? part.Name : "[Click to Select]";
                    cubeSatSlotLabels[i].text = $"<b>{slotHeaders[i]}</b>\n{partShort}";
                }
            }
        }

        private void UpdateVerificationButtonVisuals()
        {
            SetToggleVisual(toggleFccLicenseButton, "FCC RF License ($0k)", CurrentDesign.HasFccIaruRfLicense);
            SetToggleVisual(toggleNoaaLicenseButton, "NOAA Cam License ($0k)", CurrentDesign.HasNoaaRemoteSensingLicense);
            SetToggleVisual(toggleVibrationTestButton, "Vibration Test (+$8k)", CurrentDesign.CompletedVibrationTest);
            SetToggleVisual(toggleTvacTestButton, "TVAC Bakeout (+$6k)", CurrentDesign.CompletedTvacBakeoutTest);
            SetToggleVisual(toggleDitlTestButton, "DITL Timer Test (+$4k)", CurrentDesign.CompletedDitlSoftwareTest);
            SetToggleVisual(toggleDoubleBurnWireButton, "Double Burn-Wire (+$3k)", CurrentDesign.UsedDoubleBurnWire);
        }

        private void SetToggleVisual(Button btn, string label, bool isOn)
        {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isOn
                    ? new Color(0.14f, 0.55f, 0.34f, 1f)
                    : new Color(0.52f, 0.18f, 0.18f, 1f);
            }
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text = isOn ? $"[ON] {label}" : $"[OFF] {label}";
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
                    return "1. LAUNCH SEGMENT — Click 'EQUIP PART' on any card below to swap Chassis, Rocket, or Orbit!";
                case MissionSegment.Space:
                    return "2. SPACE SEGMENT — Click 'EQUIP PART' below to swap Payload, Solar Wings, Battery, ADCS, or Radio!";
                case MissionSegment.Ground:
                    return "3. GROUND SEGMENT — Click 'EQUIP PART' below to swap Ground Station Antenna Network!";
                case MissionSegment.User:
                    return "4. USER SEGMENT — Review Science Payload & Ground Data Archiving Traceability!";
                default:
                    return "SPACECRAFT BUILDER";
            }
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
