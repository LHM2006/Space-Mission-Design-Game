using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SpaceMissionGame.Core;
using SpaceMissionGame.Scenarios;
using SpaceMissionGame.UI;

namespace SpaceMissionGame.Managers
{
    /// <summary>
    /// [Member 3 - HanMing] Central Game Progression, Scenarios & Integration Controller (`GameManager.cs`).
    /// 
    /// Responsibilities:
    /// 1. Manages the 5-Phase NASA Concept Maturity Level (CML) State Machine (`GamePhase` enum).
    /// 2. Connects Member 3's `MissionScenariosData` and `ReviewEventManager` with Member 1's
    ///    `MissionCalculator` / `PartsDatabase` and Member 2's `CockpitUIAutoBuilder` / `BuilderUI` /
    ///    `OrbitVisualizer2D` / `ScorecardUI`.
    /// 3. Uses `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` so it
    ///    automatically spawns and wires itself into `MainGame.unity` at runtime with ZERO edits
    ///    needed to Member 2's scene or prefabs (preventing Git merge conflicts!).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("NASA CML Game Progression State")]
        [SerializeField] private GamePhase currentPhase = GamePhase.CML1_MissionSelect;
        [SerializeField] private string activeMissionId = "MISSION_2_BISONSAT";

        [Header("Connected Member 3 & Member 2 Controllers")]
        [SerializeField] private ReviewEventManager reviewEventManager;
        [SerializeField] private CockpitUIAutoBuilder cockpitUI;
        [SerializeField] private BuilderUI builderUI;
        [SerializeField] private ResourceBarsUI resourceBarsUI;
        [SerializeField] private OrbitVisualizer2D orbitVisualizer2D;
        [SerializeField] private ScorecardUI scorecardUI;

        private string _lastAppliedMissionId = string.Empty;
        private bool _isSyncingMissionProfile = false;

        public GamePhase CurrentPhase => currentPhase;
        public ReviewEventManager ReviewManager => reviewEventManager;

        /// <summary>
        /// Automatically spawns `GameManager` + `ReviewEventManager` when `MainGame.unity` starts,
        /// so Member 3 never has to modify Member 2's `.unity` scene file!
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapGameManager()
        {
            if (FindFirstObjectByType<GameManager>() != null) return;

            CockpitUIAutoBuilder uiRoot = FindFirstObjectByType<CockpitUIAutoBuilder>();
            if (uiRoot == null && FindFirstObjectByType<BuilderUI>() == null) return;

            GameObject gmObj = new GameObject("NASA_GameManager_Member3");
            gmObj.AddComponent<ReviewEventManager>();
            gmObj.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (reviewEventManager == null)
            {
                reviewEventManager = GetComponent<ReviewEventManager>();
                if (reviewEventManager == null)
                {
                    reviewEventManager = gameObject.AddComponent<ReviewEventManager>();
                }
            }
        }

        private void Start()
        {
            DiscoverAndWireSceneControllers();
        }

