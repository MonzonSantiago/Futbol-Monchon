using System;
using UnityEngine;

/// <summary>
/// Mathematical description of a contact gesture.
///
/// Contains TWO kinds of information:
///
/// 1. Geometric / normalized shape.
/// 2. Recent physical intent extracted from source mouse motion.
///
/// IMPORTANT:
///
/// This is NOT a catalogue of skills.
///
/// It only describes how the player moved the mouse
/// immediately before contact.
/// </summary>
[Serializable]
public class ShotGestureAnalysis
{
    // =========================================================
    // GENERAL NORMALIZED DATA
    // =========================================================

    public float duration;

    public Vector2 finalDisplacement;

    public float totalDistance;

    public float directDistance;

    public float averageSpeed;

    public float peakSpeed;

    // =========================================================
    // HORIZONTAL NORMALIZED SHAPE
    // =========================================================

    public float horizontalMin;
    public float horizontalMax;
    public float horizontalRange;

    public float horizontalPeakValue;
    public float horizontalPeakTime;

    public float horizontalFinalValue;

    public int horizontalDirectionChanges;

    // =========================================================
    // VERTICAL NORMALIZED SHAPE
    // =========================================================

    public float verticalMin;
    public float verticalMax;
    public float verticalRange;

    public float verticalPeakValue;
    public float verticalPeakTime;

    public float verticalFinalValue;

    public int verticalDirectionChanges;

    // =========================================================
    // SHAPE
    // =========================================================

    public float pathDeviation;

    public float pathDeviationTime;

    public float lateralDeviationSign;

    public float pathEfficiency;

    public float accumulatedTurning;

    public float openingClosingAmount;

    public float closureRatio;

    public float signedArea;

    // =========================================================
    // UNIVERSAL CONTACT INTENT
    // =========================================================

    /// <summary>
    /// Beginning of the recent coherent section,
    /// expressed as normalized gesture time.
    /// </summary>
    public float intentStartTime01;

    /// <summary>
    /// Duration in seconds of the coherent recent section.
    /// </summary>
    public float intentDuration;

    /// <summary>
    /// Source-space displacement during the selected
    /// recent coherent section.
    /// </summary>
    public Vector2 intentSourceDisplacement;

    /// <summary>
    /// Normalized 2D direction extracted from the
    /// recent coherent section.
    /// </summary>
    public Vector2 intentDirection;

    /// <summary>
    /// 0 = very contradictory / noisy.
    /// 1 = very coherent.
    ///
    /// This never decides a skill.
    /// It only measures confidence in the gesture.
    /// </summary>
    [Range(0f, 1f)]
    public float coherence01;

    // =========================================================
    // SOURCE SPEED METRICS
    // =========================================================

    public float intentAverageSpeed;

    public float intentPeakSpeed;

    public float intentAverageHorizontalSpeed;

    public float intentAverageVerticalSpeed;

    public float intentPeakHorizontalSpeed;

    public float intentPeakVerticalSpeed;

    public float intentHorizontalDisplacementSpeed;

    public float intentVerticalDisplacementSpeed;

    // =========================================================
    // VIOLENCE / INTENSITY
    // =========================================================

    /// <summary>
    /// Final continuous horizontal technical intensity.
    ///
    /// 0 = no meaningful horizontal action.
    /// 1 = maximum configured input intensity.
    /// </summary>
    [Range(0f, 1f)]
    public float horizontalViolence01;

    /// <summary>
    /// Final continuous vertical technical intensity.
    /// </summary>
    [Range(0f, 1f)]
    public float verticalViolence01;

    /// <summary>
    /// Signed horizontal intention:
    ///
    /// -1 = left
    ///  0 = no meaningful horizontal intention
    /// +1 = right
    /// </summary>
    public float horizontalDirection;

    /// <summary>
    /// Signed vertical intention:
    ///
    /// -1 = down
    ///  0 = no meaningful vertical intention
    /// +1 = up
    /// </summary>
    public float verticalDirection;

    /// <summary>
    /// Largest final axis intensity.
    /// Useful as general flick strength.
    /// </summary>
    [Range(0f, 1f)]
    public float overallViolence01;

    /// <summary>
    /// Raw source-speed metric representing how violently
    /// the recent gesture was executed.
    /// </summary>
    public float effectiveSourceSpeed;
}