using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Normalized version of the BALL CONTACT gesture.
///
/// IMPORTANT:
///
/// This object now preserves TWO layers:
///
/// 1. NORMALIZED SHAPE
///    independent from absolute mouse movement scale.
///
/// 2. SOURCE INTENSITY
///    preserves how much and how quickly the player
///    actually moved during the contact gesture.
///
/// This allows us to later distinguish:
///
/// huge vertical flick
///
/// from
///
/// small vertical flick
///
/// even if both have exactly the same geometric shape.
/// </summary>
[Serializable]
public class NormalizedShotGesture
{
    // =========================================================
    // CONTACT CONTEXT
    // =========================================================

    public float chargeDuration;

    public float charge01;

    public Vector3 releaseAimForward;

    public Vector3 releaseAimRight;

    public Vector3 contactBallPosition;

    public float releaseToContactDuration;

    public float preReleaseWindow;

    // =========================================================
    // SOURCE GENERAL
    // =========================================================

    public float duration;

    public int sourceSampleCount;

    /// <summary>
    /// Largest absolute processed mouse extent.
    ///
    /// Kept mainly for debug / calibration.
    /// </summary>
    public float sourceSpatialScale;

    public bool hasMeaningfulGesture;

    // =========================================================
    // SOURCE DISPLACEMENT
    // =========================================================

    /// <summary>
    /// Actual processed start-to-end displacement.
    ///
    /// Unlike finalDisplacement below,
    /// this is NOT normalized.
    /// </summary>
    public Vector2 sourceFinalDisplacement;

    public float sourceHorizontalRange;

    public float sourceVerticalRange;

    // =========================================================
    // SOURCE DISTANCE
    // =========================================================

    public float sourceTotalDistance;

    public float sourceDirectDistance;

    // =========================================================
    // SOURCE SPEED
    // =========================================================

    public float sourceAverageSpeed;

    public float sourcePeakSpeed;

    public float sourcePeakHorizontalSpeed;

    public float sourcePeakVerticalSpeed;

    // =========================================================
    // NORMALIZED POINTS
    // =========================================================

    public List<NormalizedShotGesturePoint> points =
        new List<NormalizedShotGesturePoint>();

    // =========================================================
    // NORMALIZED DISTANCE
    // =========================================================

    public float totalDistance;

    public float directDistance;

    public Vector2 finalDisplacement;
}