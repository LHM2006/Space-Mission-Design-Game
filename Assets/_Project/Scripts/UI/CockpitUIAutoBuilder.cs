using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using SpaceMissionGame.Core;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace SpaceMissionGame.UI
{
    /// <summary>
    /// [Member 2 - UI] Permanent 2D UI Scene & Prefab Builder (`CockpitUIAutoBuilder.cs`).
    /// 
    /// v4 Responsive Layout Fix:
    /// - Sets `CanvasScaler.matchWidthOrHeight = 1.0f` so wide `Free Aspect` Game windows never clip the bottom!
    /// - Anchors all bottom navigation buttons (`ProceedToSimulationButton`, `BackToBuilderButton`,
    ///   `ViewScorecardButton`, `RedesignInBuilderButton`) directly to the bottom edge (`anchorMin.y = 0`)
    ///   with a 16px bottom margin so they are 100% visible on every screen ratio!
    /// </summary>
    public class CockpitUIAutoBuilder : MonoBehaviour
    {
        private const string VersionMarkerObjectName = "UI_Version_v4_Responsive";

        [Header("Main Phase Panels (Persistent in Scene)")]
        public GameObject builderPhaseRoot;
        public GameObject simulationPhaseRoot;
        public GameObject scorecardPhaseRoot;

        [Header("Top Screen Switcher Buttons")]
        public Button screenBuilderButton;
        public Button screenSimulationButton;
        public Button screenScorecardButton;

        [Header("Core Member 2 Controllers")]
        public BuilderUI builderUI;
        public ResourceBarsUI resourceBarsUI;
        public OrbitVisualizer2D orbitVisualizer2D;
        public ScorecardUI scorecardUI;

        private void Awake()
        {
            if (screenBuilderButton != null)
            {
                screenBuilderButton.onClick.RemoveAllListeners();
                screenBuilderButton.onClick.AddListener(() => SwitchMainScreen("BUILDER"));
            }

            if (screenSimulationButton != null)
            {
                screenSimulationButton.onClick.RemoveAllListeners();
                screenSimulationButton.onClick.AddListener(() => SwitchMainScreen("SIMULATION"));
            }

            if (screenScorecardButton != null)
            {
                screenScorecardButton.onClick.RemoveAllListeners();
                screenScorecardButton.onClick.AddListener(() => SwitchMainScreen("SCORECARD"));
            }
        }

        private void Start()
        {
            if (builderUI != null)
            {
                builderUI.OnProceedToCML4ReviewRequested += () => SwitchMainScreen("SIMULATION");
            }

            if (orbitVisualizer2D != null)
            {
                orbitVisualizer2D.OnBackToBuilderRequested += () => SwitchMainScreen("BUILDER");
                orbitVisualizer2D.OnProceedToScorecardRequested += () => SwitchMainScreen("SCORECARD");
            }

            if (scorecardUI != null)
            {
                scorecardUI.OnRedesignSpacecraftRequested += () => SwitchMainScreen("BUILDER");
                scorecardUI.OnReturnToMissionSelectRequested += () => SwitchMainScreen("BUILDER");
            }

            SwitchMainScreen("BUILDER");
        }

        public void SwitchMainScreen(string phaseKey)
        {
            ShowPhasePanel(phaseKey);

            if (phaseKey == "SIMULATION" && orbitVisualizer2D != null && builderUI != null)
            {
                orbitVisualizer2D.StartSimulation(builderUI.CurrentDesign, builderUI.LastEvaluation);
            }
            else if (phaseKey == "SCORECARD" && scorecardUI != null && builderUI != null)
            {
                scorecardUI.ShowScorecard(builderUI.CurrentDesign, builderUI.LastEvaluation);
            }

            UpdateTopScreenButtons(phaseKey);
        }

        public void ShowPhasePanel(string phaseKey)
        {
            if (builderPhaseRoot != null) builderPhaseRoot.SetActive(phaseKey == "BUILDER");
            if (simulationPhaseRoot != null) simulationPhaseRoot.SetActive(phaseKey == "SIMULATION");
            if (scorecardPhaseRoot != null) scorecardPhaseRoot.SetActive(phaseKey == "SCORECARD");
        }

        private void UpdateTopScreenButtons(string activePhase)
        {
            Color activeCol = new Color(0.18f, 0.65f, 0.42f, 1f);
            Color idleCol = new Color(0.14f, 0.22f, 0.34f, 1f);

            if (screenBuilderButton != null && screenBuilderButton.GetComponent<Image>() != null)
                screenBuilderButton.GetComponent<Image>().color = activePhase == "BUILDER" ? activeCol : idleCol;

            if (screenSimulationButton != null && screenSimulationButton.GetComponent<Image>() != null)
                screenSimulationButton.GetComponent<Image>().color = activePhase == "SIMULATION" ? activeCol : idleCol;

            if (screenScorecardButton != null && screenScorecardButton.GetComponent<Image>() != null)
                screenScorecardButton.GetComponent<Image>().color = activePhase == "SCORECARD" ? activeCol : idleCol;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void AutoUpgradeOutdatedSceneUIOnCompile()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                GameObject canvas = GameObject.Find("NASA_MissionCockpit_Canvas");
                GameObject versionMarker = GameObject.Find(VersionMarkerObjectName);

                if (canvas != null && versionMarker == null)
                {
                    Debug.Log("[Member 2 UI Auto-Upgrader] Upgrading scene to v4 Responsive Bottom-Anchored Layout...");
                    BuildAndSavePermanent2DUI();
                }
            };
        }

        [MenuItem("Tools/Space Mission Game/Build & Save Permanent 2D UI (Scene + Prefabs)")]
        public static void BuildAndSavePermanent2DUI()
        {
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                Camera cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.07f, 0.12f, 1f);
            }

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();

                Type newInputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (newInputModuleType != null)
                {
                    esObj.AddComponent(newInputModuleType);
                }
                else
                {
                    esObj.AddComponent<StandaloneInputModule>();
                }
            }

            GameObject existingCanvas = GameObject.Find("NASA_MissionCockpit_Canvas");
            if (existingCanvas != null)
            {
                DestroyImmediate(existingCanvas);
            }

            // Create Permanent Root 2D Canvas (1920 x 1080, Match Height = 1.0 so bottom is NEVER cut off in Free Aspect!)
            GameObject canvasObj = new GameObject("NASA_MissionCockpit_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1.0f;
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject versionMarker = new GameObject(VersionMarkerObjectName);
            versionMarker.transform.SetParent(canvasObj.transform, false);

            CockpitUIAutoBuilder rootController = canvasObj.AddComponent<CockpitUIAutoBuilder>();

            RectTransform bgRect = CreatePanel(canvasObj.transform, "CockpitBackground", new Color(0.05f, 0.07f, 0.12f, 1f));
            StretchFull(bgRect);

            // Top Screen Switcher Bar
            RectTransform topNavBar = CreatePanel(canvasObj.transform, "TopScreenSwitcherBar", new Color(0.03f, 0.05f, 0.09f, 0.98f));
            SetStretchBox(topNavBar, new Vector2(0f, 1f), new Vector2(0.73f, 1f), new Vector2(10f, -44f), new Vector2(-6f, -8f));

            Button s1Btn = CreateButton(topNavBar, "Screen1_BuilderBtn", "[SCREEN 1] SPACECRAFT & SEGMENT BUILDER (CML 1-3)", new Color(0.18f, 0.65f, 0.42f, 1f));
            SetHorizontalSlice(s1Btn.GetComponent<RectTransform>(), 0f, 0.36f, -2f, 32f);
            rootController.screenBuilderButton = s1Btn;

            Button s2Btn = CreateButton(topNavBar, "Screen2_OrbitSimBtn", "[SCREEN 2] 2D ORBIT & LAUNCH VISUALIZER (CML 4)", new Color(0.14f, 0.22f, 0.34f, 1f));
            SetHorizontalSlice(s2Btn.GetComponent<RectTransform>(), 0.365f, 0.70f, -2f, 32f);
            rootController.screenSimulationButton = s2Btn;

            Button s3Btn = CreateButton(topNavBar, "Screen3_ScorecardBtn", "[SCREEN 3] NASA CML SCORECARD (DEBRIEF)", new Color(0.14f, 0.22f, 0.34f, 1f));
            SetHorizontalSlice(s3Btn.GetComponent<RectTransform>(), 0.705f, 1f, -2f, 32f);
            rootController.screenScorecardButton = s3Btn;

            // Right Sidebar: Live Resource Bars Panel (`ResourceBarsUI`) - 27% width on right
            ResourceBarsUI resourceBars = BuildResourceBarsPanel(canvasObj.transform);
            rootController.resourceBarsUI = resourceBars;

            // Main Left/Center Area (73% width, full height below TopScreenSwitcherBar with 12px bottom margin)
            BuilderUI builder = BuildSpacecraftBuilderPanel(canvasObj.transform, resourceBars, out GameObject builderRoot);
            rootController.builderPhaseRoot = builderRoot;
            rootController.builderUI = builder;

            OrbitVisualizer2D orbitVis = BuildOrbitSimulationPanel(canvasObj.transform, out GameObject simRoot);
            rootController.simulationPhaseRoot = simRoot;
            rootController.orbitVisualizer2D = orbitVis;

            ScorecardUI scorecard = BuildScorecardPanel(canvasObj.transform, out GameObject scorecardRoot);
            rootController.scorecardPhaseRoot = scorecardRoot;
            rootController.scorecardUI = scorecard;

            builderRoot.SetActive(true);
            simRoot.SetActive(false);
            scorecardRoot.SetActive(false);

            // Save Permanent Prefabs into `Assets/_Project/Prefabs/`
            EnsureFolderExists("Assets/_Project");
            EnsureFolderExists("Assets/_Project/Prefabs");
            EnsureFolderExists("Assets/_Project/Scenes");

            PrefabUtility.SaveAsPrefabAssetAndConnect(
                resourceBars.gameObject,
                "Assets/_Project/Prefabs/ResourceBarsPanel.prefab",
                InteractionMode.AutomatedAction);

            PrefabUtility.SaveAsPrefabAssetAndConnect(
                builderRoot,
                "Assets/_Project/Prefabs/BuilderPanel.prefab",
                InteractionMode.AutomatedAction);

            PrefabUtility.SaveAsPrefabAssetAndConnect(
                simRoot,
                "Assets/_Project/Prefabs/SimulationPanel.prefab",
                InteractionMode.AutomatedAction);

            PrefabUtility.SaveAsPrefabAssetAndConnect(
                scorecardRoot,
                "Assets/_Project/Prefabs/ScorecardPanel.prefab",
                InteractionMode.AutomatedAction);

            EditorUtility.SetDirty(canvasObj);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/_Project/Scenes/MainGame.unity");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = canvasObj;
            Debug.Log("[Member 2 UI Builder] v4 Responsive Bottom-Anchored 2D UI saved to MainGame.unity & Prefabs!");
        }

        private static ResourceBarsUI BuildResourceBarsPanel(Transform parent)
        {
            RectTransform panelRect = CreatePanel(parent, "ResourceBarsPanel", new Color(0.08f, 0.11f, 0.17f, 0.98f));
            SetStretchBox(panelRect, new Vector2(0.735f, 0f), new Vector2(1f, 1f), new Vector2(4f, 10f), new Vector2(-10f, -8f));

            ResourceBarsUI rb = panelRect.gameObject.AddComponent<ResourceBarsUI>();
            SerializedObject so = new SerializedObject(rb);

            TMP_Text header = CreateText(panelRect, "HeaderTitle", "LIVE ENGINEERING RESOURCES (CML 4)", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRectTop(header.rectTransform, 10f, 28f);

            TMP_Text readiness = CreateText(panelRect, "OverallReadinessText", "CML 4 RYG Checklist: 100% GREEN - GO FOR LAUNCH", 14, FontStyles.Bold, TextAlignmentOptions.Center);
            readiness.color = new Color(0.18f, 0.80f, 0.44f);
            SetRectTop(readiness.rectTransform, 42f, 26f);
            so.FindProperty("overallReadinessText").objectReferenceValue = readiness;

            string[] meterNames = { "massMeter", "powerMeter", "budgetMeter", "dataMeter", "scienceMeter", "marginRiskMeter" };
            string[] defaultTitles = { "Mass", "Power Load", "Budget", "Telecom Link", "Science Return", "Reliability" };

            for (int i = 0; i < meterNames.Length; i++)
            {
                ResourceMeterItemUI meter = CreateSingleMeter(panelRect, defaultTitles[i], 76f + i * 102f);
                so.FindProperty(meterNames[i]).objectReferenceValue = meter;
            }

            // Diagnostic box stretches cleanly down to 12px above the bottom edge
            RectTransform diagBox = CreatePanel(panelRect, "DiagnosticBannerBox", new Color(0.04f, 0.06f, 0.10f, 0.96f));
            SetStretchBox(diagBox, Vector2.zero, Vector2.one, new Vector2(14f, 12f), new Vector2(-14f, -696f));
            TMP_Text diagText = CreateText(diagBox, "TradeOffDiagnosticText", "[ALL SYSTEMS NOMINAL] Click 'EQUIP PART' on any card on the left or click any of the 9 spacecraft slots to swap parts!", 13, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            StretchFull(diagText.rectTransform, 12f);
            so.FindProperty("tradeOffDiagnosticText").objectReferenceValue = diagText;

            so.ApplyModifiedPropertiesWithoutUndo();
            return rb;
        }

        private static ResourceMeterItemUI CreateSingleMeter(Transform parent, string label, float topOffset)
        {
            RectTransform itemRect = CreatePanel(parent, $"Meter_{label}", new Color(0.12f, 0.16f, 0.23f, 0.92f));
            SetRectTop(itemRect, topOffset, 92f);

            ResourceMeterItemUI item = itemRect.gameObject.AddComponent<ResourceMeterItemUI>();
            SerializedObject so = new SerializedObject(item);

            TMP_Text titleTxt = CreateText(itemRect, "TitleText", label, 15, FontStyles.Bold, TextAlignmentOptions.Left);
            SetAnchoredBox(titleTxt.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(12f, -18f), new Vector2(-16f, 24f));

            TMP_Text badgeTxt = CreateText(itemRect, "StatusBadgeText", "GREEN", 14, FontStyles.Bold, TextAlignmentOptions.Right);
            badgeTxt.color = new Color(0.18f, 0.80f, 0.44f);
            SetAnchoredBox(badgeTxt.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -18f), new Vector2(-16f, 24f));

            TMP_Text valTxt = CreateText(itemRect, "ValueReadoutText", "0.0 / 0.0", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            SetAnchoredBox(valTxt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -44f), new Vector2(-24f, 22f));

            RectTransform barBg = CreatePanel(itemRect, "BarBackground", new Color(0.04f, 0.06f, 0.09f, 1f));
            SetAnchoredBox(barBg, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 16f), new Vector2(-24f, 16f));

            RectTransform fillRect = CreatePanel(barBg, "FillBar", new Color(0.18f, 0.80f, 0.44f, 1f));
            StretchFull(fillRect);
            Image fillImg = fillRect.GetComponent<Image>();
            fillImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0.65f;

            so.FindProperty("metricTitleText").objectReferenceValue = titleTxt;
            so.FindProperty("valueReadoutText").objectReferenceValue = valTxt;
            so.FindProperty("statusBadgeText").objectReferenceValue = badgeTxt;
            so.FindProperty("fillBarImage").objectReferenceValue = fillImg;
            so.ApplyModifiedPropertiesWithoutUndo();

            return item;
        }

        private static BuilderUI BuildSpacecraftBuilderPanel(Transform parent, ResourceBarsUI resourceBars, out GameObject rootObj)
        {
            RectTransform panelRect = CreatePanel(parent, "BuilderPanel", new Color(0.06f, 0.09f, 0.14f, 0.98f));
            SetStretchBox(panelRect, new Vector2(0f, 0f), new Vector2(0.73f, 1f), new Vector2(10f, 10f), new Vector2(-6f, -50f));
            rootObj = panelRect.gameObject;

            BuilderUI builder = rootObj.AddComponent<BuilderUI>();
            SerializedObject so = new SerializedObject(builder);

            // Row 1: 3 Mission Scenario Preset Buttons (CML 1)
            string[] missionLabels =
            {
                "MISSION 1 (EASY): BioNutrients-2 ISS (1U/3U)",
                "MISSION 2 (MED): BisonSat Forest Imager (3U SSO)",
                "MISSION 3 (HARD): Artemis Lunar Ice (6U Deep Space)"
            };
            string[] missionProps = { "mission1EasyButton", "mission2MediumButton", "mission3HardButton" };

            for (int i = 0; i < 3; i++)
            {
                Button mBtn = CreateButton(panelRect, $"MissionPresetBtn_{i + 1}", missionLabels[i], new Color(0.14f, 0.22f, 0.34f, 1f));
                SetHorizontalSlice(mBtn.GetComponent<RectTransform>(), i / 3f, (i + 1) / 3f, -6f, 32f);
                so.FindProperty(missionProps[i]).objectReferenceValue = mBtn;
            }

            // Row 2: 4 NASA Mission Segment Tabs
            string[] tabLabels = { "1. LAUNCH SEGMENT", "2. SPACE SEGMENT", "3. GROUND SEGMENT", "4. USER SEGMENT" };
            string[] tabProps = { "launchSegmentTabButton", "spaceSegmentTabButton", "groundSegmentTabButton", "userSegmentTabButton" };

            for (int i = 0; i < 4; i++)
            {
                Button btn = CreateButton(panelRect, $"TabBtn_{i}", tabLabels[i], new Color(0.15f, 0.34f, 0.56f, 1f));
                SetHorizontalSlice(btn.GetComponent<RectTransform>(), i * 0.25f, (i + 1) * 0.25f, -42f, 36f);
                so.FindProperty(tabProps[i]).objectReferenceValue = btn;
            }

            TMP_Text segHeader = CreateText(panelRect, "ActiveSegmentHeaderText", "1. LAUNCH SEGMENT - Click 'EQUIP PART' on any card on the left or click any slot on the right!", 14, FontStyles.Bold, TextAlignmentOptions.Left);
            segHeader.color = new Color(0.55f, 0.85f, 1f);
            SetRectTop(segHeader.rectTransform, 82f, 24f);
            so.FindProperty("activeSegmentHeaderText").objectReferenceValue = segHeader;

            // Left Column: Scrollable Part Cards List
            RectTransform scrollViewport = CreatePanel(panelRect, "PartCardScrollViewport", new Color(0.04f, 0.06f, 0.10f, 0.98f));
            SetStretchBox(scrollViewport, new Vector2(0f, 0f), new Vector2(0.51f, 1f), new Vector2(10f, 10f), new Vector2(-6f, -110f));
            scrollViewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scrollRect = scrollViewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 28f;

            RectTransform contentRect = new GameObject("PartCardListContainer", typeof(RectTransform)).GetComponent<RectTransform>();
            contentRect.SetParent(scrollViewport, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 800f);

            VerticalLayoutGroup vlg = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = contentRect.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.viewport = scrollViewport;
            so.FindProperty("partCardListContainer").objectReferenceValue = contentRect;

            PartCardUI cardTemplate = BuildPartCardTemplate(panelRect);
            cardTemplate.gameObject.SetActive(false);
            so.FindProperty("partCardPrefab").objectReferenceValue = cardTemplate;

            // Right-Center Column: Interactive 2D Visual Stage + 9 Clickable Slots + 6 Pre-Launch Checkboxes + Bottom Launch Button
            RectTransform blueprintBox = CreatePanel(panelRect, "CubeSat2DBlueprintBox", new Color(0.08f, 0.12f, 0.19f, 0.96f));
            SetStretchBox(blueprintBox, new Vector2(0.515f, 0f), new Vector2(1f, 1f), new Vector2(6f, 10f), new Vector2(-10f, -110f));

            TMP_Text chassisTitle = CreateText(blueprintBox, "ChassisTitleText", "Mission 2: BisonSat / Earth Forest Imager (3U SSO)\nTarget: ORBIT_SSO | Min Science: 95 pts | Grant Cap: $450k", 13, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRectTop(chassisTitle.rectTransform, 6f, 36f);
            so.FindProperty("chassisTitleText").objectReferenceValue = chassisTitle;

            // Interactive 2D Visual Stage
            RectTransform satPreviewStage = CreatePanel(blueprintBox, "Satellite2DVisualStage", new Color(0.03f, 0.05f, 0.09f, 1f));
            SetRectTop(satPreviewStage, 46f, 198f);

            TMP_Text stageModeTxt = CreateText(satPreviewStage, "VisualStageModeText", "2D LIVE PREVIEW - LAUNCH SEGMENT VIEW", 11, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            stageModeTxt.color = new Color(0.50f, 0.84f, 1f);
            SetAnchoredBox(stageModeTxt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -12f), new Vector2(-20f, 20f));
            so.FindProperty("visualStageModeText").objectReferenceValue = stageModeTxt;

            RectTransform planetBackdrop = CreatePanel(satPreviewStage, "PlanetBackdrop2D", new Color(0.12f, 0.55f, 0.72f, 0.40f));
            planetBackdrop.anchoredPosition = new Vector2(-205f, -12f);
            planetBackdrop.sizeDelta = new Vector2(136f, 136f);
            TMP_Text planetLbl = CreateText(planetBackdrop, "PlanetLabel", "Sun-Synchronous Polar\nLauncher: SSO Rocket", 10, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchFull(planetLbl.rectTransform, 6f);
            so.FindProperty("visualPlanetBackdrop").objectReferenceValue = planetBackdrop;
            so.FindProperty("visualPlanetLabel").objectReferenceValue = planetLbl;

            RectTransform leftWing = CreatePanel(satPreviewStage, "LeftSolarWing2D", new Color(0.14f, 0.48f, 0.90f, 1f));
            leftWing.anchoredPosition = new Vector2(-104f, -6f);
            leftWing.sizeDelta = new Vector2(100f, 36f);
            so.FindProperty("visualLeftSolarWing").objectReferenceValue = leftWing;

            RectTransform rightWing = CreatePanel(satPreviewStage, "RightSolarWing2D", new Color(0.14f, 0.48f, 0.90f, 1f));
            rightWing.anchoredPosition = new Vector2(104f, -6f);
            rightWing.sizeDelta = new Vector2(100f, 36f);
            so.FindProperty("visualRightSolarWing").objectReferenceValue = rightWing;

            RectTransform topAnt = CreatePanel(satPreviewStage, "TopAntenna2D", new Color(0.92f, 0.95f, 0.98f, 1f));
            topAnt.anchoredPosition = new Vector2(0f, 76f);
            topAnt.sizeDelta = new Vector2(42f, 14f);
            TMP_Text antLbl = CreateText(topAnt, "AntennaLabel", "SBAND", 9, FontStyles.Bold, TextAlignmentOptions.Center);
            antLbl.color = new Color(0.08f, 0.12f, 0.18f);
            StretchFull(antLbl.rectTransform);
            so.FindProperty("visualTopAntenna").objectReferenceValue = topAnt;
            so.FindProperty("visualAntennaLabel").objectReferenceValue = antLbl;

            RectTransform satHull = CreatePanel(satPreviewStage, "CubeSatHull2D", new Color(0.80f, 0.84f, 0.90f, 1f));
            satHull.anchoredPosition = new Vector2(0f, -6f);
            satHull.sizeDelta = new Vector2(92f, 138f);
            TMP_Text hullTxt = CreateText(satHull, "HullLabel", "3U BUS\n(ALUMINUM)", 10, FontStyles.Bold, TextAlignmentOptions.Top);
            hullTxt.color = new Color(0.08f, 0.12f, 0.18f);
            StretchFull(hullTxt.rectTransform, 5f);
            so.FindProperty("visualSatelliteHull").objectReferenceValue = satHull;
            so.FindProperty("visualHullSpecLabel").objectReferenceValue = hullTxt;

            RectTransform lensRect = CreatePanel(satHull, "PayloadCameraLens2D", new Color(0.12f, 0.76f, 0.96f, 1f));
            lensRect.anchoredPosition = new Vector2(0f, -22f);
            lensRect.sizeDelta = new Vector2(54f, 48f);
            TMP_Text lensLbl = CreateText(lensRect, "PayloadLabel", "OPTICAL\nCAM", 9, FontStyles.Bold, TextAlignmentOptions.Center);
            lensLbl.color = new Color(0.05f, 0.08f, 0.12f);
            StretchFull(lensLbl.rectTransform, 2f);
            so.FindProperty("visualPayloadLens").objectReferenceValue = lensRect.GetComponent<Image>();
            so.FindProperty("visualPayloadLabel").objectReferenceValue = lensLbl;

            RectTransform groundDish = CreatePanel(satPreviewStage, "GroundDish2D", new Color(0.25f, 0.72f, 0.95f, 0.9f));
            groundDish.anchoredPosition = new Vector2(215f, -58f);
            groundDish.sizeDelta = new Vector2(92f, 42f);
            TMP_Text gdLbl = CreateText(groundDish, "GroundDishLabel", "Ground:\n3m S-Band Dish", 9, FontStyles.Bold, TextAlignmentOptions.Center);
            gdLbl.color = new Color(0.05f, 0.08f, 0.12f);
            StretchFull(gdLbl.rectTransform, 2f);
            so.FindProperty("visualGroundDish").objectReferenceValue = groundDish;
            so.FindProperty("visualGroundDishLabel").objectReferenceValue = gdLbl;

            // 9 Clickable Subsystem Loadout Slots (3x3 Grid)
            SerializedProperty slotsProp = so.FindProperty("cubeSatSlotBoxes");
            SerializedProperty labelsProp = so.FindProperty("cubeSatSlotLabels");
            SerializedProperty btnsProp = so.FindProperty("cubeSatSlotButtons");
            slotsProp.arraySize = 9;
            labelsProp.arraySize = 9;
            btnsProp.arraySize = 9;

            string[] defaultSlotNames =
            {
                "<b>1. CHASSIS</b>\n3U P-POD Frame",
                "<b>2. ROCKET</b>\nSSO Rideshare",
                "<b>3. ORBIT</b>\n600km Polar SSO",
                "<b>4. PAYLOAD</b>\nBisonSat Camera",
                "<b>5. SOLAR</b>\n3U Deployable",
                "<b>6. BATTERY</b>\nUL 12Wh Pack",
                "<b>7. ADCS</b>\nMagnetorquers",
                "<b>8. RADIO</b>\nS-Band Patch",
                "<b>9. GROUND</b>\n3m S-Band Dish"
            };

            for (int i = 0; i < 9; i++)
            {
                int row = i / 3;
                int col = i % 3;

                RectTransform slotRect = CreatePanel(blueprintBox, $"LoadoutSlot_{i + 1}", new Color(0.13f, 0.22f, 0.34f, 0.95f));
                SetHorizontalSlice(slotRect, col / 3f, (col + 1) / 3f, -(252f + row * 72f), 66f);

                Button slotBtn = slotRect.gameObject.AddComponent<Button>();
                TMP_Text slotLbl = CreateText(slotRect, "SlotLabel", defaultSlotNames[i], 11, FontStyles.Normal, TextAlignmentOptions.Center);
                StretchFull(slotLbl.rectTransform, 4f);

                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotRect.GetComponent<Image>();
                labelsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotLbl;
                btnsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotBtn;
            }

            // Phase 3 Pre-Launch Verification & Licensing Toggles (3x2 Grid)
            TMP_Text vnvHeader = CreateText(blueprintBox, "VnVHeader", "PHASE 3 PRE-LAUNCH LICENSING & TESTING (Click to Toggle ON/OFF):", 12, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRectTop(vnvHeader.rectTransform, 474f, 20f);

            string[] toggleProps =
            {
                "toggleFccLicenseButton",
                "toggleNoaaLicenseButton",
                "toggleVibrationTestButton",
                "toggleTvacTestButton",
                "toggleDitlTestButton",
                "toggleDoubleBurnWireButton"
            };
            string[] toggleDefaults =
            {
                "[ON] FCC RF License ($0k)",
                "[ON] NOAA Cam License ($0k)",
                "[ON] Vibration Test (+$8k)",
                "[ON] TVAC Bakeout (+$6k)",
                "[ON] DITL Timer Test (+$4k)",
                "[ON] Double Burn-Wire (+$3k)"
            };

            for (int i = 0; i < 6; i++)
            {
                int row = i / 3;
                int col = i % 3;

                Button tBtn = CreateButton(blueprintBox, $"ToggleBtn_{i}", toggleDefaults[i], new Color(0.14f, 0.55f, 0.34f, 1f));
                SetHorizontalSlice(tBtn.GetComponent<RectTransform>(), col / 3f, (col + 1) / 3f, -(498f + row * 40f), 36f);
                tBtn.GetComponentInChildren<TMP_Text>().fontSize = 11;
                so.FindProperty(toggleProps[i]).objectReferenceValue = tBtn;
            }

            // Bottom-Anchored Launch Button (Guaranteed visible at the bottom of CubeSat2DBlueprintBox!)
            Button proceedBtn = CreateButton(blueprintBox, "ProceedToSimulationButton", "LAUNCH & SIMULATE 2D ORBIT (PHASE 4) >>", new Color(0.18f, 0.68f, 0.38f, 1f));
            SetBottomSlice(proceedBtn.GetComponent<RectTransform>(), 0.02f, 0.98f, 14f, 48f);
            so.FindProperty("proceedToReviewButton").objectReferenceValue = proceedBtn;
            so.FindProperty("resourceBarsUI").objectReferenceValue = resourceBars;

            so.ApplyModifiedPropertiesWithoutUndo();

            List<ComponentPart> launchParts = PartsDatabase.GetPartsBySegment(MissionSegment.Launch);
            foreach (ComponentPart part in launchParts)
            {
                PartCardUI previewCard = Instantiate(cardTemplate, contentRect);
                previewCard.gameObject.SetActive(true);
                previewCard.SetupCard(part, part.Id == "CHASSIS_3U" || part.Id == "LAUNCH_RIDESHARE_LEO" || part.Id == "ORBIT_SSO", null);
            }

            return builder;
        }

        private static PartCardUI BuildPartCardTemplate(Transform parent)
        {
            RectTransform cardRect = CreatePanel(parent, "PartCardTemplate", new Color(0.13f, 0.17f, 0.24f, 0.98f));
            cardRect.sizeDelta = new Vector2(640f, 120f);
            LayoutElement le = cardRect.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 120f;
            le.minHeight = 120f;

            PartCardUI card = cardRect.gameObject.AddComponent<PartCardUI>();
            SerializedObject so = new SerializedObject(card);

            TMP_Text nameTxt = CreateText(cardRect, "PartNameText", "Component Name", 15, FontStyles.Bold, TextAlignmentOptions.Left);
            SetAnchoredBox(nameTxt.rectTransform, new Vector2(0f, 1f), new Vector2(0.76f, 1f), new Vector2(12f, -18f), new Vector2(-16f, 22f));

            TMP_Text badgeTxt = CreateText(cardRect, "CategoryBadgeText", "[SPACE - PAYLOAD]", 11, FontStyles.Bold, TextAlignmentOptions.Left);
            badgeTxt.color = new Color(0.45f, 0.78f, 0.98f);
            SetAnchoredBox(badgeTxt.rectTransform, new Vector2(0f, 1f), new Vector2(0.76f, 1f), new Vector2(12f, -38f), new Vector2(-16f, 18f));

            TMP_Text statsTxt = CreateText(cardRect, "StatsSummaryText", "Mass: 0.5kg | Power: -4W | Cost: $35k", 12, FontStyles.Normal, TextAlignmentOptions.Left);
            SetAnchoredBox(statsTxt.rectTransform, new Vector2(0f, 1f), new Vector2(0.78f, 1f), new Vector2(12f, -58f), new Vector2(-16f, 20f));

            TMP_Text descTxt = CreateText(cardRect, "TradeOffNoteText", "Trade-off description.", 11, FontStyles.Italic, TextAlignmentOptions.Left);
            descTxt.color = new Color(0.78f, 0.82f, 0.88f);
            SetAnchoredBox(descTxt.rectTransform, new Vector2(0f, 0f), new Vector2(0.78f, 0f), new Vector2(12f, 20f), new Vector2(-16f, 34f));

            Button selBtn = CreateButton(cardRect, "SelectPartButton", "EQUIP PART", new Color(0.20f, 0.48f, 0.78f, 1f));
            SetAnchoredBox(selBtn.GetComponent<RectTransform>(), new Vector2(0.79f, 0.22f), new Vector2(0.98f, 0.78f), Vector2.zero, Vector2.zero);

            so.FindProperty("partNameText").objectReferenceValue = nameTxt;
            so.FindProperty("categoryBadgeText").objectReferenceValue = badgeTxt;
            so.FindProperty("statsSummaryText").objectReferenceValue = statsTxt;
            so.FindProperty("tradeOffNoteText").objectReferenceValue = descTxt;
            so.FindProperty("selectButton").objectReferenceValue = selBtn;
            so.FindProperty("selectButtonLabel").objectReferenceValue = selBtn.GetComponentInChildren<TMP_Text>();
            so.FindProperty("cardBackgroundImage").objectReferenceValue = cardRect.GetComponent<Image>();
            so.ApplyModifiedPropertiesWithoutUndo();

            return card;
        }

        private static OrbitVisualizer2D BuildOrbitSimulationPanel(Transform parent, out GameObject rootObj)
        {
            RectTransform panelRect = CreatePanel(parent, "SimulationPanel", new Color(0.04f, 0.06f, 0.11f, 0.99f));
            SetStretchBox(panelRect, new Vector2(0f, 0f), new Vector2(0.73f, 1f), new Vector2(10f, 10f), new Vector2(-6f, -50f));
            rootObj = panelRect.gameObject;

            OrbitVisualizer2D vis = rootObj.AddComponent<OrbitVisualizer2D>();
            SerializedObject so = new SerializedObject(vis);

            TMP_Text header = CreateText(panelRect, "MissionStageHeaderText", "PHASE 4: 2D LAUNCH & ORBIT VISUALIZER", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRectTop(header.rectTransform, 10f, 32f);
            so.FindProperty("missionStageHeaderText").objectReferenceValue = header;

            RectTransform arena = CreatePanel(panelRect, "Orbit2DArena", new Color(0.02f, 0.03f, 0.07f, 1f));
            SetRectTop(arena, 46f, 390f);

            RectTransform shadowZone = CreatePanel(arena, "EclipseShadowZone", new Color(0f, 0f, 0f, 0.55f));
            SetAnchoredBox(shadowZone, new Vector2(0f, 0f), new Vector2(0.45f, 1f), Vector2.zero, Vector2.zero);
            TMP_Text shadowLbl = CreateText(shadowZone, "ShadowLabel", "ECLIPSE SHADOW ZONE\n(Battery Discharge)", 14, FontStyles.Italic, TextAlignmentOptions.Center);
            StretchFull(shadowLbl.rectTransform);

            RectTransform earth = CreatePanel(arena, "Earth2DBody", new Color(0.15f, 0.48f, 0.85f, 1f));
            earth.sizeDelta = new Vector2(124f, 124f);
            TMP_Text earthLbl = CreateText(earth, "EarthLabel", "EARTH\n(GMAT 2D)", 13, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchFull(earthLbl.rectTransform);
            so.FindProperty("centralBodyTransform").objectReferenceValue = earth;
            so.FindProperty("centralBodyLabel").objectReferenceValue = earthLbl;

            RectTransform beam = CreatePanel(arena, "GroundSignalLinkBeam", new Color(0.18f, 0.90f, 0.55f, 0.35f));
            beam.sizeDelta = new Vector2(200f, 54f);
            beam.anchoredPosition = new Vector2(105f, -95f);
            so.FindProperty("groundSignalLinkBeam").objectReferenceValue = beam.gameObject;

            RectTransform sat = CreatePanel(arena, "OrbitingCubeSat2D", new Color(0.95f, 0.77f, 0.15f, 1f));
            sat.sizeDelta = new Vector2(44f, 26f);
            sat.anchoredPosition = new Vector2(148f, 0f);
            so.FindProperty("cubeSatTransform").objectReferenceValue = sat;

            RectTransform wings = CreatePanel(sat, "DeployedSolarWings", new Color(0.20f, 0.65f, 0.98f, 1f));
            wings.sizeDelta = new Vector2(18f, 74f);
            so.FindProperty("deployedSolarWingsVisual").objectReferenceValue = wings.gameObject;

            TMP_Text zoneTxt = CreateText(panelRect, "OrbitZoneStatusText", "Status: READY FOR LAUNCH", 15, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRectTop(zoneTxt.rectTransform, 444f, 24f);
            so.FindProperty("orbitZoneStatusText").objectReferenceValue = zoneTxt;

            TMP_Text batTxt = CreateText(panelRect, "BatteryChargeText", "Battery SOC: 100%", 14, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRectTop(batTxt.rectTransform, 470f, 22f);
            so.FindProperty("batteryChargeText").objectReferenceValue = batTxt;

            TMP_Text dataTxt = CreateText(panelRect, "DataBufferText", "Onboard Buffer: 0 MB | Downlinked: 0 MB", 14, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRectTop(dataTxt.rectTransform, 494f, 22f);
            so.FindProperty("dataBufferAndDownlinkedText").objectReferenceValue = dataTxt;

            // FlightLogBox stretches dynamically from top=522px down to bottom=76px, leaving 76px for the 2 bottom buttons!
            RectTransform logBox = CreatePanel(panelRect, "FlightLogBox", new Color(0.03f, 0.05f, 0.08f, 0.98f));
            SetStretchBox(logBox, Vector2.zero, Vector2.one, new Vector2(14f, 76f), new Vector2(-14f, -522f));
            TMP_Text logTicker = CreateText(logBox, "FlightLogTickerText", "T-00:10 | Awaiting launch sequence...", 14, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            StretchFull(logTicker.rectTransform, 12f);
            so.FindProperty("flightLogTickerText").objectReferenceValue = logTicker;

            // Bottom-Anchored Buttons (Always 16px above the bottom edge of SimulationPanel!)
            Button backBtn = CreateButton(panelRect, "BackToBuilderButton", "<< BACK TO SPACECRAFT BUILDER", new Color(0.20f, 0.48f, 0.75f, 1f));
            SetBottomSlice(backBtn.GetComponent<RectTransform>(), 0.015f, 0.485f, 14f, 48f);
            so.FindProperty("backToBuilderButton").objectReferenceValue = backBtn;

            Button scoreBtn = CreateButton(panelRect, "ViewScorecardButton", "PROCEED TO NASA CML SCORECARD (PHASE 5) >>", new Color(0.18f, 0.68f, 0.38f, 1f));
            SetBottomSlice(scoreBtn.GetComponent<RectTransform>(), 0.505f, 0.985f, 14f, 48f);
            so.FindProperty("viewScorecardButton").objectReferenceValue = scoreBtn;

            so.ApplyModifiedPropertiesWithoutUndo();
            return vis;
        }

        private static ScorecardUI BuildScorecardPanel(Transform parent, out GameObject rootObj)
        {
            RectTransform panelRect = CreatePanel(parent, "ScorecardPanel", new Color(0.07f, 0.10f, 0.16f, 0.99f));
            SetStretchBox(panelRect, new Vector2(0f, 0f), new Vector2(0.73f, 1f), new Vector2(10f, 10f), new Vector2(-6f, -50f));
            rootObj = panelRect.gameObject;

            ScorecardUI sc = rootObj.AddComponent<ScorecardUI>();
            SerializedObject so = new SerializedObject(sc);

            TMP_Text title = CreateText(panelRect, "MissionTitleText", "NASA MISSION DEBRIEF & CML SCORECARD", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRectTop(title.rectTransform, 12f, 32f);
            so.FindProperty("missionTitleText").objectReferenceValue = title;

            TMP_Text banner = CreateText(panelRect, "CmlOutcomeBannerText", "CML 4 Point Design Verified", 15, FontStyles.Normal, TextAlignmentOptions.Center);
            SetRectTop(banner.rectTransform, 46f, 24f);
            so.FindProperty("cmlOutcomeBannerText").objectReferenceValue = banner;

            RectTransform badgeBg = CreatePanel(panelRect, "FinalGradeBadgeBackground", new Color(0.18f, 0.80f, 0.44f, 0.25f));
            SetRectTop(badgeBg, 76f, 76f);
            TMP_Text badgeTxt = CreateText(badgeBg, "FinalGradeBadgeText", "S", 42, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchFull(badgeTxt.rectTransform);
            so.FindProperty("finalGradeBadgeBackground").objectReferenceValue = badgeBg.GetComponent<Image>();
            so.FindProperty("finalGradeBadgeText").objectReferenceValue = badgeTxt;

            string[] segProps = { "launchSegmentResultText", "spaceSegmentResultText", "groundSegmentResultText", "userSegmentResultText" };
            for (int i = 0; i < 4; i++)
            {
                TMP_Text segRow = CreateText(panelRect, $"SegmentRow_{i}", "Segment Status", 14, FontStyles.Normal, TextAlignmentOptions.Left);
                SetRectTop(segRow.rectTransform, 160f + i * 42f, 38f);
                so.FindProperty(segProps[i]).objectReferenceValue = segRow;
            }

            TMP_Text sciStat = CreateText(panelRect, "FinalScienceScoreText", "Science Score: 90 / 85 pts", 15, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRectTop(sciStat.rectTransform, 336f, 24f);
            so.FindProperty("finalScienceScoreText").objectReferenceValue = sciStat;

            TMP_Text budStat = CreateText(panelRect, "FinalBudgetReserveText", "Budget Reserve: 20%", 15, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRectTop(budStat.rectTransform, 362f, 24f);
            so.FindProperty("finalBudgetReserveText").objectReferenceValue = budStat;

            TMP_Text datStat = CreateText(panelRect, "FinalDataDownlinkedText", "Telecom Link: 60 Mbps", 15, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRectTop(datStat.rectTransform, 388f, 24f);
            so.FindProperty("finalDataDownlinkedText").objectReferenceValue = datStat;

            // ChiefEngineerDebriefBox stretches dynamically from top=420px down to bottom=76px!
            RectTransform debriefBox = CreatePanel(panelRect, "ChiefEngineerDebriefBox", new Color(0.04f, 0.06f, 0.10f, 0.95f));
            SetStretchBox(debriefBox, Vector2.zero, Vector2.one, new Vector2(14f, 76f), new Vector2(-14f, -420f));
            TMP_Text debriefTxt = CreateText(debriefBox, "ChiefEngineerAdviceText", "NASA Chief Engineer flight logs...", 13, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            StretchFull(debriefTxt.rectTransform, 12f);
            so.FindProperty("chiefEngineerAdviceText").objectReferenceValue = debriefTxt;

            // Bottom-Anchored Redesign Button (Always 14px above the bottom edge!)
            Button redesignBtn = CreateButton(panelRect, "RedesignInBuilderButton", "<< REDESIGN SPACECRAFT (RETURN TO CML 3 BUILDER)", new Color(0.20f, 0.48f, 0.75f, 1f));
            SetBottomSlice(redesignBtn.GetComponent<RectTransform>(), 0.02f, 0.98f, 14f, 48f);
            so.FindProperty("redesignInBuilderButton").objectReferenceValue = redesignBtn;

            so.ApplyModifiedPropertiesWithoutUndo();
            return sc;
        }

        private static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<RectTransform>();
        }

        private static TMP_Text CreateText(Transform parent, string name, string content, int fontSize, FontStyles style, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = Color.white;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string label, Color bgColor)
        {
            RectTransform rt = CreatePanel(parent, name, bgColor);
            Button btn = rt.gameObject.AddComponent<Button>();
            TMP_Text txt = CreateText(rt, "Label", label, 13, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchFull(txt.rectTransform, 4f);
            return btn;
        }

        private static void StretchFull(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private static void SetStretchBox(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static void SetRectTop(RectTransform rt, float topY, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -topY);
            rt.sizeDelta = new Vector2(-28f, height);
        }

        private static void SetHorizontalSlice(RectTransform rt, float minX, float maxX, float topOffset, float height)
        {
            rt.anchorMin = new Vector2(minX, 1f);
            rt.anchorMax = new Vector2(maxX, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, topOffset);
            rt.sizeDelta = new Vector2(-8f, height);
        }

        private static void SetBottomSlice(RectTransform rt, float minX, float maxX, float bottomMargin, float height)
        {
            rt.anchorMin = new Vector2(minX, 0f);
            rt.anchorMax = new Vector2(maxX, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, bottomMargin);
            rt.sizeDelta = new Vector2(0f, height);
        }

        private static void SetAnchoredBox(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        private static void EnsureFolderExists(string assetFolderPath)
        {
            if (!AssetDatabase.IsValidFolder(assetFolderPath))
            {
                int lastSlash = assetFolderPath.LastIndexOf('/');
                string parent = assetFolderPath.Substring(0, lastSlash);
                string folderName = assetFolderPath.Substring(lastSlash + 1);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
#endif
    }
}
