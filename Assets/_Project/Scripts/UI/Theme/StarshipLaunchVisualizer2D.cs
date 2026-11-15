using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceMissionGame.Core;

namespace SpaceMissionGame.UI.Theme
{
    /// <summary>
    /// [Member 3 - HanMing] Realistic 2D SpaceX Starship + Super Heavy Lunar Launch Sequence.
    ///
    /// Styled in crisp 2D Spaceflight Simulator (SFS) vector art (clean dark outlines,
    /// metallic cylindrical shading, forward/aft aero flaps, Raptor engine bells, chines,
    /// hot-stage separation sparks/smoke rings, clamshell fairing separation, and CubeSat deployment).
    /// </summary>
    public class StarshipLaunchVisualizer2D : MonoBehaviour
    {
        public static StarshipLaunchVisualizer2D Instance { get; private set; }

        private RectTransform _stageRoot;
        private Image _skyBackground;
        private Image _starfieldOverlay;
        private RectTransform _groundAndPadGroup;
        private RectTransform _earthHorizonCurve;
        private RectTransform _towerGroup;
        private RectTransform _rocketAssembly;
        private RectTransform _superHeavyBooster;
        private RectTransform _starshipUpperStage;
        private RectTransform _fairingLeft;
        private RectTransform _fairingRight;
        private RectTransform _boosterPlume;
        private RectTransform _starshipPlume;
        private RectTransform _rcsPuffLeft;
        private RectTransform _rcsPuffRight;
        private RectTransform _hotStageFlash;
        private RectTransform _cubesatPayload;
        private RectTransform _cubesatWingLeft;
        private RectTransform _cubesatWingRight;
        private RectTransform _payloadAdapter;
        private RectTransform _maxQVaporCone;
        private RectTransform _moonDisc;
        private RectTransform _tankerShip;

        private TMP_Text _telemetryPhaseText;
        private TMP_Text _telemetryStatsText;
        private Button _skipButton;
        private CanvasGroup _canvasGroup;

        private readonly List<RectTransform> _smokeParticles = new List<RectTransform>();
        private readonly List<RectTransform> _sepRings = new List<RectTransform>();
        private Coroutine _launchCoroutine;
        private Action _onSequenceFinished;

        // SFS-style procedural sprites
        private static Sprite _boosterSprite;
        private static Sprite _starshipBodySprite;
        private static Sprite _fairingLeftSprite;
        private static Sprite _fairingRightSprite;
        private static Sprite _flapLeftSprite;
        private static Sprite _flapRightSprite;
        private static Sprite _nozzleRowSprite;
        private static Sprite _adapterSprite;
        private static Sprite _cubesatBusSprite;
        private static Sprite _solarWingSprite;
        private static Sprite _plumeSprite;
        private static Sprite _smokeSprite;
        private static Sprite _ringVfxSprite;
        private static Sprite _skyGradientSprite;
        private static Sprite _starSprite;
        private static Sprite _horizonSprite;
        private static Sprite _vaporConeSprite;
        private static Sprite _moonSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            CockpitUIAutoBuilder cockpit = FindAnyObjectByType<CockpitUIAutoBuilder>();
            if (cockpit == null || cockpit.simulationPhaseRoot == null) return;

            if (cockpit.simulationPhaseRoot.GetComponent<StarshipLaunchVisualizer2D>() == null)
            {
                cockpit.simulationPhaseRoot.AddComponent<StarshipLaunchVisualizer2D>();
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        public void PlayLaunchSequence(SpacecraftDesign design, MissionEvaluationResult eval, Action onComplete)
        {
            Time.timeScale = 1f;
            _onSequenceFinished = onComplete;
            EnsureUIBuilt();

            if (_launchCoroutine != null) StopCoroutine(_launchCoroutine);
            _stageRoot.gameObject.SetActive(true);
            _stageRoot.SetAsLastSibling();
            _canvasGroup.alpha = 1f;
            _rocketAssembly.gameObject.SetActive(true);

            _launchCoroutine = StartCoroutine(RunStarshipLaunchRoutine(design, eval));
        }

        /// <summary>
        /// Displays the clean empty launchpad & Mechazilla tower when Screen 2 is opened
        /// directly without triggering a rocket launch.
        /// </summary>
        public void ShowEmptyLaunchPad(Action onBackToBuilder)
        {
            _onSequenceFinished = null;
            EnsureUIBuilt();

            if (_launchCoroutine != null)
            {
                StopCoroutine(_launchCoroutine);
                _launchCoroutine = null;
            }

            _stageRoot.gameObject.SetActive(true);
            _stageRoot.SetAsLastSibling();
            _canvasGroup.alpha = 1f;

            ResetSceneTransforms(null);
            _rocketAssembly.gameObject.SetActive(false);

            _telemetryPhaseText.text = "LAUNCH PAD CLEAR — NO ACTIVE ROCKET LAUNCH";
            _telemetryStatsText.text = "Design your CubeSat in Builder & press LAUNCH to roll out Starship + Super Heavy";

            if (_skipButton != null)
            {
                TMP_Text btnTxt = _skipButton.GetComponentInChildren<TMP_Text>();
                if (btnTxt != null) btnTxt.text = "RETURN TO BUILDER";
                _skipButton.onClick.RemoveAllListeners();
                _skipButton.onClick.AddListener(() =>
                {
                    _stageRoot.gameObject.SetActive(false);
                    onBackToBuilder?.Invoke();
                });
            }
        }

        public void SkipLaunch()
        {
            if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
                SpaceMissionGame.Audio.SpaceSoundFX.Instance.StopEngineRumble();
            if (_launchCoroutine != null)
            {
                StopCoroutine(_launchCoroutine);
                _launchCoroutine = null;
            }
            if (_stageRoot != null) _stageRoot.gameObject.SetActive(false);
            _onSequenceFinished?.Invoke();
        }

        public void CancelLaunch()
        {
            if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
                SpaceMissionGame.Audio.SpaceSoundFX.Instance.StopAllSounds();

            if (_launchCoroutine != null)
            {
                StopCoroutine(_launchCoroutine);
                _launchCoroutine = null;
            }

            _onSequenceFinished = null;
            if (_stageRoot != null) _stageRoot.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            CancelLaunch();
        }

        private IEnumerator RunStarshipLaunchRoutine(SpacecraftDesign design, MissionEvaluationResult eval)
        {
            if (_skipButton != null)
            {
                TMP_Text btnTxt = _skipButton.GetComponentInChildren<TMP_Text>();
                if (btnTxt != null) btnTxt.text = "SKIP TO 2D ORBIT >>";
                _skipButton.onClick.RemoveAllListeners();
                _skipButton.onClick.AddListener(SkipLaunch);
            }

            bool launchApproved = eval == null || eval.IsLaunchApproved;
            string missionName = (design != null && design.SelectedMission != null)
                ? design.SelectedMission.Title
                : "Artemis Lunar CubeSat Mission";

            bool isLunarMission = design != null && design.SelectedMission != null &&
                (design.SelectedMission.RequiredOrbitId == "ORBIT_LUNAR" ||
                 (!string.IsNullOrEmpty(design.SelectedMission.MissionId) && design.SelectedMission.MissionId.Contains("ARTEMIS")));
            bool isSsoMission = !isLunarMission && design != null && design.SelectedMission != null &&
                (design.SelectedMission.RequiredOrbitId == "ORBIT_SSO" ||
                 (!string.IsNullOrEmpty(design.SelectedMission.MissionId) && design.SelectedMission.MissionId.Contains("BISONSAT")));

            ResetSceneTransforms(design);

            // =========================================================================
            // STAGE 1: T-00:03 IGNITION & LAUNCHPAD STEAM (0.0s -> 2.2s)
            // =========================================================================
            _telemetryPhaseText.text = $"T-00:03 | RAPTOR 33-ENGINE IGNITION — {missionName.ToUpper()}";
            _boosterPlume.gameObject.SetActive(true);
            _starshipPlume.gameObject.SetActive(false);
            if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
                SpaceMissionGame.Audio.SpaceSoundFX.Instance.StartEngineRumble(0.38f, 0.95f);

            float t = 0f;
            while (t < 2.2f)
            {
                t += Time.deltaTime;
                float p = t / 2.2f;

                float flicker = 0.88f + Mathf.Sin(Time.time * 55f) * 0.12f;
                _boosterPlume.localScale = new Vector3(1f, Mathf.Lerp(0.35f, 1.15f, p) * flicker, 1f);
                _rocketAssembly.anchoredPosition = new Vector2(Mathf.Sin(Time.time * 75f) * 2.2f * p, -52f);

                SpawnAndAnimateSmoke(p);
                UpdateTelemetryHUD(-3f + t * 1.35f, 0f, 0f, "BOOSTER: 33x RAPTOR IGNITION");
                yield return null;
            }

            if (!launchApproved)
            {
                if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
                {
                    SpaceMissionGame.Audio.SpaceSoundFX.Instance.StopEngineRumble();
                    SpaceMissionGame.Audio.SpaceSoundFX.Instance.PlayAlertBeep();
                }
                _boosterPlume.gameObject.SetActive(false);
                _telemetryPhaseText.text = "<color=#E84C3D>LAUNCH ABORTED ON PAD — CML 4 PRE-LAUNCH CHECK FAILED!</color>";
                UpdateTelemetryHUD(0f, 0f, 0f, "SCRUBBED: CHECK LICENSES / BATTERY");
                yield return new WaitForSeconds(2.5f);
                yield return StartCoroutine(FadeOutAndFinish());
                yield break;
            }

            // =========================================================================
            // STAGE 2: LIFTOFF, TOWER CLEAR & GRAVITY TURN (2.2s -> 8.6s)
            // =========================================================================
            _telemetryPhaseText.text = "T+00:15 | LIFTOFF! CLEARING MECHAZILLA TOWER";
            t = 0f;
            float durationAscent = 6.4f;
            while (t < durationAscent)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / durationAscent);
                float ease = p * p * (3f - 2f * p);

                _groundAndPadGroup.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -1050f, Mathf.Clamp01(ease * 2.2f)));
                _earthHorizonCurve.anchoredPosition = new Vector2(0f, Mathf.Lerp(-240f, -440f, ease));

