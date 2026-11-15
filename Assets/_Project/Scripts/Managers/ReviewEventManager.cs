using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceMissionGame.Core;
using SpaceMissionGame.Scenarios;

namespace SpaceMissionGame.Managers
{
    /// <summary>
    /// [Member 3 - HanMing] Manages Phase 3 (`CML4_ReviewAndTesting`):
    /// - Evaluates NASA CubeSat 101 Pre-Launch Licensing & Environmental Verification rules.
    /// - Presents interactive Engineering Dilemma Cards (Burn-Wire Redundancy, Outgassing Epoxy,
    ///   and DITL Flight Software Freeze) before rocket launch.
    /// - Automatically builds a clean 2D Dilemma Modal overlay on the existing Canvas if one isn't
    ///   manually wired in the scene, so it works out-of-the-box with Member 2's UI without scene conflicts!
    /// </summary>
    public class ReviewEventManager : MonoBehaviour
    {
        [Header("Standard NASA CubeSat 101 Verification Costs ($k)")]
        public const float VibrationTestCostThousands = 8f;
        public const float TvacBakeoutCostThousands = 6f;
        public const float DitlTestCostThousands = 4f;
        public const float DoubleBurnWireCostThousands = 3f;

        [Header("Runtime Dilemma Card State")]
        [SerializeField] private int currentDilemmaIndex = 0;

        // Track cumulative modifiers from Dilemma Cards separately from standard test toggles
        private float _dilemmaExtraCostThousands = 0f;
        private float _dilemmaExtraMassKg = 0f;
        private int _dilemmaReliabilityModifier = 0;

        // Runtime UI references for the CML 4 Engineering Dilemma Popup
        private GameObject _dilemmaModalRoot;
        private TMP_Text _cardStepBadgeText;
        private TMP_Text _cardTitleText;
        private TMP_Text _nasaRuleRefText;
        private TMP_Text _situationBodyText;
        private Button _optionAButton;
        private TMP_Text _optionAButtonText;
        private Button _optionBButton;
        private TMP_Text _optionBButtonText;
        private Button _skipToLaunchButton;

        private SpacecraftDesign _activeDesign;
        private List<DilemmaCard> _cardsQueue = new List<DilemmaCard>();
        private Action _onReviewSequenceCompleted;

        public event Action<DilemmaCard, bool> OnDilemmaDecisionMade;

        /// <summary>
        /// Resets dilemma modifiers when a player selects a new mission preset.
        /// </summary>
        public void ResetDilemmaState(SpacecraftDesign design)
        {
            _dilemmaExtraCostThousands = 0f;
            _dilemmaExtraMassKg = 0f;
            _dilemmaReliabilityModifier = 0;
            currentDilemmaIndex = 0;

            if (design != null)
            {
                design.ExtraMassKg = 0f;
                design.ExtraReliabilityModifier = 0;
                if (design.DecisionLogs != null) design.DecisionLogs.Clear();
                SyncTotalExtraCosts(design);
            }
        }

        /// <summary>
        /// Recalculates `design.ExtraTestingCostThousands` combining Member 2's test toggles
        /// and Member 3's Dilemma Card choices.
        /// </summary>
        public void SyncTotalExtraCosts(SpacecraftDesign design)
        {
            if (design == null) return;

            float toggleCosts = 0f;
            if (design.CompletedVibrationTest) toggleCosts += VibrationTestCostThousands;
            if (design.CompletedTvacBakeoutTest) toggleCosts += TvacBakeoutCostThousands;
            if (design.CompletedDitlSoftwareTest) toggleCosts += DitlTestCostThousands;
            if (design.UsedDoubleBurnWire) toggleCosts += DoubleBurnWireCostThousands;

            design.ExtraTestingCostThousands = toggleCosts + _dilemmaExtraCostThousands;
            design.ExtraMassKg = _dilemmaExtraMassKg;
            design.ExtraReliabilityModifier = _dilemmaReliabilityModifier;
        }

        /// <summary>
        /// Starts the interactive CML 4 Pre-Launch Review & Dilemma Card sequence.
        /// Once the player answers the dilemma card(s), `onComplete` fires to start Phase 4 Orbit Simulation.
        /// </summary>
        public void StartPreLaunchReviewSequence(SpacecraftDesign design, Action onComplete)
        {
            _activeDesign = design;
            _onReviewSequenceCompleted = onComplete;

            _cardsQueue = MissionScenariosData.GetAllDilemmaCards();
            if (_cardsQueue == null || _cardsQueue.Count == 0)
            {
                _onReviewSequenceCompleted?.Invoke();
                return;
            }

            // Reset for a fresh pre-launch review pass
            ResetDilemmaState(_activeDesign);

            EnsureDilemmaModalUIExists();
            currentDilemmaIndex = 0;
            ShowCurrentDilemmaCard();
        }

        public void HideDilemmaModal()
        {
            if (_dilemmaModalRoot != null)
            {
                _dilemmaModalRoot.SetActive(false);
            }
        }

