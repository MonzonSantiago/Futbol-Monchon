using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Converts a processed BALL CONTACT gesture into a
/// normalized geometric representation.
///
/// IMPORTANT:
///
/// This component now preserves:
///
/// SOURCE DATA
///     actual processed movement magnitude and speed.
///
/// NORMALIZED DATA
///     scale-independent shape.
///
/// We NEVER destroy source intensity anymore.
///
/// This allows later systems to combine:
///
/// player execution intensity
/// +
/// gesture shape / coherence
///
/// without confusing the two.
/// </summary>
public class ShotGestureNormalizer : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public ShotGestureProcessor processor;

    // =========================================================
    // NORMALIZATION
    // =========================================================

    [Header("Normalization")]

    [Tooltip(
        "Processed extent below this value is considered " +
        "a no-flick straight contact."
    )]
    public float meaningfulGestureExtent = 0.5f;

    public bool anchorGestureAtOrigin = true;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    public bool logNormalizedGestures = true;

    // =========================================================
    // RESULT
    // =========================================================

    private NormalizedShotGesture lastGesture;

    public NormalizedShotGesture LastGesture
    {
        get
        {
            return lastGesture;
        }
    }

    // =========================================================
    // EVENT
    // =========================================================

    public event Action<NormalizedShotGesture>
        NormalizedGestureReady;

    // =========================================================
    // BUFFERS
    // =========================================================

    private readonly List<Vector2>
        normalizedPositions =
            new List<Vector2>();

    private readonly List<Vector2>
        sourcePositions =
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

        if (processor != null)
        {
            processor.ProcessedGestureReady +=
                HandleProcessedGesture;
        }
    }

    private void OnDisable()
    {
        if (processor != null)
        {
            processor.ProcessedGestureReady -=
                HandleProcessedGesture;
        }
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (processor == null)
        {
            processor =
                GetComponent<ShotGestureProcessor>();
        }
    }

    // =========================================================
    // EVENT
    // =========================================================

    private void HandleProcessedGesture(
        ProcessedShotGesture source
    )
    {
        NormalizedShotGesture normalized =
            Normalize(
                source
            );

        if (normalized == null)
        {
            return;
        }

        lastGesture =
            normalized;

        if (logNormalizedGestures)
        {
            Debug.Log(
                "[CONTACT NORMALIZER V2] " +

                "Samples: " +
                normalized.sourceSampleCount +

                " | Meaningful: " +
                normalized.hasMeaningfulGesture +

                " | Source displacement: " +
                normalized.sourceFinalDisplacement +

                " | Source range X/Y: " +
                normalized.sourceHorizontalRange
                    .ToString("F1") +
                "/" +
                normalized.sourceVerticalRange
                    .ToString("F1") +

                " | Source avg speed: " +
                normalized.sourceAverageSpeed
                    .ToString("F1") +

                " | Source peak speed: " +
                normalized.sourcePeakSpeed
                    .ToString("F1") +

                " | Peak axis speed X/Y: " +
                normalized.sourcePeakHorizontalSpeed
                    .ToString("F1") +
                "/" +
                normalized.sourcePeakVerticalSpeed
                    .ToString("F1") +

                " | Normalized displacement: " +
                normalized.finalDisplacement
            );
        }

        NormalizedGestureReady?.Invoke(
            normalized
        );
    }

    // =========================================================
    // NORMALIZE
    // =========================================================

    public NormalizedShotGesture Normalize(
        ProcessedShotGesture source
    )
    {
        if (
            source == null ||
            source.points == null ||
            source.points.Count < 2
        )
        {
            return null;
        }

        int count =
            source.points.Count;

        EnsureVectorBufferSize(
            normalizedPositions,
            count
        );

        EnsureVectorBufferSize(
            sourcePositions,
            count
        );

        // =====================================================
        // SOURCE ORIGIN
        // =====================================================

        Vector2 originalOrigin =
            anchorGestureAtOrigin
                ? source.points[0].position
                : Vector2.zero;

        // =====================================================
        // SOURCE POSITIONS
        // =====================================================

        float largestExtent =
            0f;

        float minX =
            float.PositiveInfinity;

        float maxX =
            float.NegativeInfinity;

        float minY =
            float.PositiveInfinity;

        float maxY =
            float.NegativeInfinity;

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            Vector2 relative =
                source.points[i].position -
                originalOrigin;

            sourcePositions[i] =
                relative;

            largestExtent =
                Mathf.Max(
                    largestExtent,
                    Mathf.Abs(
                        relative.x
                    ),
                    Mathf.Abs(
                        relative.y
                    )
                );

            minX =
                Mathf.Min(
                    minX,
                    relative.x
                );

            maxX =
                Mathf.Max(
                    maxX,
                    relative.x
                );

            minY =
                Mathf.Min(
                    minY,
                    relative.y
                );

            maxY =
                Mathf.Max(
                    maxY,
                    relative.y
                );
        }

        bool meaningful =
            largestExtent >=
            meaningfulGestureExtent;

        // =====================================================
        // NORMALIZED SHAPE
        // =====================================================

        if (meaningful)
        {
            for (
                int i = 0;
                i < count;
                i++
            )
            {
                normalizedPositions[i] =
                    sourcePositions[i] /
                    Mathf.Max(
                        largestExtent,
                        0.0001f
                    );
            }
        }
        else
        {
            /*
             * No meaningful flick.
             *
             * Normalized shape becomes perfectly straight,
             * but SOURCE data is still preserved.
             */
            for (
                int i = 0;
                i < count;
                i++
            )
            {
                normalizedPositions[i] =
                    Vector2.zero;
            }
        }

        // =====================================================
        // CREATE RESULT
        // =====================================================

        NormalizedShotGesture result =
            new NormalizedShotGesture();

        // =====================================================
        // COPY CONTACT CONTEXT
        // =====================================================

        result.chargeDuration =
            source.chargeDuration;

        result.charge01 =
            source.charge01;

        result.releaseAimForward =
            source.releaseAimForward;

        result.releaseAimRight =
            source.releaseAimRight;

        result.contactBallPosition =
            source.contactBallPosition;

        result.releaseToContactDuration =
            source.releaseToContactDuration;

        result.preReleaseWindow =
            source.preReleaseWindow;

        // =====================================================
        // SOURCE GENERAL
        // =====================================================

        result.duration =
            source.duration;

        result.sourceSampleCount =
            source.sourceSampleCount;

        result.sourceSpatialScale =
            largestExtent;

        result.hasMeaningfulGesture =
            meaningful;

        result.sourceHorizontalRange =
            maxX - minX;

        result.sourceVerticalRange =
            maxY - minY;

        // =====================================================
        // DELTA TIME
        // =====================================================

        float deltaTime =
            source.duration /
            Mathf.Max(
                count - 1,
                1
            );

        // =====================================================
        // POINTS + SOURCE SPEED
        // =====================================================

        result.points.Clear();

        float peakSpeed =
            0f;

        float peakHorizontalSpeed =
            0f;

        float peakVerticalSpeed =
            0f;

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            float normalizedTime =
                i /
                (float)(count - 1);

            Vector2 normalizedVelocity =
                CalculateVelocity(
                    normalizedPositions,
                    i,
                    deltaTime
                );

            Vector2 sourceVelocity =
                CalculateVelocity(
                    sourcePositions,
                    i,
                    deltaTime
                );

            peakSpeed =
                Mathf.Max(
                    peakSpeed,
                    sourceVelocity.magnitude
                );

            peakHorizontalSpeed =
                Mathf.Max(
                    peakHorizontalSpeed,
                    Mathf.Abs(
                        sourceVelocity.x
                    )
                );

            peakVerticalSpeed =
                Mathf.Max(
                    peakVerticalSpeed,
                    Mathf.Abs(
                        sourceVelocity.y
                    )
                );

            result.points.Add(
                new NormalizedShotGesturePoint(
                    normalizedTime,
                    normalizedPositions[i],
                    normalizedVelocity,
                    sourcePositions[i],
                    sourceVelocity
                )
            );
        }

        result.sourcePeakSpeed =
            peakSpeed;

        result.sourcePeakHorizontalSpeed =
            peakHorizontalSpeed;

        result.sourcePeakVerticalSpeed =
            peakVerticalSpeed;

        // =====================================================
        // SOURCE DISTANCES
        // =====================================================

        CalculateSourceDistances(
            result
        );

        // =====================================================
        // NORMALIZED DISTANCES
        // =====================================================

        CalculateNormalizedDistances(
            result
        );

        return result;
    }

    // =========================================================
    // GENERIC VELOCITY
    // =========================================================

    private Vector2 CalculateVelocity(
        List<Vector2> positions,
        int index,
        float deltaTime
    )
    {
        int count =
            positions.Count;

        if (
            count < 2 ||
            deltaTime <=
            0.000001f
        )
        {
            return Vector2.zero;
        }

        if (index == 0)
        {
            return (
                positions[1] -
                positions[0]
            ) /
            deltaTime;
        }

        if (
            index ==
            count - 1
        )
        {
            return (
                positions[count - 1] -
                positions[count - 2]
            ) /
            deltaTime;
        }

        return (
            positions[index + 1] -
            positions[index - 1]
        ) /
        (
            2f *
            deltaTime
        );
    }

    // =========================================================
    // SOURCE DISTANCE
    // =========================================================

    private void CalculateSourceDistances(
        NormalizedShotGesture gesture
    )
    {
        if (
            gesture.points == null ||
            gesture.points.Count < 2
        )
        {
            return;
        }

        float total =
            0f;

        for (
            int i = 1;
            i < gesture.points.Count;
            i++
        )
        {
            total +=
                Vector2.Distance(
                    gesture.points[i - 1]
                        .sourcePosition,
                    gesture.points[i]
                        .sourcePosition
                );
        }

        Vector2 start =
            gesture.points[0]
                .sourcePosition;

        Vector2 end =
            gesture.points[
                gesture.points.Count - 1
            ].sourcePosition;

        gesture.sourceTotalDistance =
            total;

        gesture.sourceDirectDistance =
            Vector2.Distance(
                start,
                end
            );

        gesture.sourceFinalDisplacement =
            end -
            start;

        gesture.sourceAverageSpeed =
            total /
            Mathf.Max(
                gesture.duration,
                0.0001f
            );
    }

    // =========================================================
    // NORMALIZED DISTANCE
    // =========================================================

    private void CalculateNormalizedDistances(
        NormalizedShotGesture gesture
    )
    {
        float total =
            0f;

        for (
            int i = 1;
            i < gesture.points.Count;
            i++
        )
        {
            total +=
                Vector2.Distance(
                    gesture.points[i - 1]
                        .position,
                    gesture.points[i]
                        .position
                );
        }

        Vector2 start =
            gesture.points[0]
                .position;

        Vector2 end =
            gesture.points[
                gesture.points.Count - 1
            ].position;

        gesture.totalDistance =
            total;

        gesture.directDistance =
            Vector2.Distance(
                start,
                end
            );

        gesture.finalDisplacement =
            end -
            start;
    }

    // =========================================================
    // BUFFER
    // =========================================================

    private void EnsureVectorBufferSize(
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