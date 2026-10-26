using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Converts the short raw BALL CONTACT mouse gesture
/// into a clean and uniformly sampled gameplay gesture.
///
/// Input:
///
/// ShotContactGesture
///
/// Output:
///
/// ProcessedShotGesture
///
/// ---------------------------------------------------------
///
/// IMPORTANT:
///
/// A player can shoot without making any flick.
///
/// In that case this processor still produces a valid
/// completely straight zero-offset gesture.
///
/// This is necessary because:
///
/// no mouse movement
///     !=
/// invalid shot
///
/// It means:
///
/// straight shot.
/// </summary>
public class ShotGestureProcessor : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public ShotAimContext shotContext;

    // =========================================================
    // RECONSTRUCTION
    // =========================================================

    [Header("Reconstruction")]

    [Range(32, 512)]
    public int processedPointCount = 128;

    // =========================================================
    // SMOOTHING
    // =========================================================

    [Header("Smoothing")]

    [Range(0, 8)]
    public int smoothingPasses = 2;

    [Range(1, 12)]
    public int smoothingRadius = 3;

    public bool preserveEndpoints = true;

    // =========================================================
    // DIRECTION CHANGES
    // =========================================================

    [Header("Direction Changes")]

    public float minimumRelevantAxisSpeed = 35f;

    public float minimumDirectionChangeDistance = 10f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    public bool logProcessedGestures = true;

    // =========================================================
    // RESULT
    // =========================================================

    private ProcessedShotGesture lastGesture;

    public ProcessedShotGesture LastGesture
    {
        get { return lastGesture; }
    }

    // =========================================================
    // EVENT
    // =========================================================

    public event Action<ProcessedShotGesture>
        ProcessedGestureReady;

    // =========================================================
    // BUFFERS
    // =========================================================

    private readonly List<Vector2> reconstructedPositions =
        new List<Vector2>();

    private readonly List<Vector2> smoothingBuffer =
        new List<Vector2>();

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

        if (shotContext != null)
        {
            shotContext.ContactGestureReady +=
                HandleContactGesture;
        }
    }

    private void OnDisable()
    {
        if (shotContext != null)
        {
            shotContext.ContactGestureReady -=
                HandleContactGesture;
        }
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (shotContext == null)
        {
            shotContext =
                GetComponent<ShotAimContext>();
        }
    }

    // =========================================================
    // EVENT
    // =========================================================

    private void HandleContactGesture(
        ShotContactGesture contact
    )
    {
        ProcessedShotGesture result =
            ProcessGesture(
                contact
            );

        if (result == null)
            return;

        lastGesture =
            result;

        if (logProcessedGestures)
        {
            Debug.Log(
                "[SHOT PROCESSOR CONTACT] " +
                "RAW: " +
                result.sourceSampleCount +
                " | Processed: " +
                result.points.Count +
                " | Window: " +
                result.duration.ToString("F4") +
                " s" +
                " | Charge01: " +
                result.charge01.ToString("F3") +
                " | Max X: " +
                result.maxHorizontalExcursion
                    .ToString("F2") +
                " | Max Y: " +
                result.maxVerticalExcursion
                    .ToString("F2") +
                " | Peak speed: " +
                result.peakGestureSpeed
                    .ToString("F2")
            );
        }

        ProcessedGestureReady?.Invoke(
            result
        );
    }

    // =========================================================
    // MAIN PROCESSING
    // =========================================================

    public ProcessedShotGesture ProcessGesture(
        ShotContactGesture contact
    )
    {
        if (contact == null)
            return null;

        int pointCount =
            Mathf.Clamp(
                processedPointCount,
                32,
                512
            );

        float duration =
            Mathf.Max(
                contact.windowDuration,
                0.001f
            );

        IReadOnlyList<ShotGestureSample> raw =
            contact.samples;

        // -----------------------------------------------------
        // RECONSTRUCT
        // -----------------------------------------------------

        ReconstructUniformly(
            raw,
            duration,
            pointCount
        );

        // -----------------------------------------------------
        // SMOOTH
        // -----------------------------------------------------

        for (
            int pass = 0;
            pass < smoothingPasses;
            pass++
        )
        {
            SmoothPositions();
        }

        // -----------------------------------------------------
        // RESULT
        // -----------------------------------------------------

        ProcessedShotGesture result =
            new ProcessedShotGesture();

        result.chargeDuration =
            contact.chargeDuration;

        result.charge01 =
            contact.charge01;

        result.releaseAimForward =
            contact.releaseAimForward;

        result.releaseAimRight =
            contact.releaseAimRight;

        result.contactBallPosition =
            contact.contactBallPosition;

        result.releaseToContactDuration =
            contact.releaseToContactDuration;

        result.preReleaseWindow =
            contact.preReleaseWindow;

        result.duration =
            duration;

        result.sourceSampleCount =
            raw != null
                ? raw.Count
                : 0;

        result.rawTravelDistance =
            contact.rawTravelDistance;

        result.rawDisplacement =
            contact.rawDisplacement;

        result.points.Clear();

        float deltaTime =
            duration /
            Mathf.Max(
                pointCount - 1,
                1
            );

        float maxHorizontal =
            0f;

        float maxVertical =
            0f;

        float peakSpeed =
            0f;

        for (
            int i = 0;
            i < pointCount;
            i++
        )
        {
            float normalizedTime =
                i /
                (float)(pointCount - 1);

            Vector2 position =
                reconstructedPositions[i];

            Vector2 velocity =
                CalculateVelocity(
                    i,
                    deltaTime
                );

            maxHorizontal =
                Mathf.Max(
                    maxHorizontal,
                    Mathf.Abs(
                        position.x
                    )
                );

            maxVertical =
                Mathf.Max(
                    maxVertical,
                    Mathf.Abs(
                        position.y
                    )
                );

            peakSpeed =
                Mathf.Max(
                    peakSpeed,
                    velocity.magnitude
                );

            result.points.Add(
                new ShotGesturePoint(
                    normalizedTime,
                    position,
                    velocity
                )
            );
        }

        result.maxHorizontalExcursion =
            maxHorizontal;

        result.maxVerticalExcursion =
            maxVertical;

        result.peakGestureSpeed =
            peakSpeed;

        result.horizontalDirectionChanges =
            CountDirectionChanges(
                result,
                true
            );

        result.verticalDirectionChanges =
            CountDirectionChanges(
                result,
                false
            );

        return result;
    }

    // =========================================================
    // RECONSTRUCTION
    // =========================================================

    private void ReconstructUniformly(
        IReadOnlyList<ShotGestureSample> raw,
        float duration,
        int pointCount
    )
    {
        reconstructedPositions.Clear();

        EnsureListSize(
            reconstructedPositions,
            pointCount
        );

        /*
         * NO FLICK:
         *
         * Generate a perfectly straight zero gesture.
         */
        if (
            raw == null ||
            raw.Count == 0
        )
        {
            for (
                int i = 0;
                i < pointCount;
                i++
            )
            {
                reconstructedPositions[i] =
                    Vector2.zero;
            }

            return;
        }

        int rawIndex =
            0;

        for (
            int i = 0;
            i < pointCount;
            i++
        )
        {
            float normalizedTime =
                i /
                (float)(pointCount - 1);

            float targetTime =
                normalizedTime *
                duration;

            reconstructedPositions[i] =
                EvaluateRawPositionAtTime(
                    raw,
                    targetTime,
                    ref rawIndex
                );
        }
    }

    // =========================================================
    // RAW INTERPOLATION
    // =========================================================

    private Vector2 EvaluateRawPositionAtTime(
        IReadOnlyList<ShotGestureSample> raw,
        float targetTime,
        ref int index
    )
    {
        if (
            raw == null ||
            raw.Count == 0
        )
        {
            return Vector2.zero;
        }

        if (
            targetTime <=
            raw[0].time
        )
        {
            if (
                raw[0].time <=
                0.000001f
            )
            {
                return
                    raw[0]
                    .cumulativePosition;
            }

            float amount =
                Mathf.Clamp01(
                    targetTime /
                    raw[0].time
                );

            return Vector2.Lerp(
                Vector2.zero,
                raw[0].cumulativePosition,
                amount
            );
        }

        while (
            index <
            raw.Count - 2 &&
            raw[index + 1].time <
            targetTime
        )
        {
            index++;
        }

        if (
            targetTime >=
            raw[raw.Count - 1].time
        )
        {
            return
                raw[raw.Count - 1]
                .cumulativePosition;
        }

        ShotGestureSample a =
            raw[index];

        ShotGestureSample b =
            raw[index + 1];

        float interval =
            b.time -
            a.time;

        if (
            interval <=
            0.000001f
        )
        {
            return
                b.cumulativePosition;
        }

        float t =
            Mathf.Clamp01(
                (
                    targetTime -
                    a.time
                ) /
                interval
            );

        return Vector2.Lerp(
            a.cumulativePosition,
            b.cumulativePosition,
            t
        );
    }

    // =========================================================
    // SMOOTH
    // =========================================================

    private void SmoothPositions()
    {
        int count =
            reconstructedPositions.Count;

        if (count < 3)
            return;

        EnsureListSize(
            smoothingBuffer,
            count
        );

        int radius =
            Mathf.Max(
                smoothingRadius,
                1
            );

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            if (
                preserveEndpoints &&
                (
                    i == 0 ||
                    i == count - 1
                )
            )
            {
                smoothingBuffer[i] =
                    reconstructedPositions[i];

                continue;
            }

            Vector2 sum =
                Vector2.zero;

            float weightSum =
                0f;

            int start =
                Mathf.Max(
                    0,
                    i - radius
                );

            int end =
                Mathf.Min(
                    count - 1,
                    i + radius
                );

            for (
                int j = start;
                j <= end;
                j++
            )
            {
                int distance =
                    Mathf.Abs(
                        j - i
                    );

                float weight =
                    radius +
                    1 -
                    distance;

                sum +=
                    reconstructedPositions[j] *
                    weight;

                weightSum +=
                    weight;
            }

            smoothingBuffer[i] =
                sum /
                Mathf.Max(
                    weightSum,
                    0.0001f
                );
        }

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            reconstructedPositions[i] =
                smoothingBuffer[i];
        }
    }

    // =========================================================
    // VELOCITY
    // =========================================================

    private Vector2 CalculateVelocity(
        int index,
        float deltaTime
    )
    {
        int count =
            reconstructedPositions.Count;

        if (
            count < 2 ||
            deltaTime <= 0.000001f
        )
        {
            return Vector2.zero;
        }

        if (index == 0)
        {
            return (
                reconstructedPositions[1] -
                reconstructedPositions[0]
            ) /
            deltaTime;
        }

        if (
            index ==
            count - 1
        )
        {
            return (
                reconstructedPositions[count - 1] -
                reconstructedPositions[count - 2]
            ) /
            deltaTime;
        }

        return (
            reconstructedPositions[index + 1] -
            reconstructedPositions[index - 1]
        ) /
        (
            2f *
            deltaTime
        );
    }

    // =========================================================
    // DIRECTION CHANGES
    // =========================================================

    private int CountDirectionChanges(
        ProcessedShotGesture gesture,
        bool horizontal
    )
    {
        if (
            gesture == null ||
            gesture.points == null ||
            gesture.points.Count < 3
        )
        {
            return 0;
        }

        int changes =
            0;

        int currentSign =
            0;

        float distanceSinceDirection =
            0f;

        float previousPosition =
            horizontal
                ? gesture.points[0]
                    .position.x
                : gesture.points[0]
                    .position.y;

        for (
            int i = 1;
            i < gesture.points.Count;
            i++
        )
        {
            ShotGesturePoint point =
                gesture.points[i];

            float position =
                horizontal
                    ? point.position.x
                    : point.position.y;

            float velocity =
                horizontal
                    ? point.velocity.x
                    : point.velocity.y;

            distanceSinceDirection +=
                Mathf.Abs(
                    position -
                    previousPosition
                );

            previousPosition =
                position;

            if (
                Mathf.Abs(velocity) <
                minimumRelevantAxisSpeed
            )
            {
                continue;
            }

            int sign =
                velocity > 0f
                    ? 1
                    : -1;

            if (
                currentSign == 0
            )
            {
                currentSign =
                    sign;

                distanceSinceDirection =
                    0f;

                continue;
            }

            if (
                sign != currentSign &&
                distanceSinceDirection >=
                minimumDirectionChangeDistance
            )
            {
                changes++;

                currentSign =
                    sign;

                distanceSinceDirection =
                    0f;
            }
        }

        return changes;
    }

    // =========================================================
    // BUFFER
    // =========================================================

    private void EnsureListSize(
        List<Vector2> list,
        int size
    )
    {
        while (
            list.Count <
            size
        )
        {
            list.Add(
                Vector2.zero
            );
        }

        if (
            list.Count >
            size
        )
        {
            list.RemoveRange(
                size,
                list.Count -
                size
            );
        }
    }
}