        private void ShowCurrentDilemmaCard()
        {
            if (_cardsQueue == null || currentDilemmaIndex >= _cardsQueue.Count)
            {
                FinishReviewSequence();
                return;
            }

            DilemmaCard card = _cardsQueue[currentDilemmaIndex];
            _dilemmaModalRoot.SetActive(true);
            _dilemmaModalRoot.transform.SetAsLastSibling();

            if (_cardStepBadgeText != null)
            {
                _cardStepBadgeText.text =
                    $"JPL CML 4 PRE-LAUNCH REVIEW GATE — ENGINEERING DILEMMA ({currentDilemmaIndex + 1} / {_cardsQueue.Count})";
            }

            if (_cardTitleText != null) _cardTitleText.text = card.Title;
            if (_nasaRuleRefText != null) _nasaRuleRefText.text = $"NASA Reference: {card.NasaRuleReference}";
            if (_situationBodyText != null) _situationBodyText.text = card.SituationText;

            if (_optionAButtonText != null)
            {
                _optionAButtonText.text = $"[OPTION A — NASA STANDARD]\n{card.OptionAText}";
            }

            if (_optionBButtonText != null)
            {
                _optionBButtonText.text = $"[OPTION B — SCHEDULE / COST SHORTCUT]\n{card.OptionBText}";
            }

            if (_optionAButton != null)
            {
                _optionAButton.onClick.RemoveAllListeners();
                _optionAButton.onClick.AddListener(() => ApplyDilemmaChoice(card, true));
            }

            if (_optionBButton != null)
            {
                _optionBButton.onClick.RemoveAllListeners();
                _optionBButton.onClick.AddListener(() => ApplyDilemmaChoice(card, false));
            }
        }

        /// <summary>
        /// Applies the player's Option A or Option B decision to `SpacecraftDesign` and advances to the next card.
        /// </summary>
        public void ApplyDilemmaChoice(DilemmaCard card, bool choseOptionA)
        {
            if (_activeDesign == null || card == null) return;

            if (choseOptionA)
            {
                _dilemmaExtraCostThousands += card.OptionACostThousands;
                _dilemmaExtraMassKg += card.OptionAMassKg;
                _dilemmaReliabilityModifier += card.OptionAReliabilityBonus;

                if (!string.IsNullOrEmpty(card.OptionAOutcomeLog))
                {
                    _activeDesign.DecisionLogs.Add(card.OptionAOutcomeLog);
                }

                // Sync specific CubeSat 101 flags if the dilemma directly relates to them
                if (card.CardId == "DILEMMA_BURN_WIRE") _activeDesign.UsedDoubleBurnWire = true;
                if (card.CardId == "DILEMMA_DITL_FREEZE") _activeDesign.CompletedDitlSoftwareTest = true;
            }
            else
            {
                _dilemmaExtraCostThousands += card.OptionBCostThousands;
                _dilemmaExtraMassKg += card.OptionBMassKg;
                _dilemmaReliabilityModifier += card.OptionBReliabilityPenalty;

                if (!string.IsNullOrEmpty(card.OptionBOutcomeLog))
                {
                    _activeDesign.DecisionLogs.Add(card.OptionBOutcomeLog);
                }

                if (card.CardId == "DILEMMA_BURN_WIRE") _activeDesign.UsedDoubleBurnWire = false;
            }

            SyncTotalExtraCosts(_activeDesign);
            OnDilemmaDecisionMade?.Invoke(card, choseOptionA);

            currentDilemmaIndex++;
            ShowCurrentDilemmaCard();
        }

        private void FinishReviewSequence()
        {
            HideDilemmaModal();
            _onReviewSequenceCompleted?.Invoke();
        }

        /// <summary>
        /// Appends Member 3's `DecisionLogs` and NASA Chief Engineer educational advice into
        /// Member 1's `MissionEvaluationResult.FlightSimulationLogs` so they appear in Phase 4 & Phase 5!
        /// </summary>
        public void EnrichEvaluationWithScenarioLogs(SpacecraftDesign design, MissionEvaluationResult evaluation)
        {
            if (design == null || evaluation == null) return;

            if (design.DecisionLogs != null && design.DecisionLogs.Count > 0)
            {
                foreach (string log in design.DecisionLogs)
                {
                    if (!evaluation.FlightSimulationLogs.Contains(log))
                    {
                        evaluation.FlightSimulationLogs.Add(log);
                    }
                }
            }

            if (design.SelectedMission != null && !string.IsNullOrEmpty(design.SelectedMission.NasaReferenceSource))
            {
                string refLine = $"[NASA SOURCE] {design.SelectedMission.NasaReferenceSource}";
                if (!evaluation.FlightSimulationLogs.Contains(refLine))
                {
                    evaluation.FlightSimulationLogs.Add(refLine);
                }
            }
        }

