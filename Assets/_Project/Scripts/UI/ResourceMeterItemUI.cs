using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpaceMissionGame.Core;

namespace SpaceMissionGame.UI
{
    /// <summary>
    /// [Member 2 - UI] Reusable UI component for a single live resource bar
    /// (e.g., Mass, Power, Budget, Data Volume, Science Return, or Technical Margin).
    /// 
    /// OOP Principle: Encapsulation & Reusability.
    /// Attach this script to each of the 6 resource bar objects on the Dashboard Canvas.
    /// </summary>
    public class ResourceMeterItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Displays the metric name, e.g., 'Mass (kg)' or 'Power (W)'")]
        [SerializeField] private TMP_Text metricTitleText;

        [Tooltip("Displays the numeric values, e.g., '3.2 / 4.0 kg (Margin: 20%)'")]
        [SerializeField] private TMP_Text valueReadoutText;

        [Tooltip("Displays the RYG status tag, e.g., 'GREEN' or 'RED'")]
        [SerializeField] private TMP_Text statusBadgeText;

        [Tooltip("The UI Image (set to Filled -> Horizontal) representing the progress bar")]
        [SerializeField] private Image fillBarImage;

        [Header("NASA CML 4 RYG Colors")]
        [SerializeField] private Color greenColor = new Color(0.18f, 0.80f, 0.44f);  // Compliant
        [SerializeField] private Color yellowColor = new Color(0.95f, 0.77f, 0.06f); // Marginal Risk
        [SerializeField] private Color redColor = new Color(0.91f, 0.30f, 0.24f);    // Critical Deficiency

        [Header("Animation Settings")]
        [SerializeField] private float fillSmoothSpeed = 8f;

        private float _targetFillNormalized = 0f;
        private Color _targetColor;
        private static Sprite _fallbackWhiteSprite;

        private void Awake()
        {
            _targetColor = greenColor;
            EnsureFillBarSpriteAndMode();
        }

        private void EnsureFillBarSpriteAndMode()
        {
            if (fillBarImage == null) return;

            // Unity UI Image.Type.Filled requires a non-null Sprite to respect fillAmount
            if (fillBarImage.sprite == null)
            {
                if (_fallbackWhiteSprite == null)
                {
                    Texture2D tex = Texture2D.whiteTexture;
                    _fallbackWhiteSprite = Sprite.Create(
                        tex,
                        new Rect(0f, 0f, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f));
                }
                fillBarImage.sprite = _fallbackWhiteSprite;
            }

            fillBarImage.type = Image.Type.Filled;
            fillBarImage.fillMethod = Image.FillMethod.Horizontal;
        }

        private void Update()
        {
            if (fillBarImage == null) return;

            fillBarImage.fillAmount = Mathf.Lerp(
                fillBarImage.fillAmount,
                _targetFillNormalized,
                Time.deltaTime * fillSmoothSpeed
            );

            fillBarImage.color = Color.Lerp(
                fillBarImage.color,
                _targetColor,
                Time.deltaTime * fillSmoothSpeed
            );
        }

        /// <summary>
        /// Updates this resource bar with new values from Member 1's MissionCalculator.
        /// </summary>
        public void SetMeterData(
            string title,
            float currentValue,
            float maxOrTargetValue,
            string unit,
            RYGStatus status,
            string extraNote = "")
        {
            EnsureFillBarSpriteAndMode();

            if (metricTitleText != null)
            {
                metricTitleText.text = title;
            }

            _targetFillNormalized = (maxOrTargetValue > 0.0001f)
                ? Mathf.Clamp01(currentValue / maxOrTargetValue)
                : 0f;

            if (valueReadoutText != null)
            {
                string baseNumbers = $"{currentValue:F1} / {maxOrTargetValue:F1} {unit}";
                valueReadoutText.text = string.IsNullOrEmpty(extraNote)
                    ? baseNumbers
                    : $"{baseNumbers} ({extraNote})";
            }

            ApplyRYGVisuals(status);
        }

        private void ApplyRYGVisuals(RYGStatus status)
        {
            switch (status)
            {
                case RYGStatus.Green:
                    _targetColor = greenColor;
                    if (statusBadgeText != null)
                    {
                        statusBadgeText.text = "GREEN";
                        statusBadgeText.color = greenColor;
                    }
                    break;

                case RYGStatus.Yellow:
                    _targetColor = yellowColor;
                    if (statusBadgeText != null)
                    {
                        statusBadgeText.text = "YELLOW";
                        statusBadgeText.color = yellowColor;
                    }
                    break;

                case RYGStatus.Red:
                    _targetColor = redColor;
                    if (statusBadgeText != null)
                    {
                        statusBadgeText.text = "RED";
                        statusBadgeText.color = redColor;
                    }
                    break;
            }
        }
    }
}
