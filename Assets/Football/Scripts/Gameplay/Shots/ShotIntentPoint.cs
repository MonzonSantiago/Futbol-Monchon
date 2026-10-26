using System;
using UnityEngine;

/// <summary>
/// One compact point of the final shot gesture profile.
///
/// This is NOT raw mouse input.
///
/// Thousands / hundreds of raw mouse events have already
/// participated before reaching this representation.
///
/// normalizedTime:
///     0 = beginning of contact gesture
///     1 = end of contact gesture
///
/// position.x:
///     lateral shape
///
/// position.y:
///     vertical shape
///
/// This representation is appropriate for gameplay and,
/// later, for multiplayer serialization.
/// </summary>
[Serializable]
public struct ShotIntentPoint
{
    public float normalizedTime;

    public Vector2 position;

    public ShotIntentPoint(
        float normalizedTime,
        Vector2 position
    )
    {
        this.normalizedTime =
            normalizedTime;

        this.position =
            position;
    }
}