using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the desired three-dimensional trajectory
/// generated from a player's mouse gesture.
///
/// IMPORTANT:
///
/// This is still NOT ball physics.
///
/// It is the ideal gameplay trajectory that the future
/// physical ball controller will attempt to reproduce.
///
/// The server will eventually be able to reconstruct
/// the same trajectory from a compact ShotCommand.
/// </summary>
[Serializable]
public class ShotTrajectoryProfile
{
    // =========================================================
    // GENERAL
    // =========================================================

    /// <summary>
    /// World-space origin of the trajectory.
    /// Normally this will later be the ball position
    /// at the moment of the shot.
    /// </summary>
    public Vector3 origin;

    /// <summary>
    /// Main aiming direction at release.
    /// </summary>
    public Vector3 forward;

    /// <summary>
    /// Local horizontal right axis.
    /// </summary>
    public Vector3 right;

    /// <summary>
    /// World vertical axis used by the trajectory.
    /// </summary>
    public Vector3 up;

    // =========================================================
    // DIMENSIONS
    // =========================================================

    /// <summary>
    /// Preview distance represented by this trajectory.
    ///
    /// This does NOT determine shot power yet.
    /// </summary>
    public float forwardDistance;

    /// <summary>
    /// Maximum lateral influence applied to the
    /// normalized mouse gesture.
    /// </summary>
    public float lateralScale;

    /// <summary>
    /// Maximum vertical influence applied to the
    /// normalized mouse gesture.
    /// </summary>
    public float verticalScale;

    // =========================================================
    // POINTS
    // =========================================================

    public List<Vector3> worldPoints =
        new List<Vector3>();
}