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
    /// Ensures the entire 2D Cockpit UI is:
    /// 1. Visible in Edit Mode and Play Mode (1920x1080 Canvas with dark NASA Mission Control styling).
    /// 2. Permanently saved into `Assets/_Project/Scenes/MainGame.unity` and `Assets/_Project/Prefabs/`.
    /// 3. Automatically wired with all `[SerializeField]` references on `BuilderUI`, `ResourceBarsUI`,
    ///    `OrbitVisualizer2D`, and `ScorecardUI`.
    /// </summary>
    public class CockpitUIAutoBuilder : MonoBehaviour
    {
        [Header("Main Phase Panels (Persistent in Scene)")]
        public GameObject builderPhaseRoot;
        public GameObject simulationPhaseRoot;
        public GameObject scorecardPhaseRoot;

        [Header("Core Member 2 Controllers")]
        public BuilderUI builderUI;
        public ResourceBarsUI resourceBarsUI;
        public OrbitVisualizer2D orbitVisualizer2D;
        public ScorecardUI scorecardUI;

        private void Start()
        {
            // Wire navigation events between Member 2's panels so you can test the full loop right away
            if (builderUI != null)
            {
                builderUI.OnProceedToCML4ReviewRequested += () =>
                {
                    ShowPhasePanel("SIMULATION");
                    if (orbitVisualizer2D != null)
                    {
                        orbitVisualizer2D.StartSimulation(builderUI.CurrentDesign, builderUI.LastEvaluation);
                    }
                };
            }

            if (orbitVisualizer2D != null)
            {
                orbitVisualizer2D.OnProceedToScorecardRequested += () =>
                {
                    ShowPhasePanel("SCORECARD");
                    if (scorecardUI != null && builderUI != null)
                    {
                        scorecardUI.ShowScorecard(builderUI.CurrentDesign, builderUI.LastEvaluation);
                    }
                };
            }

            if (scorecardUI != null)
            {
                scorecardUI.OnRedesignSpacecraftRequested += () => ShowPhasePanel("BUILDER");
                scorecardUI.OnReturnToMissionSelectRequested += () => ShowPhasePanel("BUILDER");
            }
        }

        public void ShowPhasePanel(string phaseKey)
        {
            if (builderPhaseRoot != null) builderPhaseRoot.SetActive(phaseKey == "BUILDER");
            if (simulationPhaseRoot != null) simulationPhaseRoot.SetActive(phaseKey == "SIMULATION");
            if (scorecardPhaseRoot != null) scorecardPhaseRoot.SetActive(phaseKey == "SCORECARD");
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Space Mission Game/Build & Save Permanent 2D UI (Scene + Prefabs)")]
        public static void BuildAndSavePermanent2DUI()
        {
            // 1. Ensure 2D Camera & EventSystem exist in the scene
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
                esObj.AddComponent<StandaloneInputModule>();
            }

            // Remove old auto-generated Canvas if rebuilding
            GameObject existingCanvas = GameObject.Find("NASA_MissionCockpit_Canvas");
            if (existingCanvas != null)
            {
                DestroyImmediate(existingCanvas);
            }

            // 2. Create Permanent Root 2D Canvas (1920 x 1080)
            GameObject canvasObj = new GameObject("NASA_MissionCockpit_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            CockpitUIAutoBuilder rootController = canvasObj.AddComponent<CockpitUIAutoBuilder>();

            // Fullscreen dark space background
            RectTransform bgRect = CreatePanel(canvasObj.transform, "CockpitBackground", new Color(0.06f, 0.08f, 0.13f, 1f));
            StretchFull(bgRect);

            // 3. Build Persistent Right Sidebar: Live Resource Bars Panel (`ResourceBarsUI`)
            ResourceBarsUI resourceBars = BuildResourceBarsPanel(canvasObj.transform);
            rootController.resourceBarsUI = resourceBars;

            // 4. Build Phase 2 Panel: Spacecraft Builder (`BuilderUI`)
            BuilderUI builder = BuildSpacecraftBuilderPanel(canvasObj.transform, resourceBars, out GameObject builderRoot);
            rootController.builderPhaseRoot = builderRoot;
            rootController.builderUI = builder;

            // 5. Build Phase 4 Panel: 2D Orbit & Launch Visualizer (`OrbitVisualizer2D`)
            OrbitVisualizer2D orbitVis = BuildOrbitSimulationPanel(canvasObj.transform, out GameObject simRoot);
            rootController.simulationPhaseRoot = simRoot;
            rootController.orbitVisualizer2D = orbitVis;

            // 6. Build Phase 5 Panel: NASA CML Scorecard (`ScorecardUI`)
            ScorecardUI scorecard = BuildScorecardPanel(canvasObj.transform, out GameObject scorecardRoot);
            rootController.scorecardPhaseRoot = scorecardRoot;
            rootController.scorecardUI = scorecard;

            // Default active view: Builder active, Simulation & Scorecard hidden until triggered
            builderRoot.SetActive(true);
            simRoot.SetActive(false);
            scorecardRoot.SetActive(false);

            // 7. Save Permanent Prefabs into `Assets/_Project/Prefabs/`
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

            // 8. Mark Scene Dirty & Save Permanently to `Assets/_Project/Scenes/MainGame.unity`
            EditorUtility.SetDirty(canvasObj);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/_Project/Scenes/MainGame.unity");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Member 2 UI Builder] Permanent 2D UI Canvas, Prefabs, and MainGame.unity saved successfully!");
        }

        private static ResourceBarsUI BuildResourceBarsPanel(Transform parent)
        {
            RectTransform panelRect = CreatePanel(parent, "ResourceBarsPanel", new Color(0.09f, 0.12f, 0.18f, 0.98f));
            SetAnchoredBox(panelRect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-280f, 0f), new Vector2(540f, -20f));

            ResourceBarsUI rb = panelRect.gameObject.AddComponent<ResourceBarsUI>();
            SerializedObject so = new SerializedObject(rb);

            TMP_Text header = CreateText(panelRect, "HeaderTitle", "LIVE ENGINEERING RESOURCES (CML 4)", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRectTop(header.rectTransform, 15f, 36f);

            TMP_Text readiness = CreateText(panelRect, "OverallReadinessText", "CML 4 RYG Checklist: 86% GREEN — GO FOR LAUNCH", 16, FontStyles.Bold, TextAlignmentOptions.Center);
            readiness.color = new Color(0.18f, 0.80f, 0.44f);
            SetRectTop(readiness.rectTransform, 55f, 32f);
            so.FindProperty("overallReadinessText").objectReferenceValue = readiness;

            string[] meterNames = { "massMeter", "powerMeter", "budgetMeter", "dataMeter", "scienceMeter", "marginRiskMeter" };
            string[] defaultTitles = { "Mass", "Power Load", "Budget", "Telecom Link", "Science Return", "Reliability" };

            for (int i = 0; i < meterNames.Length; i++)
            {
                ResourceMeterItemUI meter = CreateSingleMeter(panelRect, defaultTitles[i], 105f + i * 115f);
                so.FindProperty(meterNames[i]).objectReferenceValue = meter;
            }

            RectTransform diagBox = CreatePanel(panelRect, "DiagnosticBannerBox", new Color(0.05f, 0.07f, 0.11f, 0.95f));
            SetRectTop(diagBox, 810f, 180f);
            TMP_Text diagText = CreateText(diagBox, "TradeOffDiagnosticText", "[ALL SYSTEMS NOMINAL] Select parts on the left to evaluate real-time NASA trade-offs.", 15, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            StretchFull(diagText.rectTransform, 14f);
            so.FindProperty("tradeOffDiagnosticText").objectReferenceValue = diagText;

            so.ApplyModifiedPropertiesWithoutUndo();
            return rb;
        }

        private static ResourceMeterItemUI CreateSingleMeter(Transform parent, string label, float topOffset)
        {
            RectTransform itemRect = CreatePanel(parent, $"Meter_{label}", new Color(0.13f, 0.17f, 0.24f, 0.9f));
            SetRectTop(itemRect, topOffset, 100f);

            ResourceMeterItemUI item = itemRect.gameObject.AddComponent<ResourceMeterItemUI>();
            SerializedObject so = new SerializedObject(item);

            TMP_Text titleTxt = CreateText(itemRect, "TitleText", label, 16, FontStyles.Bold, TextAlignmentOptions.Left);
            SetAnchoredBox(titleTxt.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(15f, -22f), new Vector2(-20f, 28f));

            TMP_Text badgeTxt = CreateText(itemRect, "StatusBadgeText", "GREEN", 15, FontStyles.Bold, TextAlignmentOptions.Right);
            badgeTxt.color = new Color(0.18f, 0.80f, 0.44f);
            SetAnchoredBox(badgeTxt.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(-15f, -22f), new Vector2(-20f, 28f));

            TMP_Text valTxt = CreateText(itemRect, "ValueReadoutText", "0.0 / 0.0", 14, FontStyles.Normal, TextAlignmentOptions.Left);
            SetAnchoredBox(valTxt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-30f, 24f));

            RectTransform barBg = CreatePanel(itemRect, "BarBackground", new Color(0.05f, 0.07f, 0.10f, 1f));
            SetAnchoredBox(barBg, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 18f), new Vector2(-30f, 18f));

            RectTransform fillRect = CreatePanel(barBg, "FillBar", new Color(0.18f, 0.80f, 0.44f, 1f));
            StretchFull(fillRect);
            Image fillImg = fillRect.GetComponent<Image>();
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
            RectTransform panelRect = CreatePanel(parent, "BuilderPanel", new Color(0.07f, 0.10f, 0.15f, 0.98f));
            SetAnchoredBox(panelRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-275f, 0f), new Vector2(-570f, -20f));
            rootObj = panelRect.gameObject;

            BuilderUI builder = rootObj.AddComponent<BuilderUI>();
            SerializedObject so = new SerializedObject(builder);

            // 1. Top 4 Segment Tab Buttons
            string[] tabLabels = { "1. LAUNCH SEGMENT", "2. SPACE SEGMENT", "3. GROUND SEGMENT", "4. USER SEGMENT" };
            string[] tabProps = { "launchSegmentTabButton", "spaceSegmentTabButton", "groundSegmentTabButton", "userSegmentTabButton" };

            for (int i = 0; i < 4; i++)
            {
                Button btn = CreateButton(panelRect, $"TabBtn_{i}", tabLabels[i], new Color(0.15f, 0.28f, 0.45f, 1f));
                RectTransform rt = btn.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(i * 0.25f, 1f);
                rt.anchorMax = new Vector2((i + 1) * 0.25f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -12f);
                rt.sizeDelta = new Vector2(-16f, 46f);
                so.FindProperty(tabProps[i]).objectReferenceValue = btn;
            }

            TMP_Text segHeader = CreateText(panelRect, "ActiveSegmentHeaderText", "1. LAUNCH SEGMENT — Select CubeSat Form Factor & Orbit", 17, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRectTop(segHeader.rectTransform, 68f, 32f);
            so.FindProperty("activeSegmentHeaderText").objectReferenceValue = segHeader;

            // 2. Left Part Cards Container + Template Prefab
            RectTransform listBox = CreatePanel(panelRect, "PartCardListContainer", new Color(0.05f, 0.07f, 0.11f, 0.95f));
            SetAnchoredBox(listBox, new Vector2(0f, 0f), new Vector2(0.58f, 1f), new Vector2(10f, -45f), new Vector2(-20f, -130f));
            VerticalLayoutGroup vlg = listBox.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            so.FindProperty("partCardListContainer").objectReferenceValue = listBox;

            PartCardUI cardTemplate = BuildPartCardTemplate(panelRect);
            cardTemplate.gameObject.SetActive(false);
            so.FindProperty("partCardPrefab").objectReferenceValue = cardTemplate;

            // 3. Right-Center 2D CubeSat Slot Blueprint Preview (1U / 3U / 6U)
            RectTransform blueprintBox = CreatePanel(panelRect, "CubeSat2DBlueprintBox", new Color(0.09f, 0.13f, 0.20f, 0.95f));
            SetAnchoredBox(blueprintBox, new Vector2(0.59f, 0f), new Vector2(1f, 1f), new Vector2(-10f, -45f), new Vector2(-20f, -130f));

            TMP_Text chassisTitle = CreateText(blueprintBox, "ChassisTitleText", "2D CUBESAT BLUEPRINT (3U)", 17, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRectTop(chassisTitle.rectTransform, 14f, 48f);
            so.FindProperty("chassisTitleText").objectReferenceValue = chassisTitle;

            SerializedProperty slotsProp = so.FindProperty("cubeSatSlotBoxes");
            SerializedProperty labelsProp = so.FindProperty("cubeSatSlotLabels");
            slotsProp.arraySize = 6;
            labelsProp.arraySize = 6;

            for (int i = 0; i < 6; i++)
            {
                RectTransform slotRect = CreatePanel(blueprintBox, $"CubeSatBay_{i + 1}U", new Color(0.16f, 0.45f, 0.65f, 0.9f));
                SetRectTop(slotRect, 75f + i * 92f, 82f);
                TMP_Text slotLbl = CreateText(slotRect, "SlotLabel", $"Bay {i + 1}U: [Subsystem]", 14, FontStyles.Normal, TextAlignmentOptions.Center);
                StretchFull(slotLbl.rectTransform, 6f);

                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotRect.GetComponent<Image>();
                labelsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotLbl;
            }

            TMP_Text orbitRocketTxt = CreateText(blueprintBox, "SelectedOrbitAndRocketText", "Launch & Orbit: SSO Rideshare", 14, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRectTop(orbitRocketTxt.rectTransform, 640f, 44f);
            so.FindProperty("selectedOrbitAndRocketText").objectReferenceValue = orbitRocketTxt;

            TMP_Text gsTxt = CreateText(blueprintBox, "SelectedGroundStationText", "Ground Station: S-Band Dish", 14, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRectTop(gsTxt.rectTransform, 690f, 34f);
            so.FindProperty("selectedGroundStationText").objectReferenceValue = gsTxt;

            Button proceedBtn = CreateButton(blueprintBox, "ProceedToSimulationButton", "LAUNCH & SIMULATE ORBIT (PHASE 4) ▶", new Color(0.18f, 0.68f, 0.38f, 1f));
            SetRectTop(proceedBtn.GetComponent<RectTransform>(), 745f, 54f);
            so.FindProperty("proceedToReviewButton").objectReferenceValue = proceedBtn;
            so.FindProperty("resourceBarsUI").objectReferenceValue = resourceBars;

            so.ApplyModifiedPropertiesWithoutUndo();
            return builder;
        }

        private static PartCardUI BuildPartCardTemplate(Transform parent)
        {
            RectTransform cardRect = CreatePanel(parent, "PartCardTemplate", new Color(0.13f, 0.17f, 0.24f, 0.95f));
            cardRect.sizeDelta = new Vector2(680f, 128f);
            LayoutElement le = cardRect.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 128f;

            PartCardUI card = cardRect.gameObject.AddComponent<PartCardUI>();
            SerializedObject so = new SerializedObject(card);

            TMP_Text nameTxt = CreateText(cardRect, "PartNameText", "Component Name", 17, FontStyles.Bold, TextAlignmentOptions.Left);
            SetAnchoredBox(nameTxt.rectTransform, new Vector2(0f, 1f), new Vector2(0.75f, 1f), new Vector2(12f, -18f), new Vector2(-16f, 26f));

            TMP_Text badgeTxt = CreateText(cardRect, "CategoryBadgeText", "[SPACE • PAYLOAD]", 13, FontStyles.Bold, TextAlignmentOptions.Left);
            badgeTxt.color = new Color(0.45f, 0.78f, 0.98f);
            SetAnchoredBox(badgeTxt.rectTransform, new Vector2(0f, 1f), new Vector2(0.75f, 1f), new Vector2(12f, -42f), new Vector2(-16f, 20f));

            TMP_Text statsTxt = CreateText(cardRect, "StatsSummaryText", "Mass: 0.5kg | Power: -4W | Cost: $35k", 14, FontStyles.Normal, TextAlignmentOptions.Left);
            SetAnchoredBox(statsTxt.rectTransform, new Vector2(0f, 1f), new Vector2(0.78f, 1f), new Vector2(12f, -66f), new Vector2(-16f, 22f));

            TMP_Text descTxt = CreateText(cardRect, "TradeOffNoteText", "Trade-off description.", 13, FontStyles.Italic, TextAlignmentOptions.Left);
            descTxt.color = new Color(0.78f, 0.82f, 0.88f);
            SetAnchoredBox(descTxt.rectTransform, new Vector2(0f, 0f), new Vector2(0.78f, 0f), new Vector2(12f, 22f), new Vector2(-16f, 36f));

            Button selBtn = CreateButton(cardRect, "SelectPartButton", "SELECT PART", new Color(0.20f, 0.45f, 0.70f, 1f));
            SetAnchoredBox(selBtn.GetComponent<RectTransform>(), new Vector2(0.80f, 0.2f), new Vector2(0.98f, 0.8f), Vector2.zero, Vector2.zero);

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
            SetAnchoredBox(panelRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-275f, 0f), new Vector2(-570f, -20f));
            rootObj = panelRect.gameObject;

            OrbitVisualizer2D vis = rootObj.AddComponent<OrbitVisualizer2D>();
            SerializedObject so = new SerializedObject(vis);

            TMP_Text header = CreateText(panelRect, "MissionStageHeaderText", "PHASE 4: 2D LAUNCH & ORBIT VISUALIZER", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRectTop(header.rectTransform, 18f, 38f);
            so.FindProperty("missionStageHeaderText").objectReferenceValue = header;

            // 2D Orbit Display Arena
            RectTransform arena = CreatePanel(panelRect, "Orbit2DArena", new Color(0.02f, 0.03f, 0.07f, 1f));
            SetRectTop(arena, 68f, 520f);

            // Eclipse Shadow Overlay (Left half of orbit)
            RectTransform shadowZone = CreatePanel(arena, "EclipseShadowZone", new Color(0f, 0f, 0f, 0.55f));
            SetAnchoredBox(shadowZone, new Vector2(0f, 0f), new Vector2(0.45f, 1f), Vector2.zero, Vector2.zero);
            TMP_Text shadowLbl = CreateText(shadowZone, "ShadowLabel", "ECLIPSE SHADOW ZONE\n(Battery Discharge)", 14, FontStyles.Italic, TextAlignmentOptions.Center);
            StretchFull(shadowLbl.rectTransform);

            // Central Rotating Earth
            RectTransform earth = CreatePanel(arena, "Earth2DBody", new Color(0.15f, 0.48f, 0.85f, 1f));
            earth.sizeDelta = new Vector2(140f, 140f);
            TMP_Text earthLbl = CreateText(earth, "EarthLabel", "EARTH\n(GMAT 2D)", 14, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchFull(earthLbl.rectTransform);
            so.FindProperty("centralBodyTransform").objectReferenceValue = earth;

            // Ground Station Signal Beam
            RectTransform beam = CreatePanel(arena, "GroundSignalLinkBeam", new Color(0.18f, 0.90f, 0.55f, 0.35f));
            beam.sizeDelta = new Vector2(220f, 60f);
            beam.anchoredPosition = new Vector2(110f, -110f);
            so.FindProperty("groundSignalLinkBeam").objectReferenceValue = beam.gameObject;

            // Orbiting CubeSat Sprite
            RectTransform sat = CreatePanel(arena, "OrbitingCubeSat2D", new Color(0.95f, 0.77f, 0.15f, 1f));
            sat.sizeDelta = new Vector2(44f, 26f);
            sat.anchoredPosition = new Vector2(190f, 0f);
            so.FindProperty("cubeSatTransform").objectReferenceValue = sat;

            RectTransform wings = CreatePanel(sat, "DeployedSolarWings", new Color(0.20f, 0.65f, 0.98f, 1f));
            wings.sizeDelta = new Vector2(18f, 76f);
            so.FindProperty("deployedSolarWingsVisual").objectReferenceValue = wings.gameObject;

            // Telemetry & Flight Log Ticker
            TMP_Text zoneTxt = CreateText(panelRect, "OrbitZoneStatusText", "Status: READY FOR LAUNCH", 17, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRectTop(zoneTxt.rectTransform, 600f, 30f);
            so.FindProperty("orbitZoneStatusText").objectReferenceValue = zoneTxt;

            TMP_Text batTxt = CreateText(panelRect, "BatteryChargeText", "Battery SOC: 100%", 16, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRectTop(batTxt.rectTransform, 632f, 28f);
            so.FindProperty("batteryChargeText").objectReferenceValue = batTxt;

            TMP_Text dataTxt = CreateText(panelRect, "DataBufferText", "Onboard Buffer: 0 MB | Downlinked: 0 MB", 16, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRectTop(dataTxt.rectTransform, 662f, 28f);
            so.FindProperty("dataBufferAndDownlinkedText").objectReferenceValue = dataTxt;

            RectTransform logBox = CreatePanel(panelRect, "FlightLogBox", new Color(0.03f, 0.05f, 0.08f, 0.98f));
            SetRectTop(logBox, 700f, 230f);
            TMP_Text logTicker = CreateText(logBox, "FlightLogTickerText", "T-00:10 | Awaiting launch sequence...", 14, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            StretchFull(logTicker.rectTransform, 12f);
            so.FindProperty("flightLogTickerText").objectReferenceValue = logTicker;

            Button scoreBtn = CreateButton(panelRect, "ViewScorecardButton", "PROCEED TO NASA CML SCORECARD (PHASE 5) ▶", new Color(0.18f, 0.68f, 0.38f, 1f));
            SetRectTop(scoreBtn.GetComponent<RectTransform>(), 945f, 52f);
            so.FindProperty("viewScorecardButton").objectReferenceValue = scoreBtn;

            so.ApplyModifiedPropertiesWithoutUndo();
            return vis;
        }

        private static ScorecardUI BuildScorecardPanel(Transform parent, out GameObject rootObj)
        {
            RectTransform panelRect = CreatePanel(parent, "ScorecardPanel", new Color(0.07f, 0.10f, 0.16f, 0.99f));
            SetAnchoredBox(panelRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-275f, 0f), new Vector2(-570f, -20f));
            rootObj = panelRect.gameObject;

            ScorecardUI sc = rootObj.AddComponent<ScorecardUI>();
            SerializedObject so = new SerializedObject(sc);

            TMP_Text title = CreateText(panelRect, "MissionTitleText", "NASA MISSION DEBRIEF & CML SCORECARD", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRectTop(title.rectTransform, 20f, 38f);
            so.FindProperty("missionTitleText").objectReferenceValue = title;

            TMP_Text banner = CreateText(panelRect, "CmlOutcomeBannerText", "CML 4 Point Design Verified", 17, FontStyles.Normal, TextAlignmentOptions.Center);
            SetRectTop(banner.rectTransform, 62f, 30f);
            so.FindProperty("cmlOutcomeBannerText").objectReferenceValue = banner;

            RectTransform badgeBg = CreatePanel(panelRect, "FinalGradeBadgeBackground", new Color(0.18f, 0.80f, 0.44f, 0.25f));
            SetRectTop(badgeBg, 102f, 95f);
            TMP_Text badgeTxt = CreateText(badgeBg, "FinalGradeBadgeText", "S", 48, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchFull(badgeTxt.rectTransform);
            so.FindProperty("finalGradeBadgeBackground").objectReferenceValue = badgeBg.GetComponent<Image>();
            so.FindProperty("finalGradeBadgeText").objectReferenceValue = badgeTxt;

            string[] segProps = { "launchSegmentResultText", "spaceSegmentResultText", "groundSegmentResultText", "userSegmentResultText" };
            for (int i = 0; i < 4; i++)
            {
                TMP_Text segRow = CreateText(panelRect, $"SegmentRow_{i}", "Segment Status", 16, FontStyles.Normal, TextAlignmentOptions.Left);
                SetRectTop(segRow.rectTransform, 215f + i * 52f, 46f);
                so.FindProperty(segProps[i]).objectReferenceValue = segRow;
            }

            TMP_Text sciStat = CreateText(panelRect, "FinalScienceScoreText", "Science Score: 90 / 85 pts", 16, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRectTop(sciStat.rectTransform, 440f, 30f);
            so.FindProperty("finalScienceScoreText").objectReferenceValue = sciStat;

            TMP_Text budStat = CreateText(panelRect, "FinalBudgetReserveText", "Budget Reserve: 20%", 16, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRectTop(budStat.rectTransform, 474f, 30f);
            so.FindProperty("finalBudgetReserveText").objectReferenceValue = budStat;

            TMP_Text datStat = CreateText(panelRect, "FinalDataDownlinkedText", "Telecom Link: 60 Mbps", 16, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRectTop(datStat.rectTransform, 508f, 30f);
            so.FindProperty("finalDataDownlinkedText").objectReferenceValue = datStat;

            RectTransform debriefBox = CreatePanel(panelRect, "ChiefEngineerDebriefBox", new Color(0.04f, 0.06f, 0.10f, 0.95f));
            SetRectTop(debriefBox, 550f, 350f);
            TMP_Text debriefTxt = CreateText(debriefBox, "ChiefEngineerAdviceText", "NASA Chief Engineer flight logs...", 14, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            StretchFull(debriefTxt.rectTransform, 14f);
            so.FindProperty("chiefEngineerAdviceText").objectReferenceValue = debriefTxt;

            Button redesignBtn = CreateButton(panelRect, "RedesignInBuilderButton", "◀ REDESIGN SPACECRAFT (RETURN TO CML 3 BUILDER)", new Color(0.20f, 0.48f, 0.75f, 1f));
            SetRectTop(redesignBtn.GetComponent<RectTransform>(), 925f, 54f);
            so.FindProperty("redesignInBuilderButton").objectReferenceValue = redesignBtn;

            so.ApplyModifiedPropertiesWithoutUndo();
            return sc;
        }

        // --- UI Creation Helpers ---

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
            TMP_Text txt = CreateText(rt, "Label", label, 15, FontStyles.Bold, TextAlignmentOptions.Center);
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

        private static void SetRectTop(RectTransform rt, float topY, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -topY);
            rt.sizeDelta = new Vector2(-36f, height);
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
