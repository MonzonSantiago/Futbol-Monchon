using System;
using UnityEngine;

/// <summary>
/// Builds the physical contact intent.
///
/// IMPORTANT:
///
/// This class does NOT decide:
///
/// - chip
/// - curve shot
/// - dribble
/// - autopass
/// - volley
///
/// It only translates continuous player execution into
/// continuous physical parameters.
/// </summary>
public class ShotIntentBuilder : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public ShotGestureNormalizer normalizer;

    public ShotGestureAnalyzer analyzer;

    // =========================================================
    // PROFILE
    // =========================================================

    [Header("Profile")]

    [Range(16, 128)]
    public int profilePointCount = 64;

    // =========================================================
    // VERTICAL PRIORITY
    // =========================================================

    [Header("Vertical Priority")]

    [Tooltip(
        "If horizontal input is tiny compared to vertical, " +
        "reduce it slightly instead of deleting it."
    )]
    [Range(0f, 1f)]
    public float minorHorizontalRatio = 0.28f;

    [Range(0f, 1f)]
    public float minorHorizontalRetention = 0.55f;

    // =========================================================
    // CALIBRATION
    // =========================================================

    [Header("Calibration")]

    public bool invertHorizontalGesture = false;

    public bool invertVerticalGesture = false;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    public bool logShotIntent = true;

    // =========================================================
    // RESULT
    // =========================================================

    private ShotIntent lastIntent;

    public ShotIntent LastIntent
    {
        get
        {
            return lastIntent;
        }
    }

    public event Action<ShotIntent>
        ShotIntentReady;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        FindReferences();
    }

    private void OnEnable()
    {
        FindReferences();

        if (normalizer != null)
        {
            normalizer.NormalizedGestureReady +=
                HandleNormalizedGesture;
        }
    }

    private void OnDisable()
    {
        if (normalizer != null)
        {
            normalizer.NormalizedGestureReady -=
                HandleNormalizedGesture;
        }
    }

    private void FindReferences()
    {
        if (normalizer == null)
        {
            normalizer =
                GetComponent<ShotGestureNormalizer>();
        }

        if (analyzer == null)
        {
            analyzer =
                GetComponent<ShotGestureAnalyzer>();
        }
    }

    // =========================================================
    // EVENT
    // =========================================================

    private void HandleNormalizedGesture(
        NormalizedShotGesture gesture
    )
    {
        ShotIntent intent =
            BuildIntent(
                gesture
            );

        if (intent == null)
        {
            return;
        }

        lastIntent =
            intent;

        if (logShotIntent)
        {
            Debug.Log(
                "[CONTACT INTENT FINAL V3] " +

                "Power: " +
                intent.power01.ToString("F3") +

                " | Flick: " +
                intent.flickStrength01.ToString("F3") +

                " | Horizontal: " +
                intent.horizontalEffect01.ToString("F3") +

                " | Vertical: " +
                intent.verticalEffect01.ToString("F3") +

                " | V dir: " +
                intent.verticalDirection.ToString("F0") +

                " | Effective speed: " +
                intent.effectiveFlickSpeed.ToString("F0")
            );
        }

        ShotIntentReady?.Invoke(
            intent
        );
    }

    // =========================================================
    // BUILD
    // =========================================================

    public ShotIntent BuildIntent(
        NormalizedShotGesture gesture
    )
    {
        if (
            gesture == null ||
            gesture.points == null ||
            gesture.points.Count < 2
        )
        {
            return null;
        }

        ShotGestureAnalysis analysis =
            analyzer != null
                ? analyzer.Analyze(
                    gesture
                )
                : null;

        if (analysis == null)
        {
            return null;
        }

        ShotIntent intent =
            new ShotIntent();

        // =====================================================
        // CONTACT CONTEXT
        // =====================================================

        intent.aimForward =
            gesture.releaseAimForward;

        intent.aimRight =
            gesture.releaseAimRight;

        intent.contactBallPosition =
            gesture.contactBallPosition;

        intent.releaseToContactDuration =
            gesture.releaseToContactDuration;

        intent.chargeDuration =
            gesture.chargeDuration;

        intent.power01 =
            Mathf.Clamp01(
                gesture.charge01
            );

        // =====================================================
        // CONTINUOUS GESTURE INTENSITY
        // =====================================================

        float horizontal =
            analysis.horizontalViolence01;

        float vertical =
            analysis.verticalViolence01;

        float horizontalDirection =
            analysis.horizontalDirection;

        float verticalDirection =
            analysis.verticalDirection;

        if (invertHorizontalGesture)
        {
            horizontalDirection *=
                -1f;
        }

        if (invertVerticalGesture)
        {
            verticalDirection *=
                -1f;
        }

        // =====================================================
        // SMALL VERTICAL PRIORITY
        // =====================================================

        /*
         * Example:
         *
         * Player clearly flicks UP.
         * Tiny accidental X movement should still exist,
         * but it should not become a large curve.
         */
        if (
            vertical > 0.001f &&
            horizontal > 0.001f &&
            horizontal <
            vertical *
            minorHorizontalRatio
        )
        {
            horizontal *=
                minorHorizontalRetention;
        }

        // =====================================================
        // FINAL INTENT
        // =====================================================

        intent.horizontalEffect01 =
            Mathf.Clamp01(
                horizontal
            );

        intent.verticalEffect01 =
            Mathf.Clamp01(
                vertical
            );

        intent.verticalDirection =
            verticalDirection;

        intent.flickStrength01 =
            Mathf.Max(
                intent.horizontalEffect01,
                intent.verticalEffect01
            );

        intent.effectiveFlickSpeed =
            analysis.effectiveSourceSpeed;

        intent.hasFlick =
            gesture.hasMeaningfulGesture &&
            intent.flickStrength01 >
            0.001f;

        // =====================================================
        // LEGACY / DEBUG GEOMETRY
        // =====================================================

        intent.horizontalRange =
            analysis.horizontalRange;

        intent.verticalRange =
            analysis.verticalRange;

        intent.pathDeviation =
            analysis.pathDeviation;

        intent.pathEfficiency =
            analysis.pathEfficiency;

        intent.openingClosingAmount =
            analysis.openingClosingAmount;

        // =====================================================
        // INTENT PROFILE
        // =====================================================

        /*
         * Physics no longer follows the mouse drawing.
         *
         * The profile now only preserves a compact signed
         * representation of the contact intention.
         *
         * Horizontal sign is later used by FootballBall
         * to know which side should curve.
         */
        BuildIntentProfile(
            intent,
            horizontalDirection,
            verticalDirection
        );

        return intent;
    }

    // =========================================================
    // INTENT PROFILE
    // =========================================================

    private void BuildIntentProfile(
        ShotIntent intent,
        float horizontalDirection,
        float verticalDirection
    )
    {
        intent.profile.Clear();

        int count =
            Mathf.Clamp(
                profilePointCount,
                16,
                128
            );

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            float time =
                i /
                (float)(count - 1);

            Vector2 position =
                new Vector2(
                    horizontalDirection *
                    time,
                    verticalDirection *
                    time
                );

            intent.profile.Add(
                new ShotIntentPoint(
                    time,
                    position
                )
            );
        }
    }
}