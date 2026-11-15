using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

using SpaceMissionGame.Core;

namespace SpaceMissionGame.UI.Theme
{
    /// <summary>
    /// [Member 3 - HanMing] Runtime "Clean Flat Space" UI Skin (inspired by minimalist 2D rocket games).
    ///
    /// HOW IT WORKS (no image files needed!):
    /// 1. When MainGame.unity starts, this script finds Member 2's cockpit Canvas automatically.
    /// 2. It procedurally DRAWS its own textures in code (rounded panels, circles, glow, starfield, dashed orbit ring).
    /// 3. It walks every Image / Button / Text in the Canvas and restyles them by GameObject name:
    ///    - Panels   -> rounded translucent "glass" cards with a soft blue border
    ///    - Buttons  -> rounded pill buttons with drop shadow, hover-grow and press-shrink
    ///    - Planets  -> round shaded planets with atmosphere glow, night side and a dashed orbit path
    ///    - Background -> deep-space gradient + starfield + giant planet horizon
    ///    - Text     -> clean OS font (Bahnschrift / Segoe UI) with slight letter spacing
    ///
    /// Because everything happens at runtime, Member 2's scene, prefabs and scripts are NOT modified
    /// (zero Git merge conflicts!). Delete this file and the game goes back to the original look.
    /// </summary>
    public class SpaceUITheme : MonoBehaviour
    {
        // =========================================================================
        // OFFICIAL NASA COLOR PALETTE (NASA Blue #0B3D91, NASA Red #FC3D21, Matte Console Slate)
        // =========================================================================
        public static readonly Color NasaBlue       = new Color(0.043f, 0.239f, 0.569f, 1f);    // #0B3D91
        public static readonly Color NasaBlueBright = new Color(0.110f, 0.376f, 0.753f, 1f);    // #1C60C0
        public static readonly Color NasaRed        = new Color(0.988f, 0.239f, 0.129f, 1f);    // #FC3D21
        public static readonly Color PanelColor     = new Color(0.065f, 0.078f, 0.105f, 0.90f); // Matte Obsidian Console
        public static readonly Color InsetColor     = new Color(0.045f, 0.055f, 0.078f, 0.88f); // Deep Instrument Bay
        public static readonly Color MeterColor     = new Color(0.095f, 0.115f, 0.155f, 0.94f); // Telemetry Module
        public static readonly Color BarBgColor     = new Color(0.025f, 0.030f, 0.045f, 1f);    // Meter Track
        public static readonly Color BorderColor    = new Color(0.22f, 0.28f, 0.38f, 0.65f);    // Crisp Hardware Hairline
        public static readonly Color ArenaColor     = new Color(0.020f, 0.025f, 0.040f, 0.98f); // Deep Space Viewport

        private const int RoundTexSize = 128;
        private const float RoundTexRadius = 18f;
        private const float BorderThickness = 2f;

        private static readonly HashSet<string> MainPanels = new HashSet<string>
        {
            "TopScreenSwitcherBar", "ResourceBarsPanel", "BuilderPanel", "SimulationPanel", "ScorecardPanel"
        };

        private static readonly HashSet<string> InsetPanels = new HashSet<string>
        {
            "PartCardScrollViewport", "CubeSat2DBlueprintBox", "DiagnosticBannerBox",
            "FlightLogBox", "ChiefEngineerDebriefBox"
        };

        private static Sprite _rounded, _roundedBorder, _planet, _glow, _orbitRing, _stars, _bgGradient, _fadeRight, _nightSide;
        private static Sprite _sfsHull, _sfsSolar, _sfsLens, _sfsDish, _sunlitRim;
        private static TMP_FontAsset _themeFont;

        // =========================================================================
        // AUTO BOOTSTRAP (runs after MainGame.unity loads, before Start())
        // =========================================================================
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            CockpitUIAutoBuilder cockpit = FindAnyObjectByType<CockpitUIAutoBuilder>();
            if (cockpit == null || cockpit.GetComponent<SpaceUITheme>() != null) return;

            cockpit.gameObject.AddComponent<SpaceUITheme>();

            // Pass 1: style immediately, so the part-card template is skinned BEFORE BuilderUI clones it.
            StyleHierarchy(cockpit.transform, true);
        }

        private IEnumerator Start()
        {
            // Pass 2: one frame later the CanvasScaler has final sizes, so corner radii are exact.
            yield return null;
            Canvas.ForceUpdateCanvases();
            StyleHierarchy(transform, true);
            PolishBuilderLayout(transform);
        }

        private static void PolishBuilderLayout(Transform root)
        {
            // Give the part cards more breathing room
            Transform listContainer = root.Find("BuilderPanel/PartCardScrollViewport/PartCardListContainer");
            if (listContainer != null)
            {
                VerticalLayoutGroup vlg = listContainer.GetComponent<VerticalLayoutGroup>();
                if (vlg != null)
                {
                    vlg.padding = new RectOffset(14, 14, 12, 12);
                    vlg.spacing = 12f;
                }
            }

            // Make the 9 loadout slots crisp matte instrument bays
            Transform blueprint = root.Find("BuilderPanel/CubeSat2DBlueprintBox");
            if (blueprint != null)
            {
                for (int i = 1; i <= 9; i++)
                {
                    Transform slot = blueprint.Find($"LoadoutSlot_{i}");
                    if (slot != null)
                    {
                        Image img = slot.GetComponent<Image>();
                        if (img != null) img.color = new Color(0.09f, 0.11f, 0.15f, 0.92f);
                    }
                }
            }

            // NASA Blue / NASA Red accents on primary Proceed / Launch buttons
            Transform proceed = root.Find("BuilderPanel/CubeSat2DBlueprintBox/ProceedToSimulationButton");
            if (proceed != null)
            {
                Image pImg = proceed.GetComponent<Image>();
                if (pImg != null) pImg.color = NasaRed;
            }
        }