        /// <summary>
        /// Finds Member 2's UI controllers in `MainGame.unity` and wires them into the 5-Phase NASA CML loop.
        /// </summary>
        public void DiscoverAndWireSceneControllers()
        {
            if (cockpitUI == null) cockpitUI = FindFirstObjectByType<CockpitUIAutoBuilder>();
            if (builderUI == null) builderUI = cockpitUI != null ? cockpitUI.builderUI : FindFirstObjectByType<BuilderUI>();
            if (resourceBarsUI == null) resourceBarsUI = cockpitUI != null ? cockpitUI.resourceBarsUI : FindFirstObjectByType<ResourceBarsUI>();
            if (orbitVisualizer2D == null) orbitVisualizer2D = cockpitUI != null ? cockpitUI.orbitVisualizer2D : FindFirstObjectByType<OrbitVisualizer2D>();
            if (scorecardUI == null) scorecardUI = cockpitUI != null ? cockpitUI.scorecardUI : FindFirstObjectByType<ScorecardUI>();

            if (builderUI != null)
            {
                builderUI.OnDesignUpdated -= HandleDesignUpdated;
                builderUI.OnDesignUpdated += HandleDesignUpdated;

                // Intercept the "LAUNCH & SIMULATE 2D ORBIT" button on BuilderPanel so it opens
                // Phase 3 (`CML4_ReviewAndTesting`) Engineering Dilemma Cards first!
                WireProceedToReviewButton();

                // Sync initial design with Member 3's rich NASA `MissionScenariosData`
                if (builderUI.CurrentDesign != null)
                {
                    UpgradeDesignWithRichMissionProfile(builderUI.CurrentDesign);
                    builderUI.RecalculateAndRefreshUI();
                }
            }

            if (cockpitUI != null)
            {
                WireTopScreenSwitcherButtons();
            }

            if (orbitVisualizer2D != null)
            {
                orbitVisualizer2D.OnBackToBuilderRequested += () => TransitionToPhase(GamePhase.CML2_3_SpacecraftBuilder);
                orbitVisualizer2D.OnProceedToScorecardRequested += () => TransitionToPhase(GamePhase.PostMission_Scorecard);
            }

            if (scorecardUI != null)
            {
                scorecardUI.OnRedesignSpacecraftRequested += () => TransitionToPhase(GamePhase.CML2_3_SpacecraftBuilder);
                scorecardUI.OnReturnToMissionSelectRequested += () => TransitionToPhase(GamePhase.CML1_MissionSelect);
            }

            currentPhase = GamePhase.CML2_3_SpacecraftBuilder;
            Debug.Log("[Member 3 GameManager] Connected MissionScenariosData, ReviewEventManager, MissionCalculator, and UI!");
        }

        private void WireProceedToReviewButton()
        {
            GameObject proceedBtnObj = GameObject.Find("ProceedToSimulationButton");
            if (proceedBtnObj != null)
            {
                Button proceedBtn = proceedBtnObj.GetComponent<Button>();
                if (proceedBtn != null)
                {
                    proceedBtn.onClick.RemoveAllListeners();
                    proceedBtn.onClick.AddListener(StartPhase3PreLaunchReview);
                }
            }
        }

        private void WireTopScreenSwitcherButtons()
        {
            if (cockpitUI.screenBuilderButton != null)
            {
                cockpitUI.screenBuilderButton.onClick.RemoveAllListeners();
                cockpitUI.screenBuilderButton.onClick.AddListener(() => TransitionToPhase(GamePhase.CML2_3_SpacecraftBuilder));
            }

            if (cockpitUI.screenSimulationButton != null)
            {
                cockpitUI.screenSimulationButton.onClick.RemoveAllListeners();
                cockpitUI.screenSimulationButton.onClick.AddListener(() => TransitionToPhase(GamePhase.PhaseCD_OrbitSimulation));
            }

            if (cockpitUI.screenScorecardButton != null)
            {
                cockpitUI.screenScorecardButton.onClick.RemoveAllListeners();
                cockpitUI.screenScorecardButton.onClick.AddListener(() => TransitionToPhase(GamePhase.PostMission_Scorecard));
            }
        }

        /// <summary>
        /// Triggered whenever the player selects a Mission Preset (1, 2, 3), swaps a part, or toggles a test in `BuilderUI`.
        /// Upgrades `design.SelectedMission` with the full NASA `MissionProfile` from `MissionScenariosData.cs`.
        /// </summary>
        private void HandleDesignUpdated(SpacecraftDesign design, MissionEvaluationResult eval)
        {
            if (_isSyncingMissionProfile || design == null || design.SelectedMission == null) return;

            string currentId = design.SelectedMission.MissionId;
            bool switchedMission = !string.Equals(currentId, _lastAppliedMissionId, System.StringComparison.OrdinalIgnoreCase);

            if (switchedMission || string.IsNullOrEmpty(design.SelectedMission.NasaReferenceSource))
            {
                _isSyncingMissionProfile = true;
                UpgradeDesignWithRichMissionProfile(design);

                if (switchedMission && reviewEventManager != null)
                {
                    reviewEventManager.ResetDilemmaState(design);
                }

                _lastAppliedMissionId = design.SelectedMission.MissionId;
                activeMissionId = _lastAppliedMissionId;

                MissionEvaluationResult updatedEval = MissionCalculator.EvaluateMission(design);
                if (reviewEventManager != null)
                {
                    reviewEventManager.EnrichEvaluationWithScenarioLogs(design, updatedEval);
                }

                if (resourceBarsUI != null)
                {
                    resourceBarsUI.RefreshDashboard(updatedEval);
                }
                _isSyncingMissionProfile = false;
            }
            else if (reviewEventManager != null && eval != null)
            {
                reviewEventManager.SyncTotalExtraCosts(design);
                reviewEventManager.EnrichEvaluationWithScenarioLogs(design, eval);
            }
        }

