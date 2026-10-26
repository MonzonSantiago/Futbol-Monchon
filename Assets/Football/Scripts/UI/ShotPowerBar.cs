using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual LMB shot charge bar.
///
/// Reads charge information directly from ShotAimContext.
///
/// IMPORTANT:
///
/// This component is VISUAL ONLY.
///
/// It does not:
///
/// - calculate shot power
/// - modify ShotIntent
/// - modify FootballBall
/// - read the mouse directly
///
/// ShotAimContext remains the source of truth.
/// </summary>
public class ShotPowerBar : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [Tooltip(
        "Player component that already controls LMB shot charge."
    )]
    public ShotAimContext shotContext;

    [Tooltip(
        "Complete visual container of the power bar."
    )]
    public GameObject barRoot;

    [Tooltip(
        "Image that visually fills from 0 to 100 percent."
    )]
    public Image fillImage;

    // =========================================================
    // DISPLAY
    // =========================================================

    [Header("Display")]

    [Tooltip(
        "Hide the bar when the player is not charging."
    )]
    public bool hideWhenNotCharging = true;

    [Tooltip(
        "Smooths the displayed value without changing real shot power."
    )]
    public bool smoothVisualFill = true;

    [Tooltip(
        "Higher values make the displayed bar react faster."
    )]
    public float visualFillSpeed = 20f;

    // =========================================================
    // OPTIONAL PULSE AT FULL POWER
    // =========================================================

    [Header("Full Charge Feedback")]

    public bool pulseAtFullCharge = true;

    [Tooltip(
        "Scale increase when the bar reaches full power."
    )]
    public float fullChargePulseScale = 1.06f;

    [Tooltip(
        "Pulse frequency."
    )]
    public float fullChargePulseSpeed = 8f;

    // =========================================================
    // INTERNAL
    // =========================================================

    private float displayedCharge;

    private Vector3 originalScale;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        FindReferences();

        originalScale =
            transform.localScale;

        if (
            fillImage != null
        )
        {
            fillImage.type =
                Image.Type.Filled;

            fillImage.fillMethod =
                Image.FillMethod.Horizontal;

            fillImage.fillOrigin =
                (int)Image.OriginHorizontal.Left;

            fillImage.fillAmount =
                0f;
        }

        if (
            hideWhenNotCharging &&
            barRoot != null
        )
        {
            barRoot.SetActive(
                false
            );
        }
    }

    private void Update()
    {
        FindReferences();

        if (
            shotContext == null ||
            fillImage == null
        )
        {
            return;
        }

        bool charging =
            shotContext.IsCharging;

        // -----------------------------------------------------
        // VISIBILITY
        // -----------------------------------------------------

        if (
            hideWhenNotCharging &&
            barRoot != null
        )
        {
            if (
                barRoot.activeSelf !=
                charging
            )
            {
                barRoot.SetActive(
                    charging
                );
            }
        }

        if (!charging)
        {
            displayedCharge =
                0f;

            fillImage.fillAmount =
                0f;

            transform.localScale =
                originalScale;

            return;
        }

        // -----------------------------------------------------
        // REAL CHARGE
        // -----------------------------------------------------

        float realCharge =
            Mathf.Clamp01(
                shotContext.CurrentCharge01
            );

        // -----------------------------------------------------
        // VISUAL CHARGE
        // -----------------------------------------------------

        if (smoothVisualFill)
        {
            displayedCharge =
                Mathf.MoveTowards(
                    displayedCharge,
                    realCharge,
                    visualFillSpeed *
                    Time.unscaledDeltaTime
                );
        }
        else
        {
            displayedCharge =
                realCharge;
        }

        fillImage.fillAmount =
            displayedCharge;

        // -----------------------------------------------------
        // FULL CHARGE PULSE
        // -----------------------------------------------------

        if (
            pulseAtFullCharge &&
            realCharge >= 0.999f
        )
        {
            float pulse =
                (
                    Mathf.Sin(
                        Time.unscaledTime *
                        fullChargePulseSpeed
                    ) +
                    1f
                ) *
                0.5f;

            float scale =
                Mathf.Lerp(
                    1f,
                    fullChargePulseScale,
                    pulse
                );

            transform.localScale =
                originalScale *
                scale;
        }
        else
        {
            transform.localScale =
                originalScale;
        }
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (shotContext == null)
        {
            ShotAimContext found =
                FindObjectOfType<ShotAimContext>();

            if (found != null)
            {
                shotContext =
                    found;
            }
        }
    }
}