        /// <summary>
        /// Restyles every Image, Button and Text under `root`. Safe to call multiple times.
        /// Also used by ReviewEventManager to skin the CML 4 Dilemma popup.
        /// </summary>
        public static void StyleHierarchy(Transform root, bool uppercaseButtons)
        {
            if (root == null) return;
            EnsureSprites();

            float refPPU = 100f;
            Canvas canvas = root.GetComponentInParent<Canvas>(true);
            if (canvas != null) refPPU = canvas.rootCanvas.referencePixelsPerUnit;

            foreach (Image img in root.GetComponentsInChildren<Image>(true))
            {
                StyleImage(img, refPPU);
            }

            foreach (Button btn in root.GetComponentsInChildren<Button>(true))
            {
                StyleButton(btn, uppercaseButtons);
            }

            TMP_FontAsset font = GetThemeFont();
            foreach (TMP_Text txt in root.GetComponentsInChildren<TMP_Text>(true))
            {
                StyleText(txt, font);
            }

            OrbitVisualizer2D orbitVis = root.GetComponentInChildren<OrbitVisualizer2D>(true);
            if (orbitVis != null && orbitVis.GetComponent<OrbitMissionTimeTracker>() == null)
            {
                orbitVis.gameObject.AddComponent<OrbitMissionTimeTracker>();
            }
        }

        // =========================================================================
        // IMAGE STYLING (by GameObject name from CockpitUIAutoBuilder)
        // =========================================================================
        private static void StyleImage(Image img, float refPPU)
        {
            if (img == null) return;
            string n = img.gameObject.name;
            if (n.StartsWith("Theme_")) return;              // our own decorations
            if (img.type == Image.Type.Filled) return;        // resource meter fill bars

            switch (n)
            {
                case "CockpitBackground":
                    img.sprite = _bgGradient;
                    img.type = Image.Type.Simple;
                    img.color = Color.white;
                    AddBackgroundDecor(img.rectTransform);
                    return;

                case "Earth2DBody":
                    MakePlanet(img, true);
                    return;

                case "PlanetBackdrop2D":
                    MakePlanet(img, false);
                    return;

                case "EclipseShadowZone":
                    img.sprite = _fadeRight;
                    img.type = Image.Type.Simple;
                    return;

                case "CML4_DilemmaModal_Overlay":
                    return;

                case "Orbit2DArena":
                case "Satellite2DVisualStage":
                    ApplyRounded(img, 16f, refPPU);
                    img.color = ArenaColor;
                    AddStars(img.rectTransform, 4f, 0.85f);
                    AddBorder(img, refPPU);
                    return;

                case "CubeSatHull2D":
                    img.sprite = _sfsHull;
                    img.type = Image.Type.Simple;
                    return;

                case "OrbitingCubeSat2D":
                    img.sprite = _sfsHull;
                    img.type = Image.Type.Simple;
                    img.color = Color.white;
                    AddOrbitCubeSatDetails(img.rectTransform);
                    return;

                case "LeftSolarWing2D":
                case "RightSolarWing2D":
                    img.sprite = _sfsSolar;
                    img.type = Image.Type.Simple;
                    return;

                case "DeployedSolarWings":
                    img.sprite = _sfsSolar;
                    img.type = Image.Type.Simple;
                    img.color = new Color(0.18f, 0.42f, 0.78f, 1f);
                    img.rectTransform.sizeDelta = new Vector2(16f, 96f);
                    return;

                case "PayloadCameraLens2D":
                    img.sprite = _sfsLens;
                    img.type = Image.Type.Simple;
                    return;

                case "GroundDish2D":
                    img.sprite = _sfsDish;
                    img.type = Image.Type.Simple;
                    return;
            }

            float radius = 6f;
            bool border = false;

            if (MainPanels.Contains(n))
            {
                img.color = PanelColor;
                radius = 8f;
                border = (n != "TopScreenSwitcherBar");
            }
            else if (InsetPanels.Contains(n))
            {
                img.color = InsetColor;
                radius = 6f;
                border = false;
            }
            else if (n.StartsWith("Meter_"))
            {
                img.color = MeterColor;
                radius = 6f;
            }
            else if (n == "BarBackground")
            {
                img.color = BarBgColor;
                radius = 4f;
            }
            else if (n.StartsWith("PartCardTemplate"))
            {
                radius = 6f;
                border = false;
            }
            else if (n == "DilemmaCardBox") radius = 8f;
            else if (n == "FinalGradeBadgeBackground") radius = 8f;
            else if (n == "GroundSignalLinkBeam") radius = 24f;
            else if (n == "OrbitingCubeSat2D" || n == "TopAntenna2D") radius = 4f;
            else if (n == "DeployedSolarWings" || n.Contains("SolarWing")) radius = 3f;
            else if (n == "CubeSatHull2D") radius = 6f;
            else if (n == "GroundDish2D" || n == "PayloadCameraLens2D") radius = 6f;
            else if (img.GetComponent<Button>() != null) radius = 6f;

            ApplyRounded(img, radius, refPPU);
            if (border) AddBorder(img, refPPU);
        }

