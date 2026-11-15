using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceMissionGame.Core;
using SpaceMissionGame.Scenarios;

namespace SpaceMissionGame.UI.Theme
{
    /// <summary>
    /// [Member 3 - HanMing] Spaceflight Simulator-inspired 2D Home Screen Overlay (`HomeScreenOverlay.cs`).
    ///
    /// Automatically mounts onto `NASA_MissionCockpit_Canvas` when `MainGame.unity` starts,
    /// with ZERO edits required to Member 2's scene or prefabs!
    /// </summary>
    public class HomeScreenOverlay : MonoBehaviour
    {
        public static HomeScreenOverlay Instance { get; private set; }

        private GameObject _homeRoot;
        private CanvasGroup _canvasGroup;
        private CockpitUIAutoBuilder _cockpit;
        private BuilderUI _builderUI;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateHomeScreen()
        {
            CockpitUIAutoBuilder cockpit = FindAnyObjectByType<CockpitUIAutoBuilder>();
            if (cockpit == null) return;

            if (cockpit.GetComponent<HomeScreenOverlay>() == null)
            {
                cockpit.gameObject.AddComponent<HomeScreenOverlay>();
            }
        }

        private void Awake()
        {
            Instance = this;
            _cockpit = GetComponent<CockpitUIAutoBuilder>();
            _builderUI = _cockpit != null ? _cockpit.builderUI : FindAnyObjectByType<BuilderUI>();
        }

        private void Start()
        {
            BuildHomeScreenUI();
            ConvertTopBarToCompactDropdown();
            ShowHomeScreen();
        }

        public void ShowHomeScreen()
        {
            if (StarshipLaunchVisualizer2D.Instance != null)
                StarshipLaunchVisualizer2D.Instance.CancelLaunch();

            if (_homeRoot == null) BuildHomeScreenUI();
            _homeRoot.SetActive(true);
            _homeRoot.transform.SetAsLastSibling();
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        }

        public void HideHomeScreen()
        {
            if (_homeRoot != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(FadeOutHome());
            }
            else if (_homeRoot != null)
            {
                _homeRoot.SetActive(false);
            }
        }

        private IEnumerator FadeOutHome()
        {
            float t = 0f;
            while (t < 0.28f)
            {
                t += Time.deltaTime;
                if (_canvasGroup != null) _canvasGroup.alpha = 1f - (t / 0.28f);
                yield return null;
            }
            if (_homeRoot != null) _homeRoot.SetActive(false);
        }

        private void LaunchMissionFromHome(int missionIndex)
        {
            if (_builderUI != null)
            {
                _builderUI.SelectMissionPreset(missionIndex);
            }
            if (_cockpit != null)
            {
                _cockpit.SwitchMainScreen("BUILDER");
            }
            HideHomeScreen();
        }

        private void PlayLaunchDemoFromHome()
        {
            if (_builderUI != null)
            {
                _builderUI.SelectMissionPreset(2); // Artemis Lunar 6U preset
            }
            HideHomeScreen();

            if (_cockpit != null)
            {
                _cockpit.SwitchMainScreen("SIMULATION");
            }

            if (StarshipLaunchVisualizer2D.Instance != null && _builderUI != null)
            {
                StarshipLaunchVisualizer2D.Instance.PlayLaunchSequence(
                    _builderUI.CurrentDesign,
                    _builderUI.LastEvaluation,
                    null
                );
            }
        }

        private void ConvertTopBarToCompactDropdown()
        {
            Transform topBar = transform.Find("TopScreenSwitcherBar");
            if (topBar == null || topBar.Find("CompactMenuBtn") != null) return;

            // Hide the bulky wide bar background & 3 wide buttons
            Image barImg = topBar.GetComponent<Image>();
            if (barImg != null) barImg.enabled = false;
            Transform barBorder = topBar.Find("Theme_Border");
            if (barBorder != null) barBorder.gameObject.SetActive(false);

            for (int i = 0; i < topBar.childCount; i++)
            {
                Transform child = topBar.GetChild(i);
                if (child.name != "Theme_Border") child.gameObject.SetActive(false);
            }

            // Compact "MENU ▾" Pill Button in Top-Left
            GameObject menuBtnGo = new GameObject("CompactMenuBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            menuBtnGo.transform.SetParent(topBar, false);
            RectTransform mRt = menuBtnGo.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0f, 0.5f);
            mRt.anchorMax = new Vector2(0f, 0.5f);
            mRt.pivot = new Vector2(0f, 0.5f);
            mRt.sizeDelta = new Vector2(185f, 32f);
            mRt.anchoredPosition = new Vector2(0f, 0f);
            menuBtnGo.GetComponent<Image>().color = new Color(0.11f, 0.17f, 0.28f, 0.94f);

            TMP_Text menuTxt = CreateText(mRt, "Label", "MENU  ▼", 13, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(165f, 26f));

            // Floating Dropdown Panel (attached to Canvas root so it renders above Builder/Sim/Scorecard)
            GameObject dropGo = new GameObject("TopNavDropdownPanel", typeof(RectTransform), typeof(Image));
            dropGo.transform.SetParent(transform, false);
            RectTransform dRt = dropGo.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 1f);
            dRt.anchorMax = new Vector2(0f, 1f);
            dRt.pivot = new Vector2(0f, 1f);
            dRt.sizeDelta = new Vector2(245f, 178f);
            dRt.anchoredPosition = new Vector2(10f, -46f);
            dropGo.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.16f, 0.96f);
            dropGo.SetActive(false);

