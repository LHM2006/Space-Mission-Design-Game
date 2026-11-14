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
    /// [Member 2 - UI] Controller for Phase 4: Launch & On-Orbit Simulation (`OrbitVisualizer2D.cs`).
    /// 
    /// Directly integrates with Member 1's `SpacecraftDesign`, `MissionEvaluationResult`,
    /// and `MissionCalculator`:
    /// 1. Dynamically updates the 2D central planet (Earth LEO, Polar SSO, or Gray Moon) and CubeSat size (1U/3U/6U)
    ///    to match the parts equipped in `BuilderUI`!
    /// 2. Visualizes Day/Night Eclipse Shadow Zone and Ground Station Signal Cone.
    /// 3. Streams Member 1's `FlightSimulationLogs` step-by-step into the Flight Log Ticker.
    /// </summary>
    public class OrbitVisualizer2D : MonoBehaviour
    {
        [Header("1. 2D Visual Transforms (UI RectTransforms)")]
        [Tooltip("The central rotating planet (Earth or Moon)")]
        [SerializeField] private RectTransform centralBodyTransform;
        [SerializeField] private TMP_Text centralBodyLabel;

        [Tooltip("The orbiting CubeSat icon/sprite that revolves around the central body")]
        [SerializeField] private RectTransform cubeSatTransform;

        [Tooltip("Visual highlight shown when the CubeSat is inside the Ground Station Signal Cone")]
        [SerializeField] private GameObject groundSignalLinkBeam;

        [Tooltip("Visual indicator on the CubeSat when solar wings/antennas deploy via burn-wire")]
        [SerializeField] private GameObject deployedSolarWingsVisual;

        [Header("2. 2D Orbit Geometry & Speeds")]
        [SerializeField] private float orbitRadiusPixels = 148f;
        [SerializeField] private float planetRotationSpeedDegPerSec = 14f;
        [SerializeField] private float satelliteOrbitSpeedDegPerSec = 52f;

        [Header("3. GMAT Orbit Zone Angles (Degrees, 0 = Right, Counter-Clockwise)")]
        [SerializeField] private float eclipseStartAngleDeg = 135f;
        [SerializeField] private float eclipseEndAngleDeg = 225f;
        [SerializeField] private float groundConeStartAngleDeg = 290f;
        [SerializeField] private float groundConeEndAngleDeg = 340f;

        [Header("4. Live Telemetry & Flight Log UI")]
        [SerializeField] private TMP_Text missionStageHeaderText;
        [SerializeField] private TMP_Text orbitZoneStatusText;
        [SerializeField] private TMP_Text batteryChargeText;
        [SerializeField] private TMP_Text dataBufferAndDownlinkedText;
        [SerializeField] private TMP_Text flightLogTickerText;
        [SerializeField] private Button backToBuilderButton;
        [SerializeField] private Button viewScorecardButton;

        private bool _isOrbitingActive = false;
        private bool _survivesEclipseBattery = true;
        private float _dataGeneratedMbps = 45f;
        private float _downlinkCapacityMbps = 60f;
        private float _currentOrbitAngleDeg = 0f;
        private float _batteryChargePct = 100f;
        private float _onboardDataBufferMB = 0f;
        private float _totalDataDownlinkedMB = 0f;

        private bool _wasInEclipse = false;
        private bool _wasInGroundCone = false;

        private readonly Queue<string> _flightLogLines = new Queue<string>();
        private const int MaxVisibleLogLines = 8;
        private Coroutine _sequenceCoroutine;

        public event Action OnBackToBuilderRequested;
        public event Action OnProceedToScorecardRequested;

        private void Awake()
        {
            if (backToBuilderButton != null)
            {
                backToBuilderButton.onClick.RemoveAllListeners();
                backToBuilderButton.onClick.AddListener(() => OnBackToBuilderRequested?.Invoke());
            }

            if (viewScorecardButton != null)
            {
                viewScorecardButton.onClick.RemoveAllListeners();
                viewScorecardButton.onClick.AddListener(() => OnProceedToScorecardRequested?.Invoke());
                viewScorecardButton.gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            if (centralBodyTransform != null)
            {
                centralBodyTransform.Rotate(0f, 0f, planetRotationSpeedDegPerSec * Time.deltaTime);
            }

            if (!_isOrbitingActive || cubeSatTransform == null) return;

            _currentOrbitAngleDeg = (_currentOrbitAngleDeg + satelliteOrbitSpeedDegPerSec * Time.deltaTime) % 360f;
            UpdateCubeSatPositionOnOrbit(_currentOrbitAngleDeg);
            EvaluateOrbitZones(_currentOrbitAngleDeg);
        }

        /// <summary>
        /// Primary entry point for Phase 4 (`PhaseCD_OrbitSimulation`).
        /// Directly consumes Member 1's `SpacecraftDesign` and `MissionEvaluationResult`!
        /// </summary>
        public void StartSimulation(SpacecraftDesign design, MissionEvaluationResult eval = null)
        {
            if (design == null) design = new SpacecraftDesign();
            if (eval == null) eval = MissionCalculator.EvaluateMission(design);

            _isOrbitingActive = eval.IsLaunchApproved;
            _currentOrbitAngleDeg = 0f;
            _batteryChargePct = 100f;
            _onboardDataBufferMB = 0f;
            _totalDataDownlinkedMB = 0f;
            _wasInEclipse = false;
            _wasInGroundCone = false;
            _flightLogLines.Clear();

            _survivesEclipseBattery = eval.PowerMarginStatus != RYGStatus.Red;
            _dataGeneratedMbps = eval.DataGeneratedPerOrbitMb;
            _downlinkCapacityMbps = eval.DataDownlinkedPerOrbitMb;

            float sunlightFraction = MissionCalculator.GetOrbitSunlightFraction(design.SelectedOrbit);
            float eclipseHalfSpanDeg = (1f - sunlightFraction) * 180f;
            eclipseStartAngleDeg = 180f - eclipseHalfSpanDeg;
            eclipseEndAngleDeg = 180f + eclipseHalfSpanDeg;

            ApplyEquippedVisualsToOrbitArena(design);

            if (groundSignalLinkBeam != null) groundSignalLinkBeam.SetActive(false);
            if (deployedSolarWingsVisual != null) deployedSolarWingsVisual.SetActive(eval.IsLaunchApproved);
            if (viewScorecardButton != null) viewScorecardButton.gameObject.SetActive(true);

            UpdateCubeSatPositionOnOrbit(0f);
            UpdateLiveTelemetryReadouts("LAUNCH & ORBITAL INJECTION");

            if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = StartCoroutine(PlayMember1FlightSequence(design, eval));
        }

        private void ApplyEquippedVisualsToOrbitArena(SpacecraftDesign design)
        {
            string orbitId = design.SelectedOrbit != null ? design.SelectedOrbit.Id.ToUpperInvariant() : "ORBIT_SSO";
            string chassisId = design.SelectedChassis != null ? design.SelectedChassis.Id.ToUpperInvariant() : "CHASSIS_3U";

            if (centralBodyTransform != null)
            {
                Image planetImg = centralBodyTransform.GetComponent<Image>();
                if (planetImg != null)
                {
                    if (orbitId.Contains("LUNAR"))
                        planetImg.color = new Color(0.62f, 0.65f, 0.70f, 1f); // Gray Moon
                    else if (orbitId.Contains("SSO"))
                        planetImg.color = new Color(0.14f, 0.58f, 0.78f, 1f); // Polar SSO Earth
                    else
                        planetImg.color = new Color(0.16f, 0.45f, 0.88f, 1f); // ISS LEO Earth
                }

                if (centralBodyLabel != null)
                {
                    centralBodyLabel.text = orbitId.Contains("LUNAR")
                        ? "THE MOON\n(GMAT 2D)"
                        : (orbitId.Contains("SSO") ? "EARTH SSO\n(POLAR 2D)" : "EARTH LEO\n(ISS 400km)");
                }
            }

            if (cubeSatTransform != null)
            {
                if (chassisId.Contains("1U"))
                    cubeSatTransform.sizeDelta = new Vector2(30f, 30f);
                else if (chassisId.Contains("6U"))
                    cubeSatTransform.sizeDelta = new Vector2(58f, 38f);
                else
                    cubeSatTransform.sizeDelta = new Vector2(46f, 28f);
            }
        }

        private IEnumerator PlayMember1FlightSequence(SpacecraftDesign design, MissionEvaluationResult eval)
        {
            string orbitTitle = design.SelectedOrbit != null ? design.SelectedOrbit.Name : "Low Earth Orbit";
            SetStageHeader($"PHASE 4: 2D ORBIT SIMULATION - {orbitTitle}");

            if (!eval.IsLaunchApproved)
            {
                foreach (string log in eval.FlightSimulationLogs)
                {
                    AppendFlightLog($"<color=#E84C3D>{log}</color>");
                    yield return new WaitForSeconds(0.5f);
                }
                UpdateLiveTelemetryReadouts("SCRUBBED AT LAUNCH INTEGRATION (Click 'Back to Builder' or 'View Scorecard')");
                yield break;
            }

            for (int i = 0; i < eval.FlightSimulationLogs.Count; i++)
            {
                string log = eval.FlightSimulationLogs[i];

                if (log.Contains("STAGE 1"))
                {
                    SetStageHeader("STAGE 1/4: ROCKET ASCENT & MAX-Q VIBRATION");
                }
                else if (log.Contains("STAGE 2"))
                {
                    SetStageHeader("STAGE 2/4: DISPENSER EJECTION & 45-MIN DITL TIMER");
                    _isOrbitingActive = true;
                }
                else if (log.Contains("STAGE 3"))
                {
                    SetStageHeader("STAGE 3/4: BURN-WIRE SOLAR & ANTENNA DEPLOYMENT");
                    if (!log.Contains("FAILURE") && !log.Contains("JAM") && deployedSolarWingsVisual != null)
                    {
                        deployedSolarWingsVisual.SetActive(true);
                    }
                }
                else if (log.Contains("STAGE 4"))
                {
                    SetStageHeader($"STAGE 4/4: ON-ORBIT OPERATIONS ({orbitTitle})");
                }

                string coloredLog = FormatLogColor(log);
                AppendFlightLog(coloredLog);

                yield return new WaitForSeconds(0.75f);
            }

            if (eval.IsMissionSuccess)
            {
                AppendFlightLog("<color=#2ECC71>[SIMULATION READY] Live 2D orbit active! Click 'PROCEED TO NASA SCORECARD' whenever ready.</color>");
            }
            else
            {
                _isOrbitingActive = false;
                UpdateLiveTelemetryReadouts($"MISSION TERMINATED: {eval.FinalGrade}");
            }
        }

        private string FormatLogColor(string rawLog)
        {
            if (rawLog.Contains("FAILURE") || rawLog.Contains("SCRUBBED") || rawLog.Contains("BROWN-OUT") || rawLog.Contains("MISMATCH") || rawLog.Contains("JAM"))
                return $"<color=#E84C3D>{rawLog}</color>";
            if (rawLog.Contains("WARNING"))
                return $"<color=#F1C40F>{rawLog}</color>";
            return $"<color=#2ECC71>{rawLog}</color>";
        }

        private void UpdateCubeSatPositionOnOrbit(float angleDeg)
        {
            if (cubeSatTransform == null) return;

            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadiusPixels;

            if (centralBodyTransform != null)
            {
                cubeSatTransform.anchoredPosition = centralBodyTransform.anchoredPosition + offset;
            }
            else
            {
                cubeSatTransform.anchoredPosition = offset;
            }

            cubeSatTransform.localEulerAngles = new Vector3(0f, 0f, angleDeg + 90f);
        }

        private void EvaluateOrbitZones(float angleDeg)
        {
            bool inEclipse = (angleDeg >= eclipseStartAngleDeg && angleDeg <= eclipseEndAngleDeg);
            bool inGroundCone = (angleDeg >= groundConeStartAngleDeg && angleDeg <= groundConeEndAngleDeg);

            if (groundSignalLinkBeam != null)
            {
                groundSignalLinkBeam.SetActive(inGroundCone);
            }

            if (inEclipse && !_wasInEclipse)
            {
                AppendFlightLog("ORBIT | Entered Eclipse Shadow Zone. Running on Li-Ion battery...");
            }
            else if (!inEclipse && _wasInEclipse)
            {
                AppendFlightLog("ORBIT | Exited Eclipse into Sunlight. Solar array recharging battery.");
            }

            if (inGroundCone && !_wasInGroundCone)
            {
                AppendFlightLog("GROUND PASS | Acquired Ground Station signal cone! Downlinking science data...");
            }

            _wasInEclipse = inEclipse;
            _wasInGroundCone = inGroundCone;

            if (inEclipse)
            {
                float drainRate = _survivesEclipseBattery ? 10f : 42f;
                _batteryChargePct = Mathf.Max(0f, _batteryChargePct - drainRate * Time.deltaTime);
                UpdateLiveTelemetryReadouts("IN ECLIPSE SHADOW (BATTERY DISCHARGING)");
            }
            else
            {
                _batteryChargePct = Mathf.Min(100f, _batteryChargePct + 18f * Time.deltaTime);
                float collectRate = (_dataGeneratedMbps / 4f) * Time.deltaTime;
                _onboardDataBufferMB += collectRate;

                if (inGroundCone)
                {
                    float downlinkRate = (_downlinkCapacityMbps / 2f) * Time.deltaTime;
                    float transferred = Mathf.Min(_onboardDataBufferMB, downlinkRate);
                    _onboardDataBufferMB -= transferred;
                    _totalDataDownlinkedMB += transferred;
                    UpdateLiveTelemetryReadouts("SUNLIT + GROUND STATION DOWNLINK ACTIVE");
                }
                else
                {
                    UpdateLiveTelemetryReadouts("SUNLIT SCIENCE COLLECTION (SOLAR CHARGING)");
                }
            }
        }

        private void SetStageHeader(string header)
        {
            if (missionStageHeaderText != null)
            {
                missionStageHeaderText.text = header;
            }
        }

        private void UpdateLiveTelemetryReadouts(string zoneStatus)
        {
            if (orbitZoneStatusText != null)
            {
                orbitZoneStatusText.text = $"Status: {zoneStatus}";
            }

            if (batteryChargeText != null)
            {
                batteryChargeText.text = $"Battery SOC: {_batteryChargePct:F0}%";
            }

            if (dataBufferAndDownlinkedText != null)
            {
                dataBufferAndDownlinkedText.text =
                    $"Onboard Buffer: {_onboardDataBufferMB:F0} MB | Downlinked: {_totalDataDownlinkedMB:F0} MB";
            }
        }

        public void AppendFlightLog(string entry)
        {
            _flightLogLines.Enqueue(entry);
            while (_flightLogLines.Count > MaxVisibleLogLines)
            {
                _flightLogLines.Dequeue();
            }

            if (flightLogTickerText != null)
            {
                flightLogTickerText.text = string.Join("\n", _flightLogLines);
            }
        }
    }
}
