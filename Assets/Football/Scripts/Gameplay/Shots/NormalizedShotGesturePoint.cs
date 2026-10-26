using System;
using UnityEngine;

/// <summary>
/// One point of the contact gesture.
///
/// It stores TWO representations:
///
/// SOURCE:
///     actual processed mouse-space movement.
///     Used for gameplay intensity.
///
/// NORMALIZED:
///     scale-independent gesture shape.
///     Used for direction, coherence and geometry.
///
/// This separation is critical:
///
/// source data answers:
///     "How much / how fast did the player move?"
///
/// normalized data answers:
///     "What shape / direction did the player draw?"
/// </summary>
[Serializable]
public struct NormalizedShotGesturePoint
{
    // =========================================================
    // TIME
    // =========================================================

    public float normalizedTime;

    // =========================================================
    // NORMALIZED SHAPE
    // =========================================================

    public Vector2 position;

    public Vector2 velocity;

    // =========================================================
    // SOURCE INTENSITY
    // =========================================================

    public Vector2 sourcePosition;

    public Vector2 sourceVelocity;

    // =========================================================
    // FULL CONSTRUCTOR
    // =========================================================

    public NormalizedShotGesturePoint(
        float normalizedTime,
        Vector2 position,
        Vector2 velocity,
        Vector2 sourcePosition,
        Vector2 sourceVelocity
    )
    {
        this.normalizedTime =
            normalizedTime;

        this.position =
            position;

        this.velocity =
            velocity;

        this.sourcePosition =
            sourcePosition;

        this.sourceVelocity =
            sourceVelocity;
    }

    // =========================================================
    // COMPATIBILITY CONSTRUCTOR
    // =========================================================

    /*
     * Kept so any older code that creates points
     * with three arguments still compiles.
     */
    public NormalizedShotGesturePoint(
        float normalizedTime,
        Vector2 position,
        Vector2 velocity
    )
    {
        this.normalizedTime =
            normalizedTime;

        this.position =
            position;

        this.velocity =
            velocity;

        this.sourcePosition =
            Vector2.zero;

        this.sourceVelocity =
            Vector2.zero;
    }
}