            void ToggleDropdown()
            {
                bool next = !dropGo.activeSelf;
                dropGo.SetActive(next);
                if (next) dropGo.transform.SetAsLastSibling();
                menuTxt.text = next ? "MENU  ▲" : "MENU  ▼";
            }

            menuBtnGo.GetComponent<Button>().onClick.AddListener(ToggleDropdown);

            AddDropdownItem(dRt, "Home", new Color(0.14f, 0.22f, 0.36f, 1f), -12f, () =>
            {
                ToggleDropdown();
                ShowHomeScreen();
            });
            AddDropdownItem(dRt, "1. Spacecraft Builder", new Color(0.12f, 0.34f, 0.54f, 1f), -52f, () =>
            {
                ToggleDropdown();
                if (StarshipLaunchVisualizer2D.Instance != null) StarshipLaunchVisualizer2D.Instance.CancelLaunch();
                if (_cockpit != null && _cockpit.screenBuilderButton != null) _cockpit.screenBuilderButton.onClick.Invoke();
            });
            AddDropdownItem(dRt, "2. 2D Orbit / Launchpad", new Color(0.12f, 0.34f, 0.54f, 1f), -92f, () =>
            {
                ToggleDropdown();
                if (StarshipLaunchVisualizer2D.Instance != null) StarshipLaunchVisualizer2D.Instance.CancelLaunch();
                if (_cockpit != null && _cockpit.screenSimulationButton != null) _cockpit.screenSimulationButton.onClick.Invoke();
            });
            AddDropdownItem(dRt, "3. NASA CML Scorecard", new Color(0.12f, 0.34f, 0.54f, 1f), -132f, () =>
            {
                ToggleDropdown();
                if (StarshipLaunchVisualizer2D.Instance != null) StarshipLaunchVisualizer2D.Instance.CancelLaunch();
                if (_cockpit != null && _cockpit.screenScorecardButton != null) _cockpit.screenScorecardButton.onClick.Invoke();
            });