        private static void ApplyRounded(Image img, float desiredRadius, float refPPU)
        {
            img.sprite = _rounded;
            img.type = Image.Type.Sliced;
            img.fillCenter = true;
            img.pixelsPerUnitMultiplier = CalcMultiplier(img.rectTransform, desiredRadius, refPPU);
        }

        private static float CalcMultiplier(RectTransform rt, float desiredRadius, float refPPU)
        {
            Rect r = rt.rect;
            float minDim = Mathf.Min(Mathf.Abs(r.width), Mathf.Abs(r.height));
            float radius = desiredRadius;
            if (minDim > 2f) radius = Mathf.Min(desiredRadius, minDim * 0.5f);
            radius = Mathf.Max(1f, radius);
            // displayed corner = RoundTexRadius * refPPU / (100 * multiplier)
            return Mathf.Max(0.01f, RoundTexRadius * refPPU / (100f * radius));
        }

        private static void AddBorder(Image owner, float refPPU)
        {
            Transform existing = owner.transform.Find("Theme_Border");
            Image border = existing != null ? existing.GetComponent<Image>() : CreateChildImage(owner.transform, "Theme_Border");
            StretchFull(border.rectTransform, 0f);
            border.sprite = _roundedBorder;
            border.type = Image.Type.Sliced;
            border.pixelsPerUnitMultiplier = owner.pixelsPerUnitMultiplier;
            border.color = BorderColor;
            border.transform.SetAsFirstSibling(); // behind the panel's content
        }

        private static void AddStars(RectTransform owner, float inset, float alpha)
        {
            Transform existing = owner.Find("Theme_Stars");
            Image stars = existing != null ? existing.GetComponent<Image>() : CreateChildImage(owner, "Theme_Stars");
            StretchFull(stars.rectTransform, inset);
            stars.sprite = _stars;
            stars.type = Image.Type.Tiled;
            stars.color = new Color(1f, 1f, 1f, alpha);
            stars.transform.SetAsFirstSibling();
        }

