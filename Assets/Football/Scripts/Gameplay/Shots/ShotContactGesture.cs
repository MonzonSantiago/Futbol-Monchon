using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Complete input package captured around the exact moment
/// a shot reaches ball contact.
///
/// IMPORTANT:
///
/// This is NOT the whole LMB hold gesture.
///
/// The player may hold LMB for a long time while aiming.
/// That time determines POWER.
///
/// This object only contains the very short mouse gesture
/// around RELEASE -> BALL CONTACT.
///
/// Example:
///
///     25 ms before release
///     +
///     90 ms after release
///
/// With a 1000 Hz mouse this can still contain roughly
/// 100+ detailed mouse samples.
///
/// This short gesture is what will determine:
///
/// - lateral curve
/// - opening / closing
/// - vertical shape
/// - flick behaviour
///
/// WASD is never involved.
/// </summary>
[Serializable]
public class ShotContactGesture
{
    // =========================================================
    // RAW CONTACT SAMPLES
    // =========================================================

    public List<ShotGestureSample> samples =
        new List<ShotGestureSample>();

    // =========================================================
    // TIMING
    // =========================================================

    public double windowStartTimestamp;

    public double releaseTimestamp;

    public double contactTimestamp;

    /// <summary>
    /// Duration of the extracted contact window.
    /// </summary>
    public float windowDuration;

    /// <summary>
    /// How much time before mouse release was included.
    /// </summary>
    public float preReleaseWindow;

    /// <summary>
    /// Actual time between release and ball contact.
    /// </summary>
    public float releaseToContactDuration;

    // =========================================================
    // SHOT POWER
    // =========================================================

    /// <summary>
    /// How long LMB was held before release.
    /// </summary>
    public float chargeDuration;

    /// <summary>
    /// Charge converted to 0..1 using the configured
    /// full charge time.
    /// </summary>
    public float charge01;

    // =========================================================
    // AIM SNAPSHOT
    // =========================================================

    /// <summary>
    /// Horizontal shot direction at the exact moment
    /// LMB was released.
    ///
    /// Camera movement AFTER release does not modify this.
    /// </summary>
    public Vector3 releaseAimForward;

    public Vector3 releaseAimRight;

    // =========================================================
    // BALL
    // =========================================================

    /// <summary>
    /// Ball position at actual contact time.
    /// </summary>
    public Vector3 contactBallPosition;

    // =========================================================
    // RAW METRICS
    // =========================================================

    public Vector2 rawDisplacement;

    public float rawTravelDistance;

    public int capturedSampleCount;
}