            SpaceUITheme.StyleHierarchy(menuBtnGo.transform, true);
            SpaceUITheme.StyleHierarchy(dropGo.transform, true);
        }

        private static void AddDropdownItem(Transform parent, string label, Color col, float yOffset, UnityEngine.Events.UnityAction onClick)
        {
            GameObject item = new GameObject("DropItem_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            item.transform.SetParent(parent, false);
            RectTransform rt = item.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(225f, 34f);
            rt.anchoredPosition = new Vector2(0f, yOffset);
            item.GetComponent<Image>().color = col;
            item.GetComponent<Button>().onClick.AddListener(onClick);
            CreateText(rt, "Label", label, 12, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(210f, 26f));
        }

        private void BuildHomeScreenUI()
        {
            if (_homeRoot != null) return;

            _homeRoot = new GameObject("HomeScreenOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _homeRoot.transform.SetParent(transform, false);
            _canvasGroup = _homeRoot.GetComponent<CanvasGroup>();

            RectTransform rootRt = _homeRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // Deep space backdrop
            Image bg = _homeRoot.GetComponent<Image>();
            bg.color = new Color(0.03f, 0.045f, 0.095f, 1f);

            // Reuse SpaceUITheme starfield & horizon look
            GameObject decor = new GameObject("CockpitBackground", typeof(RectTransform), typeof(Image));
            decor.transform.SetParent(_homeRoot.transform, false);
            RectTransform decorRt = decor.GetComponent<RectTransform>();
            decorRt.anchorMin = Vector2.zero;
            decorRt.anchorMax = Vector2.one;
            decorRt.offsetMin = Vector2.zero;
            decorRt.offsetMax = Vector2.zero;
            decor.GetComponent<Image>().raycastTarget = false;

            // Moon in top-right sky
            GameObject moon = new GameObject("PlanetBackdrop2D", typeof(RectTransform), typeof(Image));
            moon.transform.SetParent(_homeRoot.transform, false);
            RectTransform moonRt = moon.GetComponent<RectTransform>();
            moonRt.anchorMin = new Vector2(0.84f, 0.78f);
            moonRt.anchorMax = new Vector2(0.84f, 0.78f);
            moonRt.sizeDelta = new Vector2(150f, 150f);
            moon.GetComponent<Image>().color = new Color(0.68f, 0.71f, 0.77f, 1f);
            moon.GetComponent<Image>().raycastTarget = false;

            // Left Hero Rocket Silhouette (2D Starship + Super Heavy)
            BuildHeroRocketVisual(_homeRoot.transform);

            // Main Title Header (Official NASA Blue #0B3D91 & NASA Red #FC3D21)
            CreateText(
                _homeRoot.transform,
                "HeroBadge",
                "NASA SPACE APPS CHALLENGE 2026  •  CML 1–4 MISSION ARCHITECT",
                15,
                FontStyles.Bold,
                SpaceUITheme.NasaRed,
                new Vector2(60f, 340f),
                new Vector2(1100f, 32f)
            );

            CreateText(
                _homeRoot.transform,
                "HeroTitle",
                "ORBITAL ARCHITECT",
                62,
                FontStyles.Bold,
                Color.white,
                new Vector2(60f, 285f),
                new Vector2(1100f, 84f)
            );

            CreateText(
                _homeRoot.transform,
                "HeroSubtitle",
                "Design CubeSat & SmallSat missions, balance mass, power & telecom margins, pass CML 4 reviews, and launch aboard Starship.",
                19,
                FontStyles.Normal,
                new Color(0.84f, 0.87f, 0.92f, 1f),
                new Vector2(60f, 215f),
                new Vector2(960f, 54f)
            );

            // 3 Mission Select Cards
            List<MissionProfile> missions = MissionScenariosData.GetAllMissions();
            string[] diffTags = { "EASY  •  1U ISS CUBESAT", "MEDIUM  •  3U POLAR SSO", "HARD  •  6U LUNAR DEEP SPACE" };
            Color[] accentCols =
            {
                new Color(0.18f, 0.78f, 0.46f, 1f),
                SpaceUITheme.NasaBlueBright,
                SpaceUITheme.NasaRed
            };

            for (int i = 0; i < 3 && i < missions.Count; i++)
            {
                int idx = i;
                MissionProfile m = missions[i];
                float xPos = -240f + i * 335f;

                GameObject card = new GameObject($"MissionCard_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                card.transform.SetParent(_homeRoot.transform, false);
                RectTransform crt = card.GetComponent<RectTransform>();
                crt.sizeDelta = new Vector2(315f, 250f);
                crt.anchoredPosition = new Vector2(xPos + 60f, 10f);
                card.GetComponent<Image>().color = new Color(0.075f, 0.090f, 0.125f, 0.95f);

                Button btn = card.GetComponent<Button>();
                btn.onClick.AddListener(() => LaunchMissionFromHome(idx));

                CreateText(crt, "DiffTag", diffTags[i], 13, FontStyles.Bold, accentCols[i], new Vector2(0f, 95f), new Vector2(280f, 24f));
                CreateText(crt, "MTitle", m.Title, 18, FontStyles.Bold, Color.white, new Vector2(0f, 52f), new Vector2(280f, 48f));
                CreateText(crt, "MDesc", $"Grant Cap: ${m.BudgetCapThousands:F0}k\nMin Science: {m.TargetScienceScore} pts\nOrbit: {m.RequiredOrbitId}", 14, FontStyles.Normal, new Color(0.76f, 0.80f, 0.86f, 1f), new Vector2(0f, -20f), new Vector2(280f, 74f));
                CreateText(crt, "MStart", "SELECT & DESIGN  >>", 14, FontStyles.Bold, accentCols[i], new Vector2(0f, -92f), new Vector2(280f, 28f));
            }

            // Bottom Action Buttons
            CreateHomeActionButton(
                _homeRoot.transform,
                "QuickStartBtn",
                "ENTER MISSION CONTROL (BISON-SAT 3U)",
                SpaceUITheme.NasaBlue,
                new Vector2(-110f, -195f),
                new Vector2(390f, 54f),
                () => LaunchMissionFromHome(1)
            );

            CreateHomeActionButton(
                _homeRoot.transform,
                "WatchLaunchDemoBtn",
                "WATCH STARSHIP LAUNCH DEMO",
                SpaceUITheme.NasaRed,
                new Vector2(280f, -195f),
                new Vector2(330f, 54f),
                PlayLaunchDemoFromHome
            );

            SpaceUITheme.StyleHierarchy(_homeRoot.transform, true);
        }

        private void BuildHeroRocketVisual(Transform parent)
        {
            StarshipLaunchVisualizer2D.CreateStaticSfsRocketDisplay(parent);
        }

        private static TMP_Text CreateText(Transform parent, string name, string content, int size, FontStyles style, Color col, Vector2 pos, Vector2 box)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;

            TMP_Text t = go.GetComponent<TextMeshProUGUI>();
            t.text = content;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = col;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        private static void CreateHomeActionButton(Transform parent, string name, string label, Color col, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = col;
            go.GetComponent<Button>().onClick.AddListener(onClick);

            CreateText(rt, "Label", label, 15, FontStyles.Bold, Color.white, Vector2.zero, size - new Vector2(16f, 8f));
        }
    }
}
