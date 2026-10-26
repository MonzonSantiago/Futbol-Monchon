using UnityEngine;

/// <summary>
/// Shared mathematical functions for shot trajectories.
///
/// Both the physical ball and the debug target trajectory
/// use this class.
///
/// This avoids duplicating trajectory calculations.
/// </summary>
public static class ShotTrajectoryMath
{
    // =========================================================
    // HORIZONTAL PROFILE
    // =========================================================

    /// <summary>
    /// Evaluates the compact mouse-derived shot profile
    /// at progress 0..1.
    /// </summary>
    public static Vector2 EvaluateProfile(
        ShotIntent intent,
        float progress
    )
    {
        if (
            intent == null ||
            intent.profile == null ||
            intent.profile.Count == 0
        )
        {
            return Vector2.zero;
        }

        progress =
            Mathf.Clamp01(
                progress
            );

        if (
            progress <=
            intent.profile[0].normalizedTime
        )
        {
            return
                intent.profile[0].position;
        }

        int lastIndex =
            intent.profile.Count - 1;

        if (
            progress >=
            intent.profile[lastIndex]
                .normalizedTime
        )
        {
            return
                intent.profile[lastIndex]
                    .position;
        }

        for (
            int i = 1;
            i < intent.profile.Count;
            i++
        )
        {
            ShotIntentPoint current =
                intent.profile[i];

            if (
                current.normalizedTime <
                progress
            )
            {
                continue;
            }

            ShotIntentPoint previous =
                intent.profile[i - 1];

            float interval =
                current.normalizedTime -
                previous.normalizedTime;

            if (
                interval <=
                0.000001f
            )
            {
                return
                    current.position;
            }

            float localTime =
                (
                    progress -
                    previous.normalizedTime
                ) /
                interval;

            return Vector2.Lerp(
                previous.position,
                current.position,
                localTime
            );
        }

        return
            intent.profile[lastIndex]
                .position;
    }

    // =========================================================
    // HORIZONTAL DERIVATIVE
    // =========================================================

    /// <summary>
    /// Calculates how quickly the desired horizontal curve
    /// is changing at this point.
    ///
    /// In other words:
    ///
    /// not only:
    ///     "where should the ball be?"
    ///
    /// but also:
    ///     "toward which side should it already be moving?"
    ///
    /// This is what removes the delayed curve sensation.
    /// </summary>
    public static float EvaluateHorizontalDerivative(
        ShotIntent intent,
        float progress,
        float sampleStep = 0.025f
    )
    {
        float step =
            Mathf.Max(
                sampleStep,
                0.001f
            );

        float before =
            Mathf.Clamp01(
                progress -
                step
            );

        float after =
            Mathf.Clamp01(
                progress +
                step
            );

        float interval =
            after -
            before;

        if (
            interval <=
            0.000001f
        )
        {
            return 0f;
        }

        float beforeX =
            EvaluateProfile(
                intent,
                before
            ).x;

        float afterX =
            EvaluateProfile(
                intent,
                after
            ).x;

        return (
            afterX -
            beforeX
        ) /
        interval;
    }

    // =========================================================
    // BALLISTIC HEIGHT
    // =========================================================

    /// <summary>
    /// Natural ballistic height:
    ///
    /// y = initialVelocity * time
    ///     + 0.5 * gravity * time^2
    ///
    /// The result is relative to the shot origin.
    /// </summary>
    public static float EvaluateBallisticHeight(
        float initialVerticalVelocity,
        float time
    )
    {
        return
            initialVerticalVelocity *
            time +
            0.5f *
            Physics.gravity.y *
            time *
            time;
    }

    // =========================================================
    // VERTICAL LAUNCH
    // =========================================================

    /// <summary>
    /// Calculates the initial vertical velocity necessary
    /// to reach a desired apex height under normal gravity.
    ///
    /// v = sqrt(2 * g * height)
    /// </summary>
    public static float CalculateVerticalVelocityForHeight(
        float targetHeight
    )
    {
        targetHeight =
            Mathf.Max(
                targetHeight,
                0f
            );

        float gravity =
            Mathf.Abs(
                Physics.gravity.y
            );

        if (
            gravity <=
            0.0001f
        )
        {
            return 0f;
        }

        return Mathf.Sqrt(
            2f *
            gravity *
            targetHeight
        );
    }
}