        private static void AddShadow(GameObject go, Vector2 distance, float alpha)
        {
            if (go.GetComponent<Shadow>() != null) return;
            Shadow s = go.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, alpha);
            s.effectDistance = distance;
        }

        private static void AddOrbitCubeSatDetails(RectTransform satRt)
        {
            if (satRt.Find("Theme_OrbitHullOverlay") != null) return;

            // Cover the center seam of DeployedSolarWings with the silver SFS CubeSat hull + camera lens
            Image hull = CreateChildImage(satRt, "Theme_OrbitHullOverlay");
            StretchFull(hull.rectTransform, 0f);
            hull.sprite = _sfsHull;
            hull.color = Color.white;

            Image lens = CreateChildImage(satRt, "Theme_OrbitLens");
            lens.rectTransform.sizeDelta = new Vector2(14f, 14f);
            lens.rectTransform.anchoredPosition = Vector2.zero;
            lens.sprite = _sfsLens;
            lens.color = new Color(0.20f, 0.78f, 0.96f, 1f);
        }

        // =========================================================================
        // BACKGROUND: gradient + starfield + nebula + giant planet horizon
        // =========================================================================
        private static void AddBackgroundDecor(RectTransform bg)
        {
            if (bg.Find("Theme_Horizon") != null) return;

            AddStars(bg, 0f, 0.85f);

            const float horizonSize = 3600f;
            Image horizonGlow = CreateChildImage(bg, "Theme_HorizonGlow");
            SetCentered(horizonGlow.rectTransform, new Vector2(0.35f, 0f), new Vector2(horizonSize / 0.7f, horizonSize / 0.7f),
                new Vector2(0f, -horizonSize * 0.5f + 165f));
            horizonGlow.sprite = _glow;
            horizonGlow.color = new Color(NasaBlueBright.r, NasaBlueBright.g, NasaBlueBright.b, 0.24f);

            Image horizon = CreateChildImage(bg, "Theme_Horizon");
            SetCentered(horizon.rectTransform, new Vector2(0.35f, 0f), new Vector2(horizonSize, horizonSize),
                new Vector2(0f, -horizonSize * 0.5f + 165f));
            horizon.sprite = _planet;
            horizon.color = NasaBlue;
        }

        // =========================================================================
        // PLANETS: round + atmosphere glow + night side + dashed orbit path
        // =========================================================================
        private static void MakePlanet(Image img, bool isOrbitArena)
        {
            img.sprite = _planet;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;

            RectTransform rt = img.rectTransform;
            float d = Mathf.Max(rt.sizeDelta.x, rt.sizeDelta.y);

            // Builder backdrop planet was 40% transparent -> make it solid so it looks like a real planet
            if (!isOrbitArena && img.GetComponent<ThemeAlphaOverride>() == null)
            {
                img.gameObject.AddComponent<ThemeAlphaOverride>().alpha = 1f;
            }

            Image glow = CreateSibling(rt, "Theme_PlanetGlow", _glow, d / 0.7f, false);
            ThemeColorFollower follower = glow.GetComponent<ThemeColorFollower>();
            if (follower == null) follower = glow.gameObject.AddComponent<ThemeColorFollower>();
            follower.source = img;
            follower.alpha = 0.45f;
            follower.brighten = 0.25f;

            if (isOrbitArena)
            {
                float orbitRadius = 148f;
                OrbitVisualizer2D vis = img.GetComponentInParent<OrbitVisualizer2D>(true);
                if (vis != null)
                {
                    FieldInfo f = typeof(OrbitVisualizer2D).GetField("orbitRadiusPixels", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (f != null) orbitRadius = (float)f.GetValue(vis);
                }

                Image ring = CreateSibling(rt, "Theme_OrbitPath", _orbitRing, orbitRadius * 2f * (256f / 254f), false);
                ring.color = new Color(0.75f, 0.88f, 1f, 0.45f);

                Image night = CreateSibling(rt, "Theme_NightSide", _nightSide, d, true);
                night.color = new Color(0f, 0.01f, 0.04f, 0.68f);

                Image sunlitRim = CreateSibling(rt, "Theme_SunlitRim", _sunlitRim, d * 1.04f, true);
                sunlitRim.color = new Color(1f, 0.96f, 0.86f, 0.55f);

                // Distant Sun & warm solar corona on the right side of Orbit2DArena
                Transform arena = rt.parent;
                if (arena != null && arena.Find("Theme_SunDisc") == null)
                {
                    Image sunCorona = CreateChildImage(arena, "Theme_SunCorona");
                    sunCorona.sprite = _glow;
                    sunCorona.color = new Color(1f, 0.82f, 0.42f, 0.32f);
                    SetCentered(sunCorona.rectTransform, new Vector2(0.96f, 0.50f), new Vector2(210f, 210f), Vector2.zero);
                    sunCorona.transform.SetAsFirstSibling();

                    Image sunDisc = CreateChildImage(arena, "Theme_SunDisc");
                    sunDisc.sprite = _planet;
                    sunDisc.color = new Color(1f, 0.95f, 0.80f, 1f);
                    SetCentered(sunDisc.rectTransform, new Vector2(0.96f, 0.50f), new Vector2(64f, 64f), Vector2.zero);
                    sunDisc.transform.SetSiblingIndex(sunCorona.transform.GetSiblingIndex() + 1);
                }
            }
        }

        private static Image CreateSibling(RectTransform target, string name, Sprite sprite, float size, bool after)
        {
            Transform parent = target.parent;
            Transform existing = parent.Find(name);
            Image img = existing != null ? existing.GetComponent<Image>() : CreateChildImage(parent, name);

            RectTransform rt = img.rectTransform;
            rt.anchorMin = target.anchorMin;
            rt.anchorMax = target.anchorMax;
            rt.pivot = target.pivot;
            rt.anchoredPosition = target.anchoredPosition;
            rt.sizeDelta = new Vector2(size, size);
            img.sprite = sprite;
            img.preserveAspect = true;

            if (existing == null)
            {
                int idx = target.GetSiblingIndex();
                rt.SetSiblingIndex(after ? idx + 1 : idx);
            }
            return img;
        }

        // =========================================================================
        // BUTTONS & TEXT
        // =========================================================================
        private static void StyleButton(Button btn, bool uppercase)
        {
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.90f, 0.93f, 0.98f, 1f);
            cb.pressedColor = new Color(0.72f, 0.76f, 0.84f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.50f, 0.52f, 0.58f, 0.6f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.06f;
            btn.colors = cb;
            btn.transition = Selectable.Transition.ColorTint;

            if (btn.GetComponent<ThemeButtonHover>() == null)
            {
                btn.gameObject.AddComponent<ThemeButtonHover>();
            }

            if (uppercase)
            {
                foreach (TMP_Text t in btn.GetComponentsInChildren<TMP_Text>(true))
                {
                    t.fontStyle |= FontStyles.UpperCase;
                }
            }
        }

        private static void StyleText(TMP_Text txt, TMP_FontAsset font)
        {
            if (txt == null) return;
            if (font != null && txt.font != font)
            {
                txt.font = font;
            }
            if ((txt.fontStyle & FontStyles.Bold) != 0)
            {
                txt.characterSpacing = Mathf.Max(txt.characterSpacing, 1.5f);
            }
        }

        /// <summary>
        /// Uses TextMeshPro's clean built-in SDF font so no OS font import warnings appear.
        /// </summary>
        private static TMP_FontAsset GetThemeFont()
        {
            return null;
        }

        // =========================================================================
        // PROCEDURAL SPRITE GENERATION (drawn pixel-by-pixel in code)
        // =========================================================================
        private static void EnsureSprites()
        {
            if (_rounded != null) return;

            _rounded = MakeRoundedSprite(false);
            _roundedBorder = MakeRoundedSprite(true);
            _planet = MakePlanetSprite(256);
            _glow = MakeGlowSprite(256);
            _orbitRing = MakeOrbitRingSprite(512);
            _stars = MakeStarsSprite(512);
            _bgGradient = MakeBackgroundGradient();
            _fadeRight = MakeFadeRightSprite();
            _nightSide = MakeNightSideSprite(256);
            _sfsHull = MakeSfsCubeSatHullSprite(128, 192);
            _sfsSolar = MakeSfsSolarSprite(128, 64);
            _sfsLens = MakeSfsLensSprite(96);
            _sfsDish = MakeSfsDishSprite(128, 64);
            _sunlitRim = MakeSunlitRimSprite(256);
        }

        private static Texture2D NewTex(int w, int h, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = wrap;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }

        private static Sprite ToSprite(Texture2D tex, Vector4 border)
        {
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
        }

        private static float RoundedAlpha(float px, float py, float size, float r)
        {
            float cx = Mathf.Clamp(px, r, size - r);
            float cy = Mathf.Clamp(py, r, size - r);
            float d = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
            return Mathf.Clamp01(r - d + 0.5f);
        }

        private static Sprite MakeRoundedSprite(bool borderOnly)
        {
            int s = RoundTexSize;
            Texture2D tex = NewTex(s, s);
            Color32[] px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float a = RoundedAlpha(fx, fy, s, RoundTexRadius);
                    if (borderOnly)
                    {
                        float t = BorderThickness;
                        float inner = RoundedAlpha(fx - t, fy - t, s - 2f * t, RoundTexRadius - t);
                        if (fx < t || fy < t || fx > s - t || fy > s - t) inner = 0f;
                        a *= 1f - inner;
                    }
                    px[y * s + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels32(px);
            float b = RoundTexRadius;
            return ToSprite(tex, new Vector4(b, b, b, b));
        }

        private static Sprite MakePlanetSprite(int s)
        {
            Texture2D tex = NewTex(s, s);
            Color32[] px = new Color32[s * s];
            float r = s * 0.5f - 1f;
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    float n = d / r;
                    float shade = Mathf.Lerp(1f, 0.72f, n * n * n); // soft limb darkening
                    px[y * s + x] = new Color(shade, shade, shade, a);
                }
            }
            tex.SetPixels32(px);
            return ToSprite(tex, Vector4.zero);
        }

        private static Sprite MakeGlowSprite(int s)
        {
            Texture2D tex = NewTex(s, s);
            Color32[] px = new Color32[s * s];
            float r = s * 0.5f;
            Vector2 c = new Vector2(r, r);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float n = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / r;
                    float a = n < 0.7f ? 1f : Mathf.Pow(Mathf.Clamp01(1f - (n - 0.7f) / 0.3f), 2.2f);
                    px[y * s + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels32(px);
            return ToSprite(tex, Vector4.zero);
        }

        private static Sprite MakeOrbitRingSprite(int s)
        {
            Texture2D tex = NewTex(s, s);
            Color32[] px = new Color32[s * s];
            float c = s * 0.5f;
            float ringR = c - 2f;
            const float thickness = 2.4f;
            const int dashes = 120;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = x + 0.5f - c, dy = y + 0.5f - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(thickness * 0.5f - Mathf.Abs(d - ringR) + 0.5f);
                    float ang = Mathf.Atan2(dy, dx) + Mathf.PI;
                    int seg = (int)(ang / (2f * Mathf.PI) * dashes);
                    if (seg % 2 == 1) a = 0f;
                    px[y * s + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels32(px);
            return ToSprite(tex, Vector4.zero);
        }

        private static Sprite MakeStarsSprite(int s)
        {
            Texture2D tex = NewTex(s, s, TextureWrapMode.Repeat);
            Color[] px = new Color[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(1f, 1f, 1f, 0f);

            System.Random rng = new System.Random(1337);
            for (int i = 0; i < 260; i++)
            {
                float sx = (float)rng.NextDouble() * s;
                float sy = (float)rng.NextDouble() * s;
                double roll = rng.NextDouble();
                float size = roll < 0.80 ? 0.7f : (roll < 0.98 ? 1.3f : 2.2f);
                float bright = 0.35f + (float)rng.NextDouble() * 0.65f;
                Color tint = rng.NextDouble() < 0.2 ? new Color(1f, 0.92f, 0.80f) : new Color(0.85f, 0.92f, 1f);

                int rad = Mathf.CeilToInt(size * 2.5f);
                for (int oy = -rad; oy <= rad; oy++)
                {
                    for (int ox = -rad; ox <= rad; ox++)
                    {
                        int px0 = ((int)sx + ox + s) % s;
                        int py0 = ((int)sy + oy + s) % s;
                        float dx = ((int)sx + ox + 0.5f) - sx;
                        float dy = ((int)sy + oy + 0.5f) - sy;
                        float g = Mathf.Exp(-(dx * dx + dy * dy) / (2f * size * size)) * bright;
                        int idx = py0 * s + px0;
                        if (g > px[idx].a) px[idx] = new Color(tint.r, tint.g, tint.b, g);
                    }
                }
            }
            tex.SetPixels(px);
            return ToSprite(tex, Vector4.zero);
        }

        private static Sprite MakeBackgroundGradient()
        {
            const int w = 8, h = 256;
            Texture2D tex = NewTex(w, h);
            Color top = new Color(0.025f, 0.030f, 0.048f, 1f);    // Pure deep space charcoal
            Color bottom = new Color(0.045f, 0.065f, 0.115f, 1f); // Subtle NASA horizon tint
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                Color c = Color.Lerp(bottom, top, Mathf.SmoothStep(0f, 1f, y / (float)(h - 1)));
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }
            tex.SetPixels(px);
            return ToSprite(tex, Vector4.zero);
        }

        private static Sprite MakeFadeRightSprite()
        {
            const int w = 256, h = 4;
            Texture2D tex = NewTex(w, h);
            Color[] px = new Color[w * h];
            for (int x = 0; x < w; x++)
            {
                float a = 1f - Mathf.SmoothStep(0f, 1f, x / (float)(w - 1));
                for (int y = 0; y < h; y++) px[y * w + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            return ToSprite(tex, Vector4.zero);
        }

        private static Sprite MakeNightSideSprite(int s)
        {
            Texture2D tex = NewTex(s, s);
            Color32[] px = new Color32[s * s];
            float r = s * 0.5f - 1f;
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    float disc = Mathf.Clamp01(r - d + 0.5f);
                    float nx = (x + 0.5f - c.x) / r; // -1 (left, away from Sun) .. 1 (right, facing Sun)
                    float shadow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((0.15f - nx) / 0.6f));
                    px[y * s + x] = new Color(1f, 1f, 1f, disc * shadow);
                }
            }
            tex.SetPixels32(px);
            return ToSprite(tex, Vector4.zero);
        }

        private static Sprite MakeSunlitRimSprite(int s)
        {
            Texture2D tex = NewTex(s, s);
            Color32[] px = new Color32[s * s];
            float r = s * 0.5f - 2f;
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / r;
                    if (d > 1f)
                    {
                        px[y * s + x] = Color.clear;
                        continue;
                    }
                    float nx = (x + 0.5f - c.x) / r; // +1 on right side (facing Sun)
                    float sunSide = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((nx + 0.05f) / 0.95f));
                    float rim = Mathf.Pow(Mathf.Clamp01(d), 3.2f);
                    float dayGlow = sunSide * (0.28f + 0.72f * rim);
                    px[y * s + x] = new Color(1f, 0.96f, 0.86f, dayGlow);
                }
            }
            tex.SetPixels32(px);
            return ToSprite(tex, Vector4.zero);
        }

        private static Sprite MakeSfsCubeSatHullSprite(int w, int h)
        {
            Texture2D tex = NewTex(w, h);
            Color outline = new Color(0.08f, 0.10f, 0.13f, 1f);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                bool seam = (y % 48 <= 2);
                bool rivetY = (y % 24 >= 10 && y % 24 <= 13);
                for (int x = 0; x < w; x++)
                {
                    if (x < 4 || x >= w - 4 || y < 4 || y >= h - 4)
                    {
                        px[y * w + x] = outline;
                        continue;
                    }
                    bool rail = (x < 16 || x >= w - 16);
                    bool rivetX = (x >= 8 && x <= 11) || (x >= w - 12 && x <= w - 9);
                    if (rail && rivetX && rivetY)
                    {
                        px[y * w + x] = new Color(0.32f, 0.35f, 0.40f, 1f);
                        continue;
                    }
                    if (x == 16 || x == w - 17)
                    {
                        px[y * w + x] = outline;
                        continue;
                    }

                    float u = x / (float)(w - 1);
                    float light = rail ? 0.78f : (u < 0.35f ? 1.0f : (u < 0.72f ? 0.90f : 0.72f));
                    if (seam && !rail) light *= 0.76f;
                    px[y * w + x] = new Color(light, light, light, 1f);
                }
            }
            tex.SetPixels(px);
            return ToSprite(tex, Vector4.zero);
        }

        private static Sprite MakeSfsSolarSprite(int w, int h)
        {
            Texture2D tex = NewTex(w, h);
            Color outline = new Color(0.08f, 0.10f, 0.13f, 1f);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x < 3 || x >= w - 3 || y < 3 || y >= h - 3)
                    {
                        px[y * w + x] = outline;
                    }
                    else if (x % 20 <= 1 || y % 16 <= 1)
                    {
                        px[y * w + x] = new Color(0.78f, 0.85f, 0.94f, 1f); // crisp solar cell grid
                    }
                    else
                    {
                        px[y * w + x] = Color.white; // multiplied by BuilderUI's blue/gold wing color!
                    }
                }
            }
            tex.SetPixels(px);
            return ToSprite(tex, Vector4.zero);
        }

        private static Sprite MakeSfsLensSprite(int s)
        {
            Texture2D tex = NewTex(s, s);
            Color outline = new Color(0.08f, 0.10f, 0.13f, 1f);
            Color[] px = new Color[s * s];
            float c = s * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    if (x < 4 || x >= s - 4 || y < 4 || y >= s - 4)
                    {
                        px[y * s + x] = outline;
                        continue;
                    }
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
                    if (d < 0.62f)
                    {
                        if (d > 0.50f) px[y * s + x] = outline;
                        else px[y * s + x] = Color.white; // tinted by BuilderUI payload color!
                    }
                    else
                    {
                        px[y * s + x] = new Color(0.32f, 0.36f, 0.42f, 1f); // dark metal camera bezel
                    }
                }
            }
            tex.SetPixels(px);
            return ToSprite(tex, Vector4.zero);
        }

        private static Sprite MakeSfsDishSprite(int w, int h)
        {
            Texture2D tex = NewTex(w, h);
            Color outline = new Color(0.08f, 0.10f, 0.13f, 1f);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x < 3 || x >= w - 3 || y < 3 || y >= h - 3)
                    {
                        px[y * w + x] = outline;
                    }
                    else
                    {
                        bool grid = (y == 10 || y == h - 11);
                        px[y * w + x] = grid ? new Color(0.65f, 0.70f, 0.76f, 1f) : new Color(0.90f, 0.94f, 0.98f, 1f);
                    }
                }
            }
            tex.SetPixels(px);
            return ToSprite(tex, Vector4.zero);
        }

        // =========================================================================
        // SMALL HELPERS
        // =========================================================================
        private static Image CreateChildImage(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        private static void StretchFull(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static void SetCentered(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 offset)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
        }
    }

    /// <summary>Smoothly grows a button on hover and shrinks it on press (juicy game feel).</summary>
    public class ThemeButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Vector3 _baseScale = Vector3.one;
        private float _target = 1f;
        private bool _hovered;

        private void Awake() { _baseScale = transform.localScale; }

        private void OnDisable()
        {
            _target = 1f;
            _hovered = false;
            transform.localScale = _baseScale;
        }

        public void OnPointerEnter(PointerEventData e) { _hovered = true; _target = 1.03f; }
        public void OnPointerExit(PointerEventData e) { _hovered = false; _target = 1f; }
        public void OnPointerDown(PointerEventData e)
        {
            _target = 0.97f;
            if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
            {
                if (gameObject.name.Contains("SelectPartButton"))
                    SpaceMissionGame.Audio.SpaceSoundFX.Instance.PlayEquip();
                else
                    SpaceMissionGame.Audio.SpaceSoundFX.Instance.PlayClick();
            }
        }
        public void OnPointerUp(PointerEventData e) { _target = _hovered ? 1.03f : 1f; }

        private void Update()
        {
            Vector3 goal = _baseScale * _target;
            if ((transform.localScale - goal).sqrMagnitude < 0.000001f) return;
            float k = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, goal, k);
        }
    }

    /// <summary>Makes a glow follow its planet's color (planet color changes per orbit: ISS / SSO / Moon).</summary>
    public class ThemeColorFollower : MonoBehaviour
    {
        public Graphic source;
        public float alpha = 0.45f;
        public float brighten = 0.2f;
        private Graphic _self;

        private void Awake() { _self = GetComponent<Graphic>(); }

        private void LateUpdate()
        {
            if (source == null || _self == null) return;
            Color c = Color.Lerp(source.color, Color.white, brighten);
            c.a = alpha;
            if (_self.color != c) _self.color = c;
        }
    }

    /// <summary>Forces a Graphic's alpha (e.g. make the Builder's 40%-transparent planet fully solid).</summary>
    public class ThemeAlphaOverride : MonoBehaviour
    {
        public float alpha = 1f;
        private Graphic _self;

        private void Awake() { _self = GetComponent<Graphic>(); }

        private void LateUpdate()
        {
            if (_self == null) return;
            Color c = _self.color;
            if (!Mathf.Approximately(c.a, alpha))
            {
                c.a = alpha;
                _self.color = c;
            }
        }
    }

    /// <summary>
    /// Tracks real GMAT-scaled Mission Elapsed Time (Days, Hours, Orbits) on Screen 2
    /// and provides a 1x / 2x / 8x / 16x / 32x Time Warp selector without modifying Member 2's scripts.
    /// </summary>
    public class OrbitMissionTimeTracker : MonoBehaviour
    {
        private static readonly float[] WarpOptions = { 1f, 2f, 8f, 16f, 32f, 64f, 128f };

        private OrbitVisualizer2D _vis;
        private BuilderUI _builderUI;
        private TMP_Text _batteryText;
        private FieldInfo _isOrbitingField;
        private FieldInfo _speedField;
        private FieldInfo _planetSpeedField;
        private float _orbitsCompleted;
        private float _currentWarp = 1f;
        private readonly List<Button> _warpButtons = new List<Button>();

        private void Awake()
        {
            _vis = GetComponent<OrbitVisualizer2D>();
            _builderUI = FindAnyObjectByType<BuilderUI>();
            _isOrbitingField = typeof(OrbitVisualizer2D).GetField("_isOrbitingActive", BindingFlags.NonPublic | BindingFlags.Instance);
            _speedField = typeof(OrbitVisualizer2D).GetField("satelliteOrbitSpeedDegPerSec", BindingFlags.NonPublic | BindingFlags.Instance);
            _planetSpeedField = typeof(OrbitVisualizer2D).GetField("planetRotationSpeedDegPerSec", BindingFlags.NonPublic | BindingFlags.Instance);

            Transform bat = transform.Find("BatteryChargeText");
            if (bat != null) _batteryText = bat.GetComponent<TMP_Text>();

            BuildTimeWarpBar();
        }

        private void OnDisable()
        {
            // Never leave Time.timeScale accelerated when leaving Screen 2
            Time.timeScale = 1f;
            ApplyExtraSpeedMultiplier(1f);
        }

        private void OnEnable()
        {
            ApplyWarpValues();
        }

        public void ResetTimer()
        {
            _orbitsCompleted = 0f;
            SetWarp(1f);
        }

        private void SetWarp(float warp)
        {
            _currentWarp = warp;
            if (gameObject.activeInHierarchy)
            {
                ApplyWarpValues();
            }
            UpdateWarpButtonColors();
        }

        private void ApplyWarpValues()
        {
            // Unity's Time.timeScale max is 100f; above 64x we keep timeScale at 64f and scale orbit speed!
            float timeScale = Mathf.Min(_currentWarp, 64f);
            float extraMult = _currentWarp > 64f ? (_currentWarp / 64f) : 1f;
            Time.timeScale = timeScale;
            ApplyExtraSpeedMultiplier(extraMult);
        }

        private void ApplyExtraSpeedMultiplier(float mult)
        {
            if (_speedField != null && _vis != null) _speedField.SetValue(_vis, 52f * mult);
            if (_planetSpeedField != null && _vis != null) _planetSpeedField.SetValue(_vis, 14f * mult);
        }

        private void BuildTimeWarpBar()
        {
            Transform arena = transform.Find("Orbit2DArena");
            if (arena == null || arena.Find("Theme_TimeWarpBar") != null) return;

            GameObject bar = new GameObject("Theme_TimeWarpBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(arena, false);
            RectTransform barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(1f, 1f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(1f, 1f);
            barRt.sizeDelta = new Vector2(390f, 36f);
            barRt.anchoredPosition = new Vector2(-10f, -10f);
            bar.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.14f, 0.85f);

            for (int i = 0; i < WarpOptions.Length; i++)
            {
                float w = WarpOptions[i];
                GameObject btnGo = new GameObject($"Warp_{w}x", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(barRt, false);
                RectTransform bRt = btnGo.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0f, 0.5f);
                bRt.anchorMax = new Vector2(0f, 0.5f);
                bRt.pivot = new Vector2(0f, 0.5f);
                bRt.sizeDelta = new Vector2(50f, 26f);
                bRt.anchoredPosition = new Vector2(6f + i * 54f, 0f);

                Button btn = btnGo.GetComponent<Button>();
                btn.onClick.AddListener(() => SetWarp(w));
                _warpButtons.Add(btn);

                GameObject txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(bRt, false);
                RectTransform tRt = txtGo.GetComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.offsetMin = Vector2.zero;
                tRt.offsetMax = Vector2.zero;

                TMP_Text t = txtGo.GetComponent<TextMeshProUGUI>();
                t.text = $"{w:0}x";
                t.fontSize = 12;
                t.fontStyle = FontStyles.Bold;
                t.color = Color.white;
                t.alignment = TextAlignmentOptions.Center;
            }

            UpdateWarpButtonColors();
        }

        private void UpdateWarpButtonColors()
        {
            Color activeCol = SpaceUITheme.NasaBlueBright;
            Color idleCol = new Color(0.10f, 0.12f, 0.17f, 1f);

            for (int i = 0; i < _warpButtons.Count && i < WarpOptions.Length; i++)
            {
                Image img = _warpButtons[i].GetComponent<Image>();
                if (img != null)
                {
                    img.color = Mathf.Approximately(_currentWarp, WarpOptions[i]) ? activeCol : idleCol;
                }
            }
        }

        private void LateUpdate()
        {
            if (_vis == null || _batteryText == null || _isOrbitingField == null) return;

            bool orbiting = (bool)_isOrbitingField.GetValue(_vis);
            if (orbiting)
            {
                float degPerSec = _speedField != null ? (float)_speedField.GetValue(_vis) : 52f;
                _orbitsCompleted += (degPerSec * Time.deltaTime) / 360f;
            }

            // Use exact GMAT orbital period (ISS 92.5m, SSO 96.7m, Lunar 118.0m)
            float orbitPeriodMin = (_builderUI != null && _builderUI.CurrentDesign != null)
                ? MissionCalculator.GetOrbitPeriodMinutes(_builderUI.CurrentDesign.SelectedOrbit)
                : 95f;

            float totalMinutes = _orbitsCompleted * orbitPeriodMin;
            int days = Mathf.FloorToInt(totalMinutes / 1440f);
            int hours = Mathf.FloorToInt((totalMinutes % 1440f) / 60f);
            int orbitNum = Mathf.FloorToInt(_orbitsCompleted) + 1;

            string baseText = _batteryText.text;
            int pipeIdx = baseText.IndexOf("  |  Time in Space:", StringComparison.Ordinal);
            if (pipeIdx >= 0) baseText = baseText.Substring(0, pipeIdx);

            _batteryText.text = $"{baseText}  |  Time in Space: Day {days} ({hours:00}h)  •  Orbit #{orbitNum}  [{_currentWarp:0}x]";
        }
    }
}