                // Fade out launchpad smoke smoothly over the first ~1.5s of liftoff
                FadeOutSmoke(Mathf.Clamp01(p / 0.24f));

                // Bright SFS daylight sky -> deep stratosphere indigo -> black space
                Color skyCol = p < 0.55f
                    ? Color.Lerp(Color.white, new Color(0.16f, 0.30f, 0.62f, 1f), p / 0.55f)
                    : Color.Lerp(new Color(0.16f, 0.30f, 0.62f, 1f), new Color(0.015f, 0.022f, 0.055f, 1f), (p - 0.55f) / 0.45f);
                _skyBackground.color = skyCol;
                _starfieldOverlay.color = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, (p - 0.45f) / 0.48f));

                // Max-Q Supersonic Vapor Cone (after clearing tower & climbing into upper sky: p = 0.46 to 0.78)
                bool inMaxQ = (p >= 0.46f && p <= 0.78f);
                _maxQVaporCone.gameObject.SetActive(inMaxQ);
                if (inMaxQ)
                {
                    float mqT = (p - 0.46f) / 0.32f;
                    float pulse = Mathf.Sin(mqT * Mathf.PI) * (0.90f + Mathf.Sin(Time.time * 52f) * 0.10f);
                    _maxQVaporCone.localScale = new Vector3(1.0f + pulse * 0.35f, 0.9f + pulse * 0.30f, 1f);
                }

                float pitch = Mathf.Lerp(0f, -24f, ease);
                _rocketAssembly.localEulerAngles = new Vector3(0f, 0f, pitch);
                _rocketAssembly.anchoredPosition = new Vector2(
                    Mathf.Sin(Time.time * 40f) * (inMaxQ ? 2.4f : 1.0f) + Mathf.Lerp(0f, 35f, ease),
                    Mathf.Lerp(-52f, 15f, ease)
                );

                float flicker = 0.90f + Mathf.Sin(Time.time * 60f) * 0.12f;
                _boosterPlume.localScale = new Vector3(1.05f, 1.35f * flicker, 1f);

                float altKm = Mathf.Lerp(0f, 68f, p * p);
                float velKmh = Mathf.Lerp(0f, 5400f, p * p);
                string ascLabel = inMaxQ ? "STAGE 1: PASSING MAX-Q (SUPERSONIC)" : "STAGE 1: SUPER HEAVY ASCENT";
                if (inMaxQ) _telemetryPhaseText.text = "T+01:12 | MAX-Q — MAXIMUM AERODYNAMIC PRESSURE";
                else if (p > 0.78f) _telemetryPhaseText.text = "T+02:10 | THROTTLING UP — APPROACHING MECO";
                UpdateTelemetryHUD(p * 145f, altKm, velKmh, ascLabel);
                yield return null;
            }
            _maxQVaporCone.gameObject.SetActive(false);

            // =========================================================================
            // STAGE 3: HOT-STAGING & SUPER HEAVY SEPARATION (6.5s -> 9.8s)
            // =========================================================================
            _telemetryPhaseText.text = "T+02:42 | HOT-STAGING! SUPER HEAVY BOOSTER SEPARATION";
            if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
            {
                SpaceMissionGame.Audio.SpaceSoundFX.Instance.PlaySeparation();
                SpaceMissionGame.Audio.SpaceSoundFX.Instance.StartEngineRumble(0.22f, 1.25f);
            }
            _boosterPlume.gameObject.SetActive(false);
            _starshipPlume.gameObject.SetActive(true);
            _rcsPuffLeft.gameObject.SetActive(true);
            _rcsPuffRight.gameObject.SetActive(true);
            _hotStageFlash.gameObject.SetActive(true);

            Vector2 boosterStartPos = _superHeavyBooster.anchoredPosition;
            Vector2 shipStartPos = _starshipUpperStage.anchoredPosition;

            t = 0f;
            float durationSep = 3.3f;
            while (t < durationSep)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / durationSep);
                float ease = 1f - Mathf.Pow(1f - p, 2f);

                // Hot-stage flash & SFS-style separation puff rings
                _hotStageFlash.localScale = Vector3.one * Mathf.Lerp(0.6f, 2.2f, Mathf.Clamp01(p * 3f));
                _hotStageFlash.gameObject.SetActive(p < 0.35f);
                AnimateSeparationRings(p);

                // Super Heavy falls completely away off-screen behind Stage 2!
                _superHeavyBooster.anchoredPosition = boosterStartPos + new Vector2(-140f * ease, -820f * ease);
                _superHeavyBooster.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, 48f, ease));
                float rcsPulse = Mathf.Abs(Mathf.Sin(Time.time * 12f));
                _rcsPuffLeft.localScale = new Vector3(rcsPulse, rcsPulse, 1f);
                _rcsPuffRight.localScale = new Vector3(rcsPulse * 0.85f, rcsPulse * 0.85f, 1f);

                // Starship accelerates forward and centers in camera
                _starshipUpperStage.anchoredPosition = Vector2.Lerp(shipStartPos, new Vector2(0f, 20f), ease);
                float vacFlicker = 0.92f + Mathf.Sin(Time.time * 50f) * 0.1f;
                _starshipPlume.localScale = new Vector3(1.1f, 1.25f * vacFlicker, 1f);

                _rocketAssembly.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-24f, -55f, ease));

                float stage3TargetAlt = isSsoMission ? 210f : 185f;
                float altKm = Mathf.Lerp(68f, stage3TargetAlt, p);
                float velKmh = Mathf.Lerp(5400f, 19800f, p);
                string stage3Label = isLunarMission
                    ? "STAGE 2: EARTH PARKING ORBIT"
                    : (isSsoMission ? "STAGE 2: POLAR SSO ASCENT" : "STAGE 2: ISS RENDEZVOUS INSERTION");
                UpdateTelemetryHUD(145f + p * 180f, altKm, velKmh, stage3Label);
                yield return null;
            }

            _rcsPuffLeft.gameObject.SetActive(false);
            _rcsPuffRight.gameObject.SetActive(false);
            _superHeavyBooster.gameObject.SetActive(false);
            foreach (RectTransform r in _sepRings) r.gameObject.SetActive(false);

            // =========================================================================
            // STAGE 4: FAIRING / PAYLOAD BAY SEPARATION
            // =========================================================================
            _telemetryPhaseText.text = isLunarMission
                ? "T+08:10 | LEO PARKING ORBIT — FAIRING SEPARATION PRIOR TO TLI"
                : "T+08:10 | MECO & CLAMSHELL FAIRING SEPARATION — PAYLOAD EXPOSED";
            if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
            {
                SpaceMissionGame.Audio.SpaceSoundFX.Instance.StopEngineRumble();
                SpaceMissionGame.Audio.SpaceSoundFX.Instance.PlaySeparation();
            }
            _starshipPlume.gameObject.SetActive(false);
            _payloadAdapter.gameObject.SetActive(true);
            _cubesatPayload.gameObject.SetActive(true);

            t = 0f;
            float durationFairing = 2.4f;
            float stage4StartAlt = isSsoMission ? 210f : 185f;
            float stage4EndAlt = isLunarMission ? 220f : (isSsoMission ? 340f : 400f);
            float stage4Vel = isLunarMission ? 28000f : (isSsoMission ? 27400f : 27600f);
            while (t < durationFairing)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / durationFairing);
                float ease = p * p * (3f - 2f * p);

                // Fairings split open and drift far away off-screen
                _fairingLeft.anchoredPosition = new Vector2(Mathf.Lerp(0f, -420f, ease), Mathf.Lerp(88f, 40f, ease));
                _fairingLeft.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, 55f, ease));

                _fairingRight.anchoredPosition = new Vector2(Mathf.Lerp(0f, 420f, ease), Mathf.Lerp(88f, 40f, ease));
                _fairingRight.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, -55f, ease));

                float altKm = Mathf.Lerp(stage4StartAlt, stage4EndAlt, p);
                UpdateTelemetryHUD(325f + p * 165f, altKm, stage4Vel,
                    isLunarMission ? "LEO PARKING: FAIRING JETTISON" : "ORBITAL INSERTION: FAIRING JETTISON");
                yield return null;
            }

            // =========================================================================
            // MISSION-SPECIFIC ORBITAL MANEUVERS
            // =========================================================================
            if (isSsoMission)
            {
                // MISSION 2 (SSO): Polar Circularization Burn up to 600 km
                _telemetryPhaseText.text = "T+22:15 | POLAR SSO CIRCULARIZATION BURN — CLIMBING TO 600 KM";
                _starshipPlume.gameObject.SetActive(true);
                if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
                    SpaceMissionGame.Audio.SpaceSoundFX.Instance.StartEngineRumble(0.24f, 1.20f);

                t = 0f;
                float durationSsoBurn = 2.4f;
                while (t < durationSsoBurn)
                {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / durationSsoBurn);
                    float ease = p * p * (3f - 2f * p);
                    float vacFlicker = 0.90f + Mathf.Sin(Time.time * 52f) * 0.12f;
                    _starshipPlume.localScale = new Vector3(1.05f, 1.20f * vacFlicker, 1f);

                    _fairingLeft.anchoredPosition += new Vector2(-160f * Time.deltaTime, -100f * Time.deltaTime);
                    _fairingRight.anchoredPosition += new Vector2(160f * Time.deltaTime, -100f * Time.deltaTime);

                    float altKm = Mathf.Lerp(340f, 600f, ease);
                    float velKmh = Mathf.Lerp(27400f, 27200f, ease);
                    UpdateTelemetryHUD(490f + p * 845f, altKm, velKmh, "SSO POLAR BURN: RAISING APOGEE TO 600 KM");
                    yield return null;
                }
                _starshipPlume.gameObject.SetActive(false);
                if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
                    SpaceMissionGame.Audio.SpaceSoundFX.Instance.StopEngineRumble();
            }
            else if (isLunarMission)
            {
                // MISSION 3 (ARTEMIS LUNAR): Step 0 — LEO Starship Tanker Rendezvous & Orbital Refueling
                _telemetryPhaseText.text = "T+00:35:00 | LEO ORBITAL REFUELING — STARSHIP TANKER APPROACH";
                _tankerShip.gameObject.SetActive(true);
                _tankerShip.localEulerAngles = new Vector3(0f, 0f, 180f); // Tail-to-tail docking orientation

                bool dockedTriggered = false;
                bool undockedTriggered = false;
                t = 0f;
                float durationRefuel = 3.4f;
                while (t < durationRefuel)
                {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / durationRefuel);

                    _fairingLeft.anchoredPosition += new Vector2(-160f * Time.deltaTime, -100f * Time.deltaTime);
                    _fairingRight.anchoredPosition += new Vector2(160f * Time.deltaTime, -100f * Time.deltaTime);

                    if (p < 0.35f)
                    {
                        float app = Mathf.SmoothStep(0f, 1f, p / 0.35f);
                        _tankerShip.anchoredPosition = new Vector2(0f, Mathf.Lerp(-480f, -66f, app));
                        UpdateTelemetryHUD(2100f + p * 600f, 220f, 28000f, "LEO RENDEZVOUS: TANKER FINAL APPROACH");
                    }
                    else if (p < 0.78f)
                    {
                        if (!dockedTriggered)
                        {
                            dockedTriggered = true;
                            if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
                                SpaceMissionGame.Audio.SpaceSoundFX.Instance.PlayEquip();
                        }
                        _tankerShip.anchoredPosition = new Vector2(0f, -66f);
                        float xfer = (p - 0.35f) / 0.43f;
                        int pct = Mathf.RoundToInt(xfer * 100f);
                        _telemetryPhaseText.text = $"T+00:45:00 | ORBITAL PROPELLANT TRANSFER — LOX / CH4 ({pct}%)";
                        UpdateTelemetryHUD(2700f + xfer * 1200f, 220f, 28000f, $"TANKER DOCKED: REFUELING {pct}%");
                    }
                    else
                    {
                        if (!undockedTriggered)
                        {
                            undockedTriggered = true;
                            if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
                                SpaceMissionGame.Audio.SpaceSoundFX.Instance.PlaySeparation();
                            _telemetryPhaseText.text = "T+01:05:00 | TANKER UNDOCKED — PROPELLANT 100% READY FOR TLI";
                        }
                        float dep = Mathf.Pow((p - 0.78f) / 0.22f, 2f);
                        _tankerShip.anchoredPosition = new Vector2(0f, Mathf.Lerp(-66f, -520f, dep));
                        UpdateTelemetryHUD(3900f + dep * 600f, 220f, 28000f, "TANKER SEPARATION: CLEARED FOR TLI");
                    }
                    yield return null;
                }
                _tankerShip.gameObject.SetActive(false);

                // Step 1 — Trans-Lunar Injection (TLI) Escape Burn
                _telemetryPhaseText.text = "T+01:15:00 | TRANS-LUNAR INJECTION (TLI) BURN — ESCAPING EARTH ORBIT";
                _starshipPlume.gameObject.SetActive(true);
                if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
                    SpaceMissionGame.Audio.SpaceSoundFX.Instance.StartEngineRumble(0.30f, 1.35f);

                t = 0f;
                float durationTli = 3.0f;
                while (t < durationTli)
                {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / durationTli);
                    float ease = p * p * (3f - 2f * p);
                    float vacFlicker = 0.92f + Mathf.Sin(Time.time * 60f) * 0.14f;
                    _starshipPlume.localScale = new Vector3(1.25f, 1.55f * vacFlicker, 1f);

                    _fairingLeft.anchoredPosition += new Vector2(-180f * Time.deltaTime, -110f * Time.deltaTime);
                    _fairingRight.anchoredPosition += new Vector2(180f * Time.deltaTime, -110f * Time.deltaTime);

                    float distKm = Mathf.Lerp(220f, 48000f, ease * ease);
                    float velKmh = Mathf.Lerp(28000f, 39400f, ease);
                    UpdateTelemetryHUD(4500f + p * 360f, distKm, velKmh, "TLI BURN: ACCELERATING TO LUNAR TRANSFER");
                    yield return null;
                }

                // Step 2 — Translunar Coast & Moon Approach
                _starshipPlume.gameObject.SetActive(false);
                if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
                    SpaceMissionGame.Audio.SpaceSoundFX.Instance.StopEngineRumble();
                _telemetryPhaseText.text = "T+72:00:00 | TRANSLUNAR COAST — APPROACHING LUNAR SOUTH POLE";

                _moonDisc.gameObject.SetActive(true);
                t = 0f;
                float durationCoast = 2.6f;
                while (t < durationCoast)
                {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / durationCoast);
                    float ease = 1f - Mathf.Pow(1f - p, 2f);

                    _moonDisc.anchoredPosition = Vector2.Lerp(new Vector2(680f, 240f), new Vector2(250f, 75f), ease);
                    _moonDisc.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.0f, ease);

                    float distKm = Mathf.Lerp(48000f, 384400f, ease);
                    float velKmh = Mathf.Lerp(39400f, 8400f, ease);
                    UpdateTelemetryHUD(4860f + p * 254340f, distKm, velKmh, "TRANSLUNAR COAST: MOON INTERCEPT");
                    yield return null;
                }

                // Step 3 — Lunar Orbit Insertion (LOI) Retrograde Capture Burn
                _telemetryPhaseText.text = "T+84:30:00 | LUNAR ORBIT INSERTION (LOI) — CAPTURING 100 KM LUNAR ORBIT";
                _starshipPlume.gameObject.SetActive(true);
                if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
                    SpaceMissionGame.Audio.SpaceSoundFX.Instance.StartEngineRumble(0.24f, 1.15f);

                t = 0f;
                float durationLoi = 2.2f;
                while (t < durationLoi)
                {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / durationLoi);
                    float ease = p * p * (3f - 2f * p);
                    float vacFlicker = 0.90f + Mathf.Sin(Time.time * 50f) * 0.12f;
                    _starshipPlume.localScale = new Vector3(1.05f, 1.25f * vacFlicker, 1f);

                    _rocketAssembly.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-55f, 125f, ease));

                    float velKmh = Mathf.Lerp(8400f, 5900f, ease);
                    UpdateTelemetryHUD(259200f + p * 4500f, 384400f, velKmh, "LOI BURN: 100 KM LUNAR ORBIT CAPTURE");
                    yield return null;
                }

                _starshipPlume.gameObject.SetActive(false);
                if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
                    SpaceMissionGame.Audio.SpaceSoundFX.Instance.StopEngineRumble();
                _rocketAssembly.localEulerAngles = new Vector3(0f, 0f, -55f);
            }

            // =========================================================================
            // STAGE 5: CUBESAT DISPENSER EJECTION & BURN-WIRE DEPLOY
            // =========================================================================
            _telemetryPhaseText.text = isLunarMission
                ? "T+85:00:00 | 6U ARTEMIS LUNAR PATHFINDER DEPLOYED IN LUNAR ORBIT!"
                : (isSsoMission
                    ? "T+45:00 | 3U BISONSAT DEPLOYED IN 600 KM POLAR SSO!"
                    : "T+45:00 | CUBESAT DISPENSER EJECTION & BURN-WIRE SOLAR DEPLOYMENT!");
            if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
                SpaceMissionGame.Audio.SpaceSoundFX.Instance.PlaySeparation();
            t = 0f;
            float durationDeploy = 2.8f;
            Vector2 shipDeployStart = _starshipUpperStage.anchoredPosition;
            float finalAltKm = isLunarMission ? 384400f : (isSsoMission ? 600f : 400f);
            float finalVelKmh = isLunarMission ? 5900f : (isSsoMission ? 27200f : 27600f);
            float finalTimeSec = isLunarMission ? 306000f : 2700f;
            string finalStatus = isLunarMission
                ? "NOMINAL: 6U PATHFINDER IN 100 KM LUNAR ORBIT"
                : (isSsoMission ? "NOMINAL: 3U BISONSAT IN 600 KM SSO" : "NOMINAL: CUBESAT IN ISS 400 KM ORBIT");

            while (t < durationDeploy)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / durationDeploy);
                float ease = 1f - Mathf.Pow(1f - p, 3f);

                // Fairings continue drifting off-screen
                _fairingLeft.anchoredPosition += new Vector2(-180f * Time.deltaTime, -120f * Time.deltaTime);
                _fairingRight.anchoredPosition += new Vector2(180f * Time.deltaTime, -120f * Time.deltaTime);

                // Stage 2 pulls back/falls away off-screen while CubeSat stays centered in orbit!
                float shipDrop = Mathf.Lerp(0f, -580f, p * p);
                _starshipUpperStage.anchoredPosition = shipDeployStart + new Vector2(0f, shipDrop);
                _cubesatPayload.anchoredPosition = new Vector2(0f, 78f - shipDrop + Mathf.Lerp(0f, 40f, ease));
                _cubesatPayload.localScale = Vector3.one * Mathf.Lerp(0.95f, 1.55f, ease);

                float wingP = Mathf.Clamp01((p - 0.30f) / 0.45f);
                _cubesatWingLeft.localScale = new Vector3(wingP, 1f, 1f);
                _cubesatWingRight.localScale = new Vector3(wingP, 1f, 1f);

                UpdateTelemetryHUD(finalTimeSec, finalAltKm, finalVelKmh, finalStatus);
                yield return null;
            }

            if (SpaceMissionGame.Audio.SpaceSoundFX.Instance != null)
                SpaceMissionGame.Audio.SpaceSoundFX.Instance.PlaySuccessChime();
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(FadeOutAndFinish());
        }

        private IEnumerator FadeOutAndFinish()
        {
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = 1f - (t / 0.45f);
                yield return null;
            }
            _stageRoot.gameObject.SetActive(false);
            _onSequenceFinished?.Invoke();
        }

        private void ResetSceneTransforms(SpacecraftDesign design)
        {
            _skyBackground.color = Color.white; // Shows bright daylight sky gradient!
            _starfieldOverlay.color = new Color(1f, 1f, 1f, 0f);
            _groundAndPadGroup.anchoredPosition = Vector2.zero;
            _earthHorizonCurve.anchoredPosition = new Vector2(0f, -240f);
            _maxQVaporCone.gameObject.SetActive(false);
            if (_moonDisc != null) _moonDisc.gameObject.SetActive(false);
            if (_tankerShip != null) _tankerShip.gameObject.SetActive(false);

            _rocketAssembly.anchoredPosition = new Vector2(0f, -52f);
            _rocketAssembly.localEulerAngles = Vector3.zero;

            _superHeavyBooster.gameObject.SetActive(true);
            _superHeavyBooster.anchoredPosition = new Vector2(0f, -86f);
            _superHeavyBooster.localEulerAngles = Vector3.zero;

            _starshipUpperStage.anchoredPosition = new Vector2(0f, 46f);
            _starshipUpperStage.localEulerAngles = Vector3.zero;

            _fairingLeft.anchoredPosition = new Vector2(0f, 88f);
            _fairingLeft.localEulerAngles = Vector3.zero;

            _fairingRight.anchoredPosition = new Vector2(0f, 88f);
            _fairingRight.localEulerAngles = Vector3.zero;

            _boosterPlume.gameObject.SetActive(false);
            _starshipPlume.gameObject.SetActive(false);
            _rcsPuffLeft.gameObject.SetActive(false);
            _rcsPuffRight.gameObject.SetActive(false);
            _hotStageFlash.gameObject.SetActive(false);
            _payloadAdapter.gameObject.SetActive(false);

            _cubesatPayload.gameObject.SetActive(false);
            _cubesatPayload.anchoredPosition = new Vector2(0f, 78f);
            _cubesatPayload.localScale = Vector3.one * 0.9f;
            _cubesatWingLeft.localScale = new Vector3(0f, 1f, 1f);
            _cubesatWingRight.localScale = new Vector3(0f, 1f, 1f);

            string chassis = (design != null && design.SelectedChassis != null) ? design.SelectedChassis.Id : "CHASSIS_3U";
            if (chassis.Contains("1U")) _cubesatPayload.sizeDelta = new Vector2(26f, 26f);
            else if (chassis.Contains("6U")) _cubesatPayload.sizeDelta = new Vector2(42f, 34f);
            else _cubesatPayload.sizeDelta = new Vector2(26f, 40f);

            foreach (RectTransform s in _smokeParticles)
            {
                Image img = s.GetComponent<Image>();
                if (img != null) img.color = new Color(0.95f, 0.96f, 0.98f, 0.60f);
                s.gameObject.SetActive(false);
            }
            foreach (RectTransform r in _sepRings) r.gameObject.SetActive(false);
        }

        private void SpawnAndAnimateSmoke(float progress)
        {
            for (int i = 0; i < _smokeParticles.Count; i++)
            {
                RectTransform p = _smokeParticles[i];
                if (!p.gameObject.activeSelf)
                {
                    p.gameObject.SetActive(true);
                    float side = (i % 2 == 0) ? -1f : 1f;
                    p.anchoredPosition = new Vector2(side * (8f + (i * 7f) % 45f), -190f);
                }

                float sideDir = (i % 2 == 0) ? -1f : 1f;
                float speed = 95f + (i * 19f) % 80f;
                Vector2 pos = p.anchoredPosition;
                pos.x += sideDir * speed * Time.deltaTime;
                pos.y += Mathf.Sin(Time.time * 4f + i) * 18f * Time.deltaTime;
                p.anchoredPosition = pos;

                float scale = 0.7f + progress * 1.4f + (i % 3) * 0.25f;
                p.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void FadeOutSmoke(float fadeProgress)
        {
            float alpha = Mathf.Lerp(0.60f, 0f, fadeProgress);
            for (int i = 0; i < _smokeParticles.Count; i++)
            {
                RectTransform p = _smokeParticles[i];
                if (!p.gameObject.activeSelf) continue;

                if (fadeProgress >= 0.99f)
                {
                    p.gameObject.SetActive(false);
                    continue;
                }

                float sideDir = (i % 2 == 0) ? -1f : 1f;
                p.anchoredPosition += new Vector2(sideDir * 35f * Time.deltaTime, 8f * Time.deltaTime);
                Image img = p.GetComponent<Image>();
                if (img != null) img.color = new Color(0.95f, 0.96f, 0.98f, alpha);
            }
        }

        private void AnimateSeparationRings(float p)
        {
            for (int i = 0; i < _sepRings.Count; i++)
            {
                RectTransform r = _sepRings[i];
                if (!r.gameObject.activeSelf)
                {
                    r.gameObject.SetActive(true);
                    float angle = (i / (float)_sepRings.Count) * Mathf.PI * 2f;
                    r.anchoredPosition = new Vector2(Mathf.Cos(angle) * 18f, Mathf.Sin(angle) * 8f - 8f);
                }
                float speed = 45f + i * 9f;
                float ang = (i / (float)_sepRings.Count) * Mathf.PI * 2f;
                r.anchoredPosition += new Vector2(Mathf.Cos(ang) * speed * Time.deltaTime, Mathf.Sin(ang) * speed * 0.45f * Time.deltaTime);
                r.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.8f, p);
            }
        }

        private void UpdateTelemetryHUD(float tSec, float altKm, float velKmh, string stageLabel)
        {
            string sign = tSec < 0f ? "-" : "+";
            float absT = Mathf.Abs(tSec);
            string timeStr;
            if (absT >= 3600f)
            {
                int hrs = Mathf.FloorToInt(absT / 3600f);
                int mins = Mathf.FloorToInt((absT % 3600f) / 60f);
                int secs = Mathf.FloorToInt(absT % 60f);
                timeStr = $"T{sign}{hrs:00}:{mins:00}:{secs:00}";
            }
            else
            {
                int mins = Mathf.FloorToInt(absT / 60f);
                int secs = Mathf.FloorToInt(absT % 60f);
                timeStr = $"T{sign}{mins:00}:{secs:00}";
            }

            string altStr = altKm >= 1000f ? $"{altKm:N0} km" : $"{altKm:F1} km";
            _telemetryStatsText.text =
                $"{timeStr}   |   ALT: {altStr}   |   VEL: {velKmh:N0} km/h   |   {stageLabel}";
        }

        // =========================================================================
        // PROCEDURAL UI & SFS-STYLE 2D ROCKET CONSTRUCTION
        // =========================================================================
        private void EnsureUIBuilt()
        {
            if (_stageRoot != null) return;
            EnsureProceduralRocketSprites();

            GameObject rootObj = new GameObject("StarshipLaunchStage2D", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            rootObj.transform.SetParent(transform, false);
            _stageRoot = rootObj.GetComponent<RectTransform>();
            _stageRoot.anchorMin = Vector2.zero;
            _stageRoot.anchorMax = Vector2.one;
            _stageRoot.offsetMin = Vector2.zero;
            _stageRoot.offsetMax = Vector2.zero;
            _canvasGroup = rootObj.GetComponent<CanvasGroup>();
            _skyBackground = rootObj.GetComponent<Image>();
            _skyBackground.sprite = _skyGradientSprite;
            _skyBackground.color = new Color(0.18f, 0.48f, 0.86f, 1f);
            rootObj.AddComponent<RectMask2D>();

            // Starfield (DO NOT run SpaceUITheme.StyleHierarchy on this so _starSprite stays intact!)
            _starfieldOverlay = CreateImg(_stageRoot, "LaunchStarfield", _starSprite, new Color(1f, 1f, 1f, 0f));
            Stretch(_starfieldOverlay.rectTransform);
            _starfieldOverlay.type = Image.Type.Tiled;

            // Curved planetary horizon
            Image horizon = CreateImg(_stageRoot, "LaunchHorizon", _horizonSprite, new Color(0.11f, 0.28f, 0.52f, 1f));
            _earthHorizonCurve = horizon.rectTransform;
            _earthHorizonCurve.anchorMin = new Vector2(0.5f, 0f);
            _earthHorizonCurve.anchorMax = new Vector2(0.5f, 0f);
            _earthHorizonCurve.sizeDelta = new Vector2(2400f, 640f);
            _earthHorizonCurve.anchoredPosition = new Vector2(0f, -240f);

            // Lunar Target Disc (shown during Artemis Translunar Coast & LOI)
            Image moonImg = CreateImg(_stageRoot, "LunarMoonDisc", _moonSprite, Color.white);
            _moonDisc = moonImg.rectTransform;
            _moonDisc.sizeDelta = new Vector2(270f, 270f);
            _moonDisc.anchoredPosition = new Vector2(680f, 240f);
            _moonDisc.gameObject.SetActive(false);

            // Launchpad & Mechazilla Tower Group (SFS-style clean concrete pad)
            _groundAndPadGroup = CreateGroup(_stageRoot, "PadAndTowerGroup");
            Stretch(_groundAndPadGroup);

            Image groundPad = CreateImg(_groundAndPadGroup, "ConcretePad", null, new Color(0.42f, 0.43f, 0.44f, 1f));
            groundPad.rectTransform.anchorMin = new Vector2(0f, 0f);
            groundPad.rectTransform.anchorMax = new Vector2(1f, 0f);
            groundPad.rectTransform.sizeDelta = new Vector2(0f, 96f);
            groundPad.rectTransform.anchoredPosition = new Vector2(0f, 48f);

            Image olmBase = CreateImg(_groundAndPadGroup, "OrbitalLaunchMount", null, new Color(0.24f, 0.27f, 0.32f, 1f));
            olmBase.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            olmBase.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            olmBase.rectTransform.sizeDelta = new Vector2(96f, 42f);
            olmBase.rectTransform.anchoredPosition = new Vector2(0f, 114f);

            _towerGroup = CreateGroup(_groundAndPadGroup, "MechazillaTower");
            _towerGroup.anchorMin = new Vector2(0.5f, 0f);
            _towerGroup.anchorMax = new Vector2(0.5f, 0f);
            _towerGroup.anchoredPosition = new Vector2(-94f, 290f);
            _towerGroup.sizeDelta = new Vector2(42f, 410f);

            Image towerSpine = CreateImg(_towerGroup, "TowerTruss", null, new Color(0.20f, 0.23f, 0.28f, 1f));
            Stretch(towerSpine.rectTransform);
            Image chopstickArm = CreateImg(_towerGroup, "ChopstickArms", null, new Color(0.42f, 0.45f, 0.50f, 1f));
            chopstickArm.rectTransform.sizeDelta = new Vector2(88f, 12f);
            chopstickArm.rectTransform.anchoredPosition = new Vector2(44f, 118f);

            // Rocket Assembly
            _rocketAssembly = CreateGroup(_stageRoot, "StarshipSuperHeavyAssembly");
            _rocketAssembly.anchorMin = new Vector2(0.5f, 0.5f);
            _rocketAssembly.anchorMax = new Vector2(0.5f, 0.5f);
            _rocketAssembly.sizeDelta = new Vector2(140f, 380f);

            // 1. SUPER HEAVY BOOSTER (SFS style with dark outlines, chines, grid fins, 5 Raptor nozzles)
            _superHeavyBooster = CreateGroup(_rocketAssembly, "SuperHeavyBooster");
            _superHeavyBooster.sizeDelta = new Vector2(48f, 186f);

            Image boosterBody = CreateImg(_superHeavyBooster, "BoosterSteel", _boosterSprite, Color.white);
            Stretch(boosterBody.rectTransform);

            // Booster Raptor Nozzle Bells
            Image bNozzles = CreateImg(_superHeavyBooster, "BoosterNozzles", _nozzleRowSprite, Color.white);
            bNozzles.rectTransform.sizeDelta = new Vector2(46f, 14f);
            bNozzles.rectTransform.anchoredPosition = new Vector2(0f, -96f);

            // Hot-stage black ring + grid fins
            Image hotStage = CreateImg(_superHeavyBooster, "HotStageRing", null, new Color(0.10f, 0.12f, 0.15f, 1f));
            hotStage.rectTransform.anchorMin = new Vector2(0f, 1f);
            hotStage.rectTransform.anchorMax = new Vector2(1f, 1f);
            hotStage.rectTransform.sizeDelta = new Vector2(0f, 14f);
            hotStage.rectTransform.anchoredPosition = new Vector2(0f, -7f);

            Image finL = CreateImg(_superHeavyBooster, "GridFinL", null, new Color(0.16f, 0.18f, 0.22f, 1f));
            finL.rectTransform.sizeDelta = new Vector2(18f, 11f);
            finL.rectTransform.anchoredPosition = new Vector2(-28f, 76f);

            Image finR = CreateImg(_superHeavyBooster, "GridFinR", null, new Color(0.16f, 0.18f, 0.22f, 1f));
            finR.rectTransform.sizeDelta = new Vector2(18f, 11f);
            finR.rectTransform.anchoredPosition = new Vector2(28f, 76f);

            Image bPlume = CreateImg(_superHeavyBooster, "BoosterPlume", _plumeSprite, new Color(1f, 0.68f, 0.28f, 0.95f));
            _boosterPlume = bPlume.rectTransform;
            _boosterPlume.pivot = new Vector2(0.5f, 1f);
            _boosterPlume.sizeDelta = new Vector2(62f, 155f);
            _boosterPlume.anchoredPosition = new Vector2(0f, -100f);

            Image rcsL = CreateImg(_superHeavyBooster, "RcsPuffL", _smokeSprite, new Color(0.92f, 0.96f, 1f, 0.85f));
            _rcsPuffLeft = rcsL.rectTransform;
            _rcsPuffLeft.sizeDelta = new Vector2(30f, 14f);
            _rcsPuffLeft.anchoredPosition = new Vector2(-38f, 78f);

            Image rcsR = CreateImg(_superHeavyBooster, "RcsPuffR", _smokeSprite, new Color(0.92f, 0.96f, 1f, 0.85f));
            _rcsPuffRight = rcsR.rectTransform;
            _rcsPuffRight.sizeDelta = new Vector2(30f, 14f);
            _rcsPuffRight.anchoredPosition = new Vector2(38f, -76f);

            // 2. STARSHIP UPPER STAGE (SFS style: 48px lower tank + aft flaps + payload adapter + seamless 48px fairing)
            _starshipUpperStage = CreateGroup(_rocketAssembly, "LunarStarshipUpperStage");
            _starshipUpperStage.sizeDelta = new Vector2(48f, 74f);

            Image shipBody = CreateImg(_starshipUpperStage, "StarshipHull", _starshipBodySprite, Color.white);
            Stretch(shipBody.rectTransform);

            // Starship Vacuum + Sea-Level Raptor Nozzles (2 large bells)
            Image sNozzles = CreateImg(_starshipUpperStage, "StarshipNozzles", _nozzleRowSprite, Color.white);
            sNozzles.rectTransform.sizeDelta = new Vector2(36f, 12f);
            sNozzles.rectTransform.anchoredPosition = new Vector2(0f, -41f);

            // Aft Aero Flaps (left & right flush against 48px tank)
            Image aftL = CreateImg(_starshipUpperStage, "AftFlapL", _flapLeftSprite, Color.white);
            aftL.rectTransform.sizeDelta = new Vector2(18f, 44f);
            aftL.rectTransform.anchoredPosition = new Vector2(-31f, -14f);

            Image aftR = CreateImg(_starshipUpperStage, "AftFlapR", _flapRightSprite, Color.white);
            aftR.rectTransform.sizeDelta = new Vector2(18f, 44f);
            aftR.rectTransform.anchoredPosition = new Vector2(31f, -14f);

            // Payload Adapter Cone (revealed when fairing opens)
            Image adapterImg = CreateImg(_starshipUpperStage, "PayloadAdapter", _adapterSprite, Color.white);
            _payloadAdapter = adapterImg.rectTransform;
            _payloadAdapter.sizeDelta = new Vector2(36f, 20f);
            _payloadAdapter.anchoredPosition = new Vector2(0f, 46f);

            // Orbital Propellant Tanker Starship (for Mission 3 LEO tail-to-tail refueling)
            _tankerShip = CreateGroup(_rocketAssembly, "StarshipPropellantTanker");
            _tankerShip.sizeDelta = new Vector2(48f, 74f);
            _tankerShip.anchoredPosition = new Vector2(0f, -480f);
            Image tankerBody = CreateImg(_tankerShip, "TankerHull", _starshipBodySprite, new Color(0.86f, 0.89f, 0.93f, 1f));
            Stretch(tankerBody.rectTransform);
            Image tNozzles = CreateImg(_tankerShip, "TankerNozzles", _nozzleRowSprite, Color.white);
            tNozzles.rectTransform.sizeDelta = new Vector2(36f, 12f);
            tNozzles.rectTransform.anchoredPosition = new Vector2(0f, -41f);
            Image tAftL = CreateImg(_tankerShip, "TankerAftFlapL", _flapLeftSprite, Color.white);
            tAftL.rectTransform.sizeDelta = new Vector2(18f, 44f);
            tAftL.rectTransform.anchoredPosition = new Vector2(-31f, -14f);
            Image tAftR = CreateImg(_tankerShip, "TankerAftFlapR", _flapRightSprite, Color.white);
            tAftR.rectTransform.sizeDelta = new Vector2(18f, 44f);
            tAftR.rectTransform.anchoredPosition = new Vector2(31f, -14f);
            Image tNoseL = CreateImg(_tankerShip, "TankerNoseL", _fairingLeftSprite, new Color(0.84f, 0.87f, 0.92f, 1f));
            tNoseL.rectTransform.sizeDelta = new Vector2(24f, 102f);
            tNoseL.rectTransform.anchoredPosition = new Vector2(-12f, 88f);
            Image tNoseR = CreateImg(_tankerShip, "TankerNoseR", _fairingRightSprite, new Color(0.84f, 0.87f, 0.92f, 1f));
            tNoseR.rectTransform.sizeDelta = new Vector2(24f, 102f);
            tNoseR.rectTransform.anchoredPosition = new Vector2(12f, 88f);
            _tankerShip.gameObject.SetActive(false);

            // Hot-stage flash + SFS separation VFX rings
            Image flash = CreateImg(_starshipUpperStage, "HotStageFlash", _smokeSprite, new Color(1f, 0.65f, 0.25f, 0.9f));
            _hotStageFlash = flash.rectTransform;
            _hotStageFlash.sizeDelta = new Vector2(68f, 28f);
            _hotStageFlash.anchoredPosition = new Vector2(0f, -42f);

            for (int i = 0; i < 8; i++)
            {
                Image ring = CreateImg(_starshipUpperStage, $"SepRing_{i}", _ringVfxSprite, new Color(0.95f, 0.96f, 1f, 0.8f));
                ring.rectTransform.sizeDelta = new Vector2(18f, 18f);
                ring.gameObject.SetActive(false);
                _sepRings.Add(ring.rectTransform);
            }

            // CubeSat Payload Inside Open Fairing Bay (SFS detailed bus + blue solar wings)
            Image satImg = CreateImg(_starshipUpperStage, "CubeSatPayload", _cubesatBusSprite, Color.white);
            _cubesatPayload = satImg.rectTransform;
            _cubesatPayload.sizeDelta = new Vector2(26f, 40f);
            _cubesatPayload.anchoredPosition = new Vector2(0f, 78f);

            Image wL = CreateImg(_cubesatPayload, "SatWingL", _solarWingSprite, Color.white);
            _cubesatWingLeft = wL.rectTransform;
            _cubesatWingLeft.pivot = new Vector2(1f, 0.5f);
            _cubesatWingLeft.sizeDelta = new Vector2(36f, 16f);
            _cubesatWingLeft.anchoredPosition = new Vector2(-13f, 0f);

            Image wR = CreateImg(_cubesatPayload, "SatWingR", _solarWingSprite, Color.white);
            _cubesatWingRight = wR.rectTransform;
            _cubesatWingRight.pivot = new Vector2(0f, 0.5f);
            _cubesatWingRight.sizeDelta = new Vector2(36f, 16f);
            _cubesatWingRight.anchoredPosition = new Vector2(13f, 0f);

            // Clamshell Fairing Halves (exact 24px half-cylinder + 14px forward flap, pivoted at center seam!)
            Image fL = CreateImg(_starshipUpperStage, "FairingHalfLeft", _fairingLeftSprite, Color.white);
            _fairingLeft = fL.rectTransform;
            _fairingLeft.pivot = new Vector2(1f, 0.5f);
            _fairingLeft.sizeDelta = new Vector2(38f, 114f);
            _fairingLeft.anchoredPosition = new Vector2(0f, 88f);

            Image fR = CreateImg(_starshipUpperStage, "FairingHalfRight", _fairingRightSprite, Color.white);
            _fairingRight = fR.rectTransform;
            _fairingRight.pivot = new Vector2(0f, 0.5f);
            _fairingRight.sizeDelta = new Vector2(38f, 114f);
            _fairingRight.anchoredPosition = new Vector2(0f, 88f);

            // Starship Vacuum Plume
            Image sPlume = CreateImg(_starshipUpperStage, "StarshipVacPlume", _plumeSprite, new Color(0.45f, 0.78f, 1f, 0.95f));
            _starshipPlume = sPlume.rectTransform;
            _starshipPlume.pivot = new Vector2(0.5f, 1f);
            _starshipPlume.sizeDelta = new Vector2(46f, 122f);
            _starshipPlume.anchoredPosition = new Vector2(0f, -45f);

            // Max-Q Supersonic Vapor Cone wrapping around Starship nosecone & shoulder
            Image vapor = CreateImg(_rocketAssembly, "MaxQVaporCone", _vaporConeSprite, new Color(0.95f, 0.99f, 1f, 0.88f));
            _maxQVaporCone = vapor.rectTransform;
            _maxQVaporCone.sizeDelta = new Vector2(180f, 110f);
            _maxQVaporCone.anchoredPosition = new Vector2(0f, 128f);
            _maxQVaporCone.gameObject.SetActive(false);

            // Launchpad Smoke Particles Pool
            for (int i = 0; i < 14; i++)
            {
                Image smoke = CreateImg(_groundAndPadGroup, $"PadSmoke_{i}", _smokeSprite, new Color(0.95f, 0.97f, 1f, 0.65f));
                smoke.rectTransform.sizeDelta = new Vector2(76f, 52f);
                smoke.gameObject.SetActive(false);
                _smokeParticles.Add(smoke.rectTransform);
            }

            // Top Telemetry Banner (only style HUD elements, NOT the rocket/starfield!)
            Image topHud = CreateImg(_stageRoot, "LaunchTelemetryTopBar", null, new Color(0.03f, 0.05f, 0.10f, 0.86f));
            topHud.rectTransform.anchorMin = new Vector2(0f, 1f);
            topHud.rectTransform.anchorMax = new Vector2(1f, 1f);
            topHud.rectTransform.sizeDelta = new Vector2(-24f, 68f);
            topHud.rectTransform.anchoredPosition = new Vector2(0f, -44f);

            _telemetryPhaseText = CreateTxt(topHud.rectTransform, "PhaseHeader", "STARSHIP + SUPER HEAVY LUNAR LAUNCH", 17, FontStyles.Bold, new Vector2(0f, 14f));
            _telemetryStatsText = CreateTxt(topHud.rectTransform, "TelemetryStats", "T-00:03 | ALT: 0.0 km | VEL: 0 km/h", 14, FontStyles.Normal, new Vector2(0f, -14f));
            _telemetryStatsText.color = new Color(0.55f, 0.88f, 1f);
            SpaceUITheme.StyleHierarchy(topHud.transform, false);

            // Skip Launch Button (Bottom Right)
            GameObject skipGo = new GameObject("SkipLaunchAnimButton", typeof(RectTransform), typeof(Image), typeof(Button));
            skipGo.transform.SetParent(_stageRoot, false);
            RectTransform skipRt = skipGo.GetComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(1f, 0f);
            skipRt.anchorMax = new Vector2(1f, 0f);
            skipRt.pivot = new Vector2(1f, 0f);
            skipRt.sizeDelta = new Vector2(210f, 42f);
            skipRt.anchoredPosition = new Vector2(-18f, 18f);
            skipGo.GetComponent<Image>().color = new Color(0.14f, 0.24f, 0.38f, 0.95f);

            TMP_Text skipTxt = CreateTxt(skipRt, "Label", "SKIP TO 2D ORBIT >>", 13, FontStyles.Bold, Vector2.zero);
            Stretch(skipTxt.rectTransform);
            _skipButton = skipGo.GetComponent<Button>();
            _skipButton.onClick.AddListener(SkipLaunch);
            SpaceUITheme.StyleHierarchy(skipGo.transform, true);
        }

        // =========================================================================
        // SFS-STYLE PROCEDURAL VECTOR TEXTURES (crisp dark outlines + shading)
        // =========================================================================
        private static void EnsureProceduralRocketSprites()
        {
            if (_boosterSprite != null) return;

            _boosterSprite = MakeSfsCylinderSprite(96, 256, true);
            _starshipBodySprite = MakeSfsCylinderSprite(96, 160, false);
            _fairingLeftSprite = MakeSfsFairingHalfSprite(76, 228, true);
            _fairingRightSprite = MakeSfsFairingHalfSprite(76, 228, false);
            _flapLeftSprite = MakeSfsFlapSprite(48, 96, true);
            _flapRightSprite = MakeSfsFlapSprite(48, 96, false);
            _nozzleRowSprite = MakeSfsNozzlesSprite(96, 28);
            _adapterSprite = MakeSfsAdapterSprite(72, 40);
            _cubesatBusSprite = MakeSfsCubeSatSprite(64, 96);
            _solarWingSprite = MakeSfsSolarWingSprite(96, 40);
            _plumeSprite = MakePlumeSprite(64, 128);
            _smokeSprite = MakeSmokeSprite(64);
            _ringVfxSprite = MakeRingVfxSprite(48);
            _skyGradientSprite = MakeVerticalGradientSprite();
            _starSprite = MakeStarTile(256);
            _horizonSprite = MakeHorizonArcSprite(512, 128);
            _vaporConeSprite = MakeVaporConeSprite(128, 64);
            _moonSprite = MakeMoonSprite(128);
        }

        private static Sprite MakeSfsCylinderSprite(int w, int h, bool isBooster)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color outline = new Color(0.08f, 0.10f, 0.13f, 1f);
            Color seamCol = new Color(0.28f, 0.31f, 0.36f, 1f);
            Color[] px = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                bool horizSeam = (y % 42 <= 1);
                bool lowerChineZone = isBooster && y < h * 0.42f;
                for (int x = 0; x < w; x++)
                {
                    // Outer SFS dark border
                    if (x < 4 || x >= w - 4 || y < 3 || y >= h - 3)
                    {
                        px[y * w + x] = outline;
                        continue;
                    }

                    float u = x / (float)(w - 1);
                    // Clean SFS left-lit shading
                    float light = u < 0.25f ? 0.96f : (u < 0.65f ? 0.90f : (u < 0.82f ? 0.72f : 0.60f));
                    Color baseCol = isBooster ? new Color(0.84f, 0.87f, 0.91f) : new Color(0.94f, 0.96f, 0.99f);
                    Color c = baseCol * light;

                    // Vertical panel line & Booster chines/raceways
                    if (Mathf.Abs(x - w / 2) <= 1) c = Color.Lerp(c, seamCol, 0.55f);
                    if (lowerChineZone && (Mathf.Abs(x - 18) <= 2 || Mathf.Abs(x - (w - 19)) <= 2))
                    {
                        c = new Color(0.68f, 0.72f, 0.77f);
                    }
                    if (horizSeam) c = Color.Lerp(c, seamCol, 0.65f);
                    c.a = 1f;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }


        private static Sprite MakeSfsFairingHalfSprite(int w, int h, bool isLeft)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color outline = new Color(0.08f, 0.10f, 0.13f, 1f);
            Color[] px = new Color[w * h];

            // Inner 24/38 = 0.6316 of width is the half-cylinder; outer 14/38 = 0.3684 is for the forward flap!
            const float halfCylRatio = 24f / 38f;

            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1); // 0 = fairing base (meets lower tank), 1 = nosecone tip
                // Lower 36% is straight cylinder (exact width = halfCylRatio), upper 64% tapers smoothly into ogive nosecone!
                float cylT = ny < 0.36f ? 0f : (ny - 0.36f) / 0.64f;
                float bodyW = Mathf.Pow(1f - cylT * 0.94f, 0.56f) * halfCylRatio;

                // Forward SFS aero flap in upper nosecone section (ny 0.56 .. 0.86)
                bool inFlap = ny > 0.56f && ny < 0.86f;
                float flapOut = inFlap ? bodyW + Mathf.Lerp(0.32f, 0.14f, (ny - 0.56f) / 0.30f) : bodyW;

                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    float dSeam = isLeft ? (1f - nx) : nx; // 0 at center seam, 1 at outer edge

                    if (dSeam > flapOut)
                    {
                        px[y * w + x] = Color.clear;
                        continue;
                    }

                    bool isBorder = (y < 3) || (dSeam > flapOut - 0.065f) || (dSeam < 0.025f);
                    if (isBorder)
                    {
                        px[y * w + x] = outline;
                        continue;
                    }

                    // Horizontal fairing panel seam at cylinder-to-nosecone transition
                    bool panelSeam = Mathf.Abs(ny - 0.36f) < 0.012f;
                    if (panelSeam && dSeam < bodyW)
                    {
                        px[y * w + x] = new Color(0.28f, 0.31f, 0.36f, 1f);
                        continue;
                    }

                    // Clean SFS left-lit shading (matches MakeSfsCylinderSprite!)
                    float shade = isLeft ? 0.95f : (dSeam < bodyW * 0.52f ? 0.90f : 0.64f);
                    if (dSeam > bodyW) shade = 0.92f; // forward flap is crisp white-silver
                    px[y * w + x] = new Color(shade, shade * 0.99f, 1f, 1f);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }


        private static Sprite MakeSfsAdapterSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color outline = new Color(0.08f, 0.10f, 0.13f, 1f);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                float halfW = Mathf.Lerp(0.92f, 0.56f, ny);
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs((x / (float)(w - 1)) - 0.5f) * 2f;
                    if (dx > halfW)
                    {
                        px[y * w + x] = Color.clear;
                    }
                    else
                    {
                        bool border = y < 3 || y >= h - 3 || dx > halfW - 0.10f;
                        px[y * w + x] = border ? outline : new Color(0.32f, 0.35f, 0.40f, 1f);
                    }
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }


        private static Sprite MakeSfsFlapSprite(int w, int h, bool isLeft)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color outline = new Color(0.08f, 0.10f, 0.13f, 1f);
            Color[] px = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1); // 0 bottom .. 1 top
                float span = Mathf.Lerp(0.95f, 0.35f, ny);
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    float dInner = isLeft ? (1f - nx) : nx;
                    if (dInner > span)
                    {
                        px[y * w + x] = Color.clear;
                        continue;
                    }
                    bool isBorder = y < 3 || y >= h - 3 || dInner > span - 0.10f || dInner < 0.08f;
                    px[y * w + x] = isBorder ? outline : new Color(0.90f, 0.93f, 0.96f, 1f);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }


        private static Sprite MakeSfsNozzlesSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color outline = new Color(0.08f, 0.10f, 0.13f, 1f);
            Color bell = new Color(0.32f, 0.35f, 0.40f, 1f);
            Color[] px = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = x / (float)(w - 1);
                    int cell = Mathf.FloorToInt(nx * 5f);
                    float localX = Mathf.Abs((nx * 5f - cell) - 0.5f) * 2f;
                    float bellW = Mathf.Lerp(0.88f, 0.50f, ny);
                    if (localX > bellW)
                    {
                        px[y * w + x] = Color.clear;
                    }
                    else
                    {
                        bool border = y < 2 || y >= h - 2 || localX > bellW - 0.16f;
                        px[y * w + x] = border ? outline : bell;
                    }
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }


        private static Sprite MakeSfsCubeSatSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color outline = new Color(0.08f, 0.10f, 0.13f, 1f);
            Color[] px = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x < 3 || x >= w - 3 || y < 3 || y >= h - 3)
                    {
                        px[y * w + x] = outline;
                        continue;
                    }
                    // Camera lens in center
                    float dx = (x - w * 0.5f);
                    float dy = (y - h * 0.5f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist < 8f)
                    {
                        px[y * w + x] = dist > 5.5f ? outline : new Color(0.18f, 0.28f, 0.42f, 1f);
                    }
                    else
                    {
                        bool rail = (x < 8 || x >= w - 8);
                        bool seam = (y % 24 <= 1);
                        Color c = rail ? new Color(0.65f, 0.69f, 0.74f) : new Color(0.88f, 0.90f, 0.93f);
                        if (seam) c *= 0.78f;
                        c.a = 1f;
                        px[y * w + x] = c;
                    }
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }


        private static Sprite MakeSfsSolarWingSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color outline = new Color(0.08f, 0.10f, 0.13f, 1f);
            Color cell = new Color(0.16f, 0.34f, 0.64f, 1f);
            Color grid = new Color(0.55f, 0.70f, 0.88f, 1f);
            Color[] px = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x < 3 || x >= w - 3 || y < 3 || y >= h - 3)
                    {
                        px[y * w + x] = outline;
                    }
                    else if (x % 16 == 0 || y % 12 == 0)
                    {
                        px[y * w + x] = grid;
                    }
                    else
                    {
                        px[y * w + x] = cell;
                    }
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }


        private static Sprite MakeRingVfxSprite(int s)
        {
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color[] px = new Color[s * s];
            float c = s * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c)) / c;
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.62f) / 0.28f);
                    px[y * s + x] = new Color(1f, 1f, 1f, ring);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }

        private static Sprite MakePlumeSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = 1f - (y / (float)(h - 1));
                float halfW = Mathf.Lerp(0.45f, 0.05f, v) + Mathf.Sin(v * 22f) * 0.05f;
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs((x / (float)(w - 1)) - 0.5f) * 2f;
                    float radial = Mathf.Clamp01(1f - (dx / Mathf.Max(0.05f, halfW)));
                    float core = Mathf.Pow(radial, 1.8f) * (1f - v * 0.92f);
                    Color c = Color.Lerp(Color.white, new Color(1f, 0.75f, 0.35f), v);
                    px[y * w + x] = new Color(c.r, c.g, c.b, core);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f));
        }

        private static Sprite MakeSmokeSprite(int s)
        {
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color[] px = new Color[s * s];
            float r = s * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    float a = Mathf.Pow(Mathf.Clamp01(1f - d), 1.8f);
                    px[y * s + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }

        private static Sprite MakeVerticalGradientSprite()
        {
            Texture2D tex = new Texture2D(4, 128, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color[] px = new Color[4 * 128];
            Color horizonCol = new Color(0.58f, 0.80f, 1.00f, 1f); // SFS luminous sky-blue horizon
            Color midCol     = new Color(0.36f, 0.64f, 0.96f, 1f); // SFS mid sky
            Color zenithCol  = new Color(0.20f, 0.49f, 0.88f, 1f); // SFS clear cerulean zenith
            for (int y = 0; y < 128; y++)
            {
                float t = y / 127f;
                Color c = t < 0.45f
                    ? Color.Lerp(horizonCol, midCol, Mathf.SmoothStep(0f, 1f, t / 0.45f))
                    : Color.Lerp(midCol, zenithCol, Mathf.SmoothStep(0f, 1f, (t - 0.45f) / 0.55f));
                for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 128), new Vector2(0.5f, 0.5f));
        }

        private static Sprite MakeVaporConeSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1); // 0 = swept-back skirt, 1 = nose tip apex
                float coneW = Mathf.Lerp(0.95f, 0.08f, Mathf.Pow(ny, 0.85f));
                float vertFade = Mathf.Clamp01(ny / 0.25f) * Mathf.Clamp01((1f - ny) / 0.12f);
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs((x / (float)(w - 1)) - 0.5f) * 2f;
                    // Outer Mach shock line + soft condensation skirt inside
                    float shockLine = Mathf.Clamp01(1f - Mathf.Abs(dx - coneW) / 0.14f);
                    float vaporSkirt = (dx < coneW && ny < 0.65f) ? 0.35f * (1f - dx / Mathf.Max(0.1f, coneW)) : 0f;
                    float alpha = Mathf.Max(shockLine * 0.9f, vaporSkirt) * vertFade;
                    px[y * w + x] = new Color(0.94f, 0.98f, 1f, alpha);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private static Sprite MakeMoonSprite(int s)
        {
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color[] px = new Color[s * s];
            Vector3[] craters =
            {
                new Vector3(0.35f, 0.62f, 0.12f),
                new Vector3(0.62f, 0.48f, 0.15f),
                new Vector3(0.48f, 0.30f, 0.09f),
                new Vector3(0.28f, 0.38f, 0.07f),
                new Vector3(0.68f, 0.70f, 0.08f)
            };

            for (int y = 0; y < s; y++)
            {
                float ny = y / (float)(s - 1);
                for (int x = 0; x < s; x++)
                {
                    float nx = x / (float)(s - 1);
                    float dx = (nx - 0.5f) * 2f;
                    float dy = (ny - 0.5f) * 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > 0.96f)
                    {
                        px[y * s + x] = Color.clear;
                        continue;
                    }

                    // Sunlight from upper-left + subtle lunar maria shading
                    float sun = Mathf.Clamp01(0.55f + (-dx * 0.45f + dy * 0.35f));
                    float shade = Mathf.Lerp(0.45f, 0.88f, sun);
                    for (int c = 0; c < craters.Length; c++)
                    {
                        float cd = Mathf.Sqrt(Mathf.Pow(nx - craters[c].x, 2f) + Mathf.Pow(ny - craters[c].y, 2f));
                        if (cd < craters[c].z) shade -= 0.10f * (1f - cd / craters[c].z);
                    }

                    float aa = Mathf.Clamp01((0.96f - dist) * 22f);
                    px[y * s + x] = new Color(shade, shade, shade + 0.02f, aa);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }


        private static Sprite MakeStarTile(int s)
        {
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.DontSave };
            Color[] px = new Color[s * s];
            System.Random rng = new System.Random(42);
            for (int i = 0; i < 120; i++)
            {
                int sx = rng.Next(0, s);
                int sy = rng.Next(0, s);
                float b = 0.5f + (float)rng.NextDouble() * 0.5f;
                px[sy * s + sx] = new Color(0.9f, 0.95f, 1f, b);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }

        private static Sprite MakeHorizonArcSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (x / (float)(w - 1)) * 2f - 1f;
                    float curveY = 0.85f - nx * nx * 0.55f;
                    float a = Mathf.Clamp01((curveY - ny) * h * 0.4f);
                    px[y * w + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }


        private static RectTransform CreateGroup(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Image CreateImg(Transform parent, string name, Sprite sprite, Color col)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = col;
            img.raycastTarget = false;
            return img;
        }

        private static TMP_Text CreateTxt(Transform parent, string name, string text, int size, FontStyles style, Vector2 pos)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;
            RectTransform rt = t.rectTransform;
            rt.sizeDelta = new Vector2(960f, 34f);
            rt.anchoredPosition = pos;
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Builds a static SFS-style 2D Starship + Super Heavy stack for the Home Screen hero display.
        /// </summary>
        public static GameObject CreateStaticSfsRocketDisplay(Transform parent)
        {
            EnsureProceduralRocketSprites();

            GameObject hero = new GameObject("Theme_HeroRocket", typeof(RectTransform));
            hero.transform.SetParent(parent, false);
            RectTransform hrt = hero.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.11f, 0.45f);
            hrt.anchorMax = new Vector2(0.11f, 0.45f);
            hrt.sizeDelta = new Vector2(140f, 460f);
            hrt.localEulerAngles = new Vector3(0f, 0f, -6f);

            // Booster + nozzles + grid fins
            Image bImg = CreateImg(hrt, "Theme_HeroBooster", _boosterSprite, Color.white);
            bImg.rectTransform.sizeDelta = new Vector2(52f, 200f);
            bImg.rectTransform.anchoredPosition = new Vector2(0f, -95f);

            Image bNoz = CreateImg(hrt, "Theme_HeroNozzles", _nozzleRowSprite, Color.white);
            bNoz.rectTransform.sizeDelta = new Vector2(48f, 15f);
            bNoz.rectTransform.anchoredPosition = new Vector2(0f, -200f);

            Image hs = CreateImg(hrt, "Theme_HeroHotStage", null, new Color(0.10f, 0.12f, 0.15f, 1f));
            hs.rectTransform.sizeDelta = new Vector2(52f, 14f);
            hs.rectTransform.anchoredPosition = new Vector2(0f, 10f);

            Image gL = CreateImg(hrt, "Theme_HeroGridL", null, new Color(0.14f, 0.16f, 0.20f, 1f));
            gL.rectTransform.sizeDelta = new Vector2(18f, 12f);
            gL.rectTransform.anchoredPosition = new Vector2(-28f, -6f);

            Image gR = CreateImg(hrt, "Theme_HeroGridR", null, new Color(0.14f, 0.16f, 0.20f, 1f));
            gR.rectTransform.sizeDelta = new Vector2(18f, 12f);
            gR.rectTransform.anchoredPosition = new Vector2(28f, -6f);

            // Starship Upper Stage + Aft Flaps + Seamless Fairing Nosecone with Forward Flaps
            Image sImg = CreateImg(hrt, "Theme_HeroShip", _starshipBodySprite, Color.white);
            sImg.rectTransform.sizeDelta = new Vector2(52f, 80f);
            sImg.rectTransform.anchoredPosition = new Vector2(0f, 56f);

            Image aftL = CreateImg(hrt, "Theme_HeroAftL", _flapLeftSprite, Color.white);
            aftL.rectTransform.sizeDelta = new Vector2(20f, 48f);
            aftL.rectTransform.anchoredPosition = new Vector2(-34f, 40f);

            Image aftR = CreateImg(hrt, "Theme_HeroAftR", _flapRightSprite, Color.white);
            aftR.rectTransform.sizeDelta = new Vector2(20f, 48f);
            aftR.rectTransform.anchoredPosition = new Vector2(34f, 40f);

            Image nL = CreateImg(hrt, "Theme_HeroNoseL", _fairingLeftSprite, Color.white);
            nL.rectTransform.pivot = new Vector2(1f, 0.5f);
            nL.rectTransform.sizeDelta = new Vector2(41f, 122f);
            nL.rectTransform.anchoredPosition = new Vector2(0f, 154f);

            Image nR = CreateImg(hrt, "Theme_HeroNoseR", _fairingRightSprite, Color.white);
            nR.rectTransform.pivot = new Vector2(0f, 0.5f);
            nR.rectTransform.sizeDelta = new Vector2(41f, 122f);
            nR.rectTransform.anchoredPosition = new Vector2(0f, 154f);

            return hero;
        }
    }
}