        // =========================================================================
        // RUNTIME 2D MODAL GENERATOR (Zero Scene / Prefab Merge Conflicts!)
        // =========================================================================
        private void EnsureDilemmaModalUIExists()
        {
            if (_dilemmaModalRoot != null) return;

            Canvas targetCanvas = FindAnyObjectByType<Canvas>();
            if (targetCanvas == null) return;

            // Full-screen semi-transparent backdrop
            _dilemmaModalRoot = new GameObject("CML4_DilemmaModal_Overlay", typeof(RectTransform), typeof(Image));
            _dilemmaModalRoot.transform.SetParent(targetCanvas.transform, false);

            RectTransform overlayRect = _dilemmaModalRoot.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayImg = _dilemmaModalRoot.GetComponent<Image>();
            overlayImg.color = new Color(0.02f, 0.04f, 0.08f, 0.88f);

            // Center Card Box (920 x 540)
            GameObject cardBox = new GameObject("DilemmaCardBox", typeof(RectTransform), typeof(Image), typeof(Outline));
            cardBox.transform.SetParent(_dilemmaModalRoot.transform, false);

            RectTransform cardRect = cardBox.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(920f, 540f);

            Image cardImg = cardBox.GetComponent<Image>();
            cardImg.color = new Color(0.08f, 0.13f, 0.22f, 0.98f);

            Outline cardOutline = cardBox.GetComponent<Outline>();
            cardOutline.effectColor = new Color(0.24f, 0.68f, 0.96f, 0.9f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // 1. Top Badge
            _cardStepBadgeText = CreateModalText(
                cardBox.transform,
                "StepBadgeText",
                new Vector2(30f, -22f),
                new Vector2(860f, 32f),
                16,
                FontStyles.Bold,
                new Color(0.96f, 0.78f, 0.18f, 1f)
            );

            // 2. Dilemma Title
            _cardTitleText = CreateModalText(
                cardBox.transform,
                "TitleText",
                new Vector2(30f, -58f),
                new Vector2(860f, 42f),
                24,
                FontStyles.Bold,
                Color.white
            );

            // 3. NASA Rule Reference
            _nasaRuleRefText = CreateModalText(
                cardBox.transform,
                "NasaRuleRefText",
                new Vector2(30f, -102f),
                new Vector2(860f, 30f),
                15,
                FontStyles.Italic,
                new Color(0.45f, 0.82f, 0.98f, 1f)
            );

            // 4. Situation Description Body
            _situationBodyText = CreateModalText(
                cardBox.transform,
                "SituationBodyText",
                new Vector2(30f, -142f),
                new Vector2(860f, 130f),
                18,
                FontStyles.Normal,
                new Color(0.88f, 0.92f, 0.96f, 1f)
            );

            // 5. Option A Button (Green NASA Best Practice)
            _optionAButton = CreateModalButton(
                cardBox.transform,
                "OptionAButton",
                new Vector2(30f, -290f),
                new Vector2(860f, 82f),
                new Color(0.14f, 0.52f, 0.34f, 1f),
                out _optionAButtonText
            );

            // 6. Option B Button (Amber/Red Risky Shortcut)
            _optionBButton = CreateModalButton(
                cardBox.transform,
                "OptionBButton",
                new Vector2(30f, -386f),
                new Vector2(860f, 82f),
                new Color(0.55f, 0.26f, 0.16f, 1f),
                out _optionBButtonText
            );

            // 7. Skip Directly to Launch Button
            TMP_Text skipTxt;
            _skipToLaunchButton = CreateModalButton(
                cardBox.transform,
                "SkipReviewButton",
                new Vector2(640f, -482f),
                new Vector2(250f, 40f),
                new Color(0.20f, 0.28f, 0.40f, 1f),
                out skipTxt
            );
            skipTxt.text = "Skip Remaining Cards -> Launch";
            skipTxt.fontSize = 14;
            _skipToLaunchButton.onClick.AddListener(FinishReviewSequence);

            // Apply the same clean space theme (rounded card, pill buttons, font) to this popup
            SpaceMissionGame.UI.Theme.SpaceUITheme.StyleHierarchy(_dilemmaModalRoot.transform, false);
        }

        private TMP_Text CreateModalText(
            Transform parent,
            string name,
            Vector2 topLeftOffset,
            Vector2 size,
            float fontSize,
            FontStyles style,
            Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = topLeftOffset;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            return tmp;
        }

        private Button CreateModalButton(
            Transform parent,
            string name,
            Vector2 topLeftOffset,
            Vector2 size,
            Color bgColor,
            out TMP_Text labelText)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = topLeftOffset;
            rt.sizeDelta = size;

            Image img = btnGo.GetComponent<Image>();
            img.color = bgColor;

            GameObject txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);

            RectTransform txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(18f, 8f);
            txtRt.offsetMax = new Vector2(-18f, -8f);

            TextMeshProUGUI tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 17;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            labelText = tmp;
            return btnGo.GetComponent<Button>();
        }
    }
}