        private void UpgradeDesignWithRichMissionProfile(SpacecraftDesign design)
        {
            if (design == null || design.SelectedMission == null) return;

            MissionProfile richProfile = MissionScenariosData.GetMissionById(design.SelectedMission.MissionId);
            if (richProfile != null)
            {
                design.SelectedMission = richProfile;
            }
        }

        /// <summary>
        /// Phase 3 (`CML4_ReviewAndTesting`): Triggered when the player clicks "LAUNCH & SIMULATE 2D ORBIT".
        /// Opens the interactive NASA CubeSat 101 Pre-Launch Dilemma Cards modal before launching into orbit!
        /// </summary>
        public void StartPhase3PreLaunchReview()
        {
            if (builderUI == null || builderUI.CurrentDesign == null)
            {
                TransitionToPhase(GamePhase.PhaseCD_OrbitSimulation);
                return;
            }

            currentPhase = GamePhase.CML4_ReviewAndTesting;
            SpacecraftDesign design = builderUI.CurrentDesign;
            UpgradeDesignWithRichMissionProfile(design);

            if (reviewEventManager != null)
            {
                reviewEventManager.StartPreLaunchReviewSequence(design, () =>
                {
                    // Refresh Builder & Resource Bars with the player's Dilemma Card modifiers
                    builderUI.RecalculateAndRefreshUI();
                    if (reviewEventManager != null && builderUI.LastEvaluation != null)
                    {
                        reviewEventManager.EnrichEvaluationWithScenarioLogs(design, builderUI.LastEvaluation);
                    }

                    // Advance to Phase 4: 2D Orbit & Launch Simulation!
                    TransitionToPhase(GamePhase.PhaseCD_OrbitSimulation);
                });
            }
            else
            {
                TransitionToPhase(GamePhase.PhaseCD_OrbitSimulation);
            }
        }

        /// <summary>
        /// Transitions cleanly between the 5 NASA CML Game Phases and updates Member 2's screens.
        /// </summary>
        public void TransitionToPhase(GamePhase targetPhase)
        {
            currentPhase = targetPhase;

            if (reviewEventManager != null && targetPhase != GamePhase.CML4_ReviewAndTesting)
            {
                reviewEventManager.HideDilemmaModal();
            }

            if (cockpitUI == null) return;

            switch (targetPhase)
            {
                case GamePhase.MainMenu:
                case GamePhase.CML1_MissionSelect:
                case GamePhase.CML2_3_SpacecraftBuilder:
                    cockpitUI.SwitchMainScreen("BUILDER");
                    break;

                case GamePhase.CML4_ReviewAndTesting:
                    cockpitUI.SwitchMainScreen("BUILDER");
                    StartPhase3PreLaunchReview();
                    break;

                case GamePhase.PhaseCD_OrbitSimulation:
                    if (builderUI != null && builderUI.CurrentDesign != null)
                    {
                        builderUI.RecalculateAndRefreshUI();
                        if (reviewEventManager != null && builderUI.LastEvaluation != null)
                        {
                            reviewEventManager.EnrichEvaluationWithScenarioLogs(builderUI.CurrentDesign, builderUI.LastEvaluation);
                        }
                    }
                    cockpitUI.SwitchMainScreen("SIMULATION");
                    break;

                case GamePhase.PostMission_Scorecard:
                    if (builderUI != null && builderUI.CurrentDesign != null)
                    {
                        builderUI.RecalculateAndRefreshUI();
                        if (reviewEventManager != null && builderUI.LastEvaluation != null)
                        {
                            reviewEventManager.EnrichEvaluationWithScenarioLogs(builderUI.CurrentDesign, builderUI.LastEvaluation);
                        }
                    }
                    cockpitUI.SwitchMainScreen("SCORECARD");
                    break;
            }
        }
    }
}
