using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Processed version of the short BALL CONTACT gesture.
///
/// This contains both:
///
/// - cleaned gesture geometry
/// - shot context captured at release/contact
///
/// It still does NOT move the ball.
/// </summary>
[Serializable]
public class ProcessedShotGesture
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
    // SOURCE
    // =========================================================

    public float duration;

    public int sourceSampleCount;

    public float rawTravelDistance;

    public Vector2 rawDisplacement;

    // =========================================================
    // PROCESSED POINTS
    // =========================================================

    public List<ShotGesturePoint> points =
        new List<ShotGesturePoint>();

    // =========================================================
    // METRICS
    // =========================================================

    public float maxHorizontalExcursion;

    public float maxVerticalExcursion;

    public float peakGestureSpeed;

    public int horizontalDirectionChanges;

    public int verticalDirectionChanges;
}