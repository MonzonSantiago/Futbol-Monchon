using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Final gameplay description of a shot.
///
/// This is the boundary between:
///
/// PLAYER INPUT / TECHNIQUE
///
/// and
///
/// BALL PHYSICS / NETWORKING.
///
/// FootballBall does not need to know about:
///
/// - mouse hardware
/// - raw mouse events
/// - DPI
/// - LMB
/// - camera scripts
///
/// It only receives a ShotIntent.
///
/// This object is also designed to become the basis
/// of the future multiplayer shot command.
/// </summary>
[Serializable]
public class ShotIntent
{
    // =========================================================
    // AIM
    // =========================================================

    /// <summary>
    /// Horizontal world direction captured exactly
    /// when LMB was released.
    /// </summary>
    public Vector3 aimForward;

    public Vector3 aimRight;

    // =========================================================
    // CONTACT
    // =========================================================

    public Vector3 contactBallPosition;

    public float releaseToContactDuration;

    // =========================================================
    // POWER
    // =========================================================

    public float chargeDuration;

    /// <summary>
    /// 0 = minimum charge
    /// 1 = full charge
    /// </summary>
    public float power01;

    // =========================================================
    // FLICK
    // =========================================================

    public bool hasFlick;

    /// <summary>
    /// Global intensity of the contact flick.
    ///
    /// 0 = practically no flick
    /// 1 = strong flick according to calibration
    /// </summary>
    public float flickStrength01;

    /// <summary>
    /// Amount of horizontal effect requested.
    ///
    /// Magnitude only.
    /// The exact signed lateral shape is in profile.
    /// </summary>
    public float horizontalEffect01;

    /// <summary>
    /// Amount of vertical effect requested.
    ///
    /// Magnitude only.
    /// </summary>
    public float verticalEffect01;

    /// <summary>
    /// Dominant vertical direction of the contact gesture.
    ///
    /// +1 = upward flick
    ///  0 = no relevant vertical flick
    /// -1 = downward flick
    ///
    /// For now:
    /// upward flick -> higher shot
    /// downward flick -> low shot
    /// </summary>
    public float verticalDirection;

    /// <summary>
    /// Debug/calibration speed metric.
    /// </summary>
    public float effectiveFlickSpeed;

    // =========================================================
    // ANALYSIS
    // =========================================================

    public float horizontalRange;

    public float verticalRange;

    public float pathDeviation;

    public float pathEfficiency;

    public float openingClosingAmount;

    // =========================================================
    // SHAPE PROFILE
    // =========================================================

    /// <summary>
    /// Compact shape of the contact gesture.
    ///
    /// IMPORTANT:
    ///
    /// X and Y are shape channels.
    ///
    /// Their absolute strength is stored separately in:
    ///
    /// horizontalEffect01
    /// verticalEffect01
    ///
    /// This avoids applying gesture amplitude twice.
    /// </summary>
    public List<ShotIntentPoint> profile =
        new List<ShotIntentPoint>();
}