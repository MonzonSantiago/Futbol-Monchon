using System;
using UnityEngine;

/// <summary>
/// Analyzes the contact gesture.
///
/// DESIGN PRINCIPLE:
///
/// The system does NOT recognize predefined skills.
///
/// Instead it measures:
///
/// - direction
/// - violence
/// - coherence
/// - recent intent
/// - normalized gesture geometry
///
/// The final football behavior is produced later by physics.
/// </summary>
public class ShotGestureAnalyzer : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public ShotGestureNormalizer normalizer;

    // =========================================================
    // OLD SHAPE ANALYSIS
    // =========================================================

    [Header("Shape Analysis")]

    public float minimumRelevantAxisSpeed = 0.12f;

    public float minimumDirectionChangeDistance = 0.04f;

    // =========================================================
    // RECENT INTENT
    // =========================================================

    [Header("Recent Intent")]

    [Tooltip(
        "Fraction at the END of the gesture used to seed " +
        "the current contact direction."
    )]
    [Range(0.15f, 0.75f)]
    public float directionSeedFraction = 0.35f;

    [Tooltip(
        "Minimum source-space speed considered meaningful " +
        "when finding the recent contact intention."
    )]
    public float minimumSourceSpeed = 120f;

    [Tooltip(
        "Minimum directional dot product still considered " +
        "part of the same recent intention."
    )]
    [Range(-0.25f, 0.9f)]
    public float coherentDirectionDot = 0.15f;

    [Tooltip(
        "Small contradictory samples allowed before ending " +
        "the recent coherent section."
    )]
    [Range(0, 12)]
    public int allowedContradictorySamples = 3;

    [Tooltip(
        "Recent movement weighs more than older movement."
    )]
    [Range(0.25f, 4f)]
    public float recencyWeightExponent = 1.5f;

    // =========================================================
    // FLICK VIOLENCE
    // =========================================================

    [Header("Flick Violence")]

    [Tooltip(
        "Axis speed below this produces almost no technical effect."
    )]
    public float axisMinimumSpeed = 350f;

    [Tooltip(
        "Axis speed around this value reaches maximum input effect."
    )]
    public float axisFullSpeed = 5500f;

    [Range(0f, 1f)]
    public float averageSpeedWeight = 0.55f;

    [Range(0f, 1f)]
    public float peakSpeedWeight = 0.30f;

    [Range(0f, 1f)]
    public float displacementSpeedWeight = 0.15f;

    [Tooltip(
        "Prevents one isolated mouse spike from creating " +
        "an absurd maximum flick."
    )]
    public float peakToAverageClamp = 2.5f;

    [Tooltip(
        "Below 1 makes medium flicks more expressive."
    )]
    [Range(0.35f, 2f)]
    public float violenceResponseExponent = 0.75f;

    // =========================================================
    // MICRO ERROR PROTECTION
    // =========================================================

    [Header("Micro Error Protection")]

    [Tooltip(
        "Axis travel below this is treated as human micro-noise."
    )]
    public float minimumAxisTravel = 5f;

    [Tooltip(
        "At this axis travel the gesture receives full " +
        "distance confidence. Speed can still dominate."
    )]
    public float fullAxisTravelSupport = 35f;

    [Tooltip(
        "Even a short but violent flick keeps this percentage " +
        "of its speed-derived effect."
    )]
    [Range(0f, 1f)]
    public float shortFlickSpeedRetention = 0.65f;

    // =========================================================
    // COHERENCE
    // =========================================================

    [Header("Coherence")]

    [Tooltip(
        "Even creative curved gestures should not disappear " +
        "because they are not perfectly straight."
    )]
    [Range(0f, 1f)]
    public float minimumCoherenceInfluence = 0.60f;

    // =========================================================
    // DIRECTION DEADZONE
    // =========================================================

    [Header("Direction")]

    [Range(0f, 0.5f)]
    public float directionComponentDeadZone = 0.08f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    public bool logAnalysis = true;

    // =========================================================
    // RESULT
    // =========================================================

    private ShotGestureAnalysis lastAnalysis;

    public ShotGestureAnalysis LastAnalysis
    {
        get
        {
            return lastAnalysis;
        }
    }

    public event Action<ShotGestureAnalysis>
        AnalysisReady;

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

        if (normalizer != null)
        {
            normalizer.NormalizedGestureReady +=
                HandleNormalizedGesture;
        }
    }

    private void OnDisable()
    {
        if (normalizer != null)
        {
            normalizer.NormalizedGestureReady -=
                HandleNormalizedGesture;
        }
    }

    private void FindReferences()
    {
        if (normalizer == null)
        {
            normalizer =
                GetComponent<ShotGestureNormalizer>();
        }
    }

    // =========================================================
    // EVENT
    // =========================================================

    private void HandleNormalizedGesture(
        NormalizedShotGesture gesture
    )
    {
        ShotGestureAnalysis analysis =
            Analyze(
                gesture
            );

        if (analysis == null)
        {
            return;
        }

        lastAnalysis =
            analysis;

        if (logAnalysis)
        {
            Debug.Log(
                "[CONTACT INTENT V3] " +

                "Dir: " +
                analysis.intentDirection +

                " | H dir: " +
                analysis.horizontalDirection.ToString("F0") +

                " | V dir: " +
                analysis.verticalDirection.ToString("F0") +

                " | H violence: " +
                analysis.horizontalViolence01.ToString("F3") +

                " | V violence: " +
                analysis.verticalViolence01.ToString("F3") +

                " | Overall: " +
                analysis.overallViolence01.ToString("F3") +

                " | Coherence: " +
                analysis.coherence01.ToString("F3") +

                " | Segment: " +
                analysis.intentDuration.ToString("F3") +
                " s" +

                " | Source displacement: " +
                analysis.intentSourceDisplacement +

                " | Avg axis speed X/Y: " +
                analysis.intentAverageHorizontalSpeed
                    .ToString("F0") +
                "/" +
                analysis.intentAverageVerticalSpeed
                    .ToString("F0") +

                " | Peak axis speed X/Y: " +
                analysis.intentPeakHorizontalSpeed
                    .ToString("F0") +
                "/" +
                analysis.intentPeakVerticalSpeed
                    .ToString("F0")
            );
        }

        AnalysisReady?.Invoke(
            analysis
        );
    }

    // =========================================================
    // MAIN ANALYSIS
    // =========================================================

    public ShotGestureAnalysis Analyze(
        NormalizedShotGesture gesture
    )
    {
        if (
            gesture == null ||
            gesture.points == null ||
            gesture.points.Count < 2
        )
        {
            return null;
        }

        ShotGestureAnalysis result =
            new ShotGestureAnalysis();

        result.duration =
            gesture.duration;

        result.finalDisplacement =
            gesture.finalDisplacement;

        result.totalDistance =
            gesture.totalDistance;

        result.directDistance =
            gesture.directDistance;

        result.averageSpeed =
            gesture.totalDistance /
            Mathf.Max(
                gesture.duration,
                0.0001f
            );

        CalculateRanges(
            gesture,
            result
        );

        CalculatePeakSpeed(
            gesture,
            result
        );

        CalculateDirectionChanges(
            gesture,
            result
        );

        CalculatePathEfficiency(
            gesture,
            result
        );

        CalculatePathDeviation(
            gesture,
            result
        );

        CalculateTurning(
            gesture,
            result
        );

        CalculateOpeningClosing(
            gesture,
            result
        );

        CalculateClosure(
            gesture,
            result
        );

        CalculateSignedArea(
            gesture,
            result
        );

        CalculateRecentIntent(
            gesture,
            result
        );

        return result;
    }

    // =========================================================
    // RECENT UNIVERSAL INTENT
    // =========================================================

    private void CalculateRecentIntent(
        NormalizedShotGesture gesture,
        ShotGestureAnalysis result
    )
    {
        int count =
            gesture.points.Count;

        int lastActiveIndex =
            FindLastActiveIndex(
                gesture
            );

        if (lastActiveIndex <= 0)
        {
            ClearIntent(
                result
            );

            return;
        }

        Vector2 seedDirection =
            CalculateSeedDirection(
                gesture,
                lastActiveIndex
            );

        if (
            seedDirection.sqrMagnitude <
            0.000001f
        )
        {
            ClearIntent(
                result
            );

            return;
        }

        seedDirection.Normalize();

        int segmentStart =
            FindRecentCoherentStart(
                gesture,
                lastActiveIndex,
                seedDirection
            );

        segmentStart =
            Mathf.Clamp(
                segmentStart,
                0,
                lastActiveIndex - 1
            );

        float startTime01 =
            gesture.points[segmentStart]
                .normalizedTime;

        float endTime01 =
            gesture.points[lastActiveIndex]
                .normalizedTime;

        float segmentDuration =
            Mathf.Max(
                (
                    endTime01 -
                    startTime01
                ) *
                gesture.duration,
                0.0001f
            );

        Vector2 startPosition =
            gesture.points[segmentStart]
                .sourcePosition;

        Vector2 endPosition =
            gesture.points[lastActiveIndex]
                .sourcePosition;

        Vector2 displacement =
            endPosition -
            startPosition;

        float totalWeight =
            0f;

        Vector2 weightedDirectionSum =
            Vector2.zero;

        float weightedAverageSpeed =
            0f;

        float weightedAverageX =
            0f;

        float weightedAverageY =
            0f;

        float peakSpeed =
            0f;

        float peakX =
            0f;

        float peakY =
            0f;

        float horizontalTravel =
            0f;

        float verticalTravel =
            0f;

        Vector2 previousPosition =
            gesture.points[segmentStart]
                .sourcePosition;

        for (
            int i = segmentStart;
            i <= lastActiveIndex;
            i++
        )
        {
            NormalizedShotGesturePoint point =
                gesture.points[i];

            Vector2 velocity =
                point.sourceVelocity;

            float speed =
                velocity.magnitude;

            float relativeTime =
                Mathf.InverseLerp(
                    startTime01,
                    Mathf.Max(
                        endTime01,
                        startTime01 +
                        0.0001f
                    ),
                    point.normalizedTime
                );

            float recencyWeight =
                Mathf.Pow(
                    Mathf.Lerp(
                        0.35f,
                        1f,
                        relativeTime
                    ),
                    Mathf.Max(
                        recencyWeightExponent,
                        0.01f
                    )
                );

            if (
                speed >=
                minimumSourceSpeed &&
                velocity.sqrMagnitude >
                0.000001f
            )
            {
                Vector2 direction =
                    velocity.normalized;

                weightedDirectionSum +=
                    direction *
                    recencyWeight;

                weightedAverageSpeed +=
                    speed *
                    recencyWeight;

                weightedAverageX +=
                    Mathf.Abs(
                        velocity.x
                    ) *
                    recencyWeight;

                weightedAverageY +=
                    Mathf.Abs(
                        velocity.y
                    ) *
                    recencyWeight;

                totalWeight +=
                    recencyWeight;

                peakSpeed =
                    Mathf.Max(
                        peakSpeed,
                        speed
                    );

                peakX =
                    Mathf.Max(
                        peakX,
                        Mathf.Abs(
                            velocity.x
                        )
                    );

                peakY =
                    Mathf.Max(
                        peakY,
                        Mathf.Abs(
                            velocity.y
                        )
                    );
            }

            if (i > segmentStart)
            {
                Vector2 delta =
                    point.sourcePosition -
                    previousPosition;

                horizontalTravel +=
                    Mathf.Abs(
                        delta.x
                    );

                verticalTravel +=
                    Mathf.Abs(
                        delta.y
                    );
            }

            previousPosition =
                point.sourcePosition;
        }

        if (totalWeight <= 0.0001f)
        {
            ClearIntent(
                result
            );

            return;
        }

        float averageSpeed =
            weightedAverageSpeed /
            totalWeight;

        float averageX =
            weightedAverageX /
            totalWeight;

        float averageY =
            weightedAverageY /
            totalWeight;

        float coherence =
            Mathf.Clamp01(
                weightedDirectionSum.magnitude /
                totalWeight
            );

        Vector2 velocityDirection =
            weightedDirectionSum.sqrMagnitude >
            0.000001f
                ? weightedDirectionSum.normalized
                : Vector2.zero;

        Vector2 displacementDirection =
            displacement.sqrMagnitude >
            0.000001f
                ? displacement.normalized
                : velocityDirection;

        Vector2 finalDirection =
            velocityDirection * 0.70f +
            displacementDirection * 0.30f;

        if (
            finalDirection.sqrMagnitude >
            0.000001f
        )
        {
            finalDirection.Normalize();
        }

        float horizontalDisplacementSpeed =
            Mathf.Abs(
                displacement.x
            ) /
            segmentDuration;

        float verticalDisplacementSpeed =
            Mathf.Abs(
                displacement.y
            ) /
            segmentDuration;

        float safePeakX =
            Mathf.Min(
                peakX,
                Mathf.Max(
                    averageX *
                    peakToAverageClamp,
                    averageX
                )
            );

        float safePeakY =
            Mathf.Min(
                peakY,
                Mathf.Max(
                    averageY *
                    peakToAverageClamp,
                    averageY
                )
            );

        float horizontalSpeedMetric =
            CombineSpeedMetrics(
                averageX,
                safePeakX,
                horizontalDisplacementSpeed
            );

        float verticalSpeedMetric =
            CombineSpeedMetrics(
                averageY,
                safePeakY,
                verticalDisplacementSpeed
            );

        float horizontalViolence =
            CalculateAxisViolence(
                horizontalSpeedMetric,
                horizontalTravel,
                coherence
            );

        float verticalViolence =
            CalculateAxisViolence(
                verticalSpeedMetric,
                verticalTravel,
                coherence
            );

        float horizontalDirection =
            Mathf.Abs(
                finalDirection.x
            ) >=
            directionComponentDeadZone
                ? Mathf.Sign(
                    finalDirection.x
                )
                : 0f;

        float verticalDirection =
            Mathf.Abs(
                finalDirection.y
            ) >=
            directionComponentDeadZone
                ? Mathf.Sign(
                    finalDirection.y
                )
                : 0f;

        /*
         * If an axis ended up with no meaningful direction,
         * its technical intensity should not survive.
         */
        if (horizontalDirection == 0f)
        {
            horizontalViolence =
                0f;
        }

        if (verticalDirection == 0f)
        {
            verticalViolence =
                0f;
        }

        result.intentStartTime01 =
            startTime01;

        result.intentDuration =
            segmentDuration;

        result.intentSourceDisplacement =
            displacement;

        result.intentDirection =
            finalDirection;

        result.coherence01 =
            coherence;

        result.intentAverageSpeed =
            averageSpeed;

        result.intentPeakSpeed =
            peakSpeed;

        result.intentAverageHorizontalSpeed =
            averageX;

        result.intentAverageVerticalSpeed =
            averageY;

        result.intentPeakHorizontalSpeed =
            safePeakX;

        result.intentPeakVerticalSpeed =
            safePeakY;

        result.intentHorizontalDisplacementSpeed =
            horizontalDisplacementSpeed;

        result.intentVerticalDisplacementSpeed =
            verticalDisplacementSpeed;

        result.horizontalViolence01 =
            horizontalViolence;

        result.verticalViolence01 =
            verticalViolence;

        result.horizontalDirection =
            horizontalDirection;

        result.verticalDirection =
            verticalDirection;

        result.overallViolence01 =
            Mathf.Max(
                horizontalViolence,
                verticalViolence
            );

        result.effectiveSourceSpeed =
            Mathf.Max(
                horizontalSpeedMetric,
                verticalSpeedMetric
            );
    }

    // =========================================================
    // LAST ACTIVE SAMPLE
    // =========================================================

    private int FindLastActiveIndex(
        NormalizedShotGesture gesture
    )
    {
        for (
            int i = gesture.points.Count - 1;
            i >= 0;
            i--
        )
        {
            if (
                gesture.points[i]
                    .sourceVelocity
                    .magnitude >=
                minimumSourceSpeed
            )
            {
                return i;
            }
        }

        return -1;
    }

    // =========================================================
    // SEED DIRECTION
    // =========================================================

    private Vector2 CalculateSeedDirection(
        NormalizedShotGesture gesture,
        int lastIndex
    )
    {
        int count =
            gesture.points.Count;

        int seedCount =
            Mathf.Max(
                3,
                Mathf.RoundToInt(
                    count *
                    directionSeedFraction
                )
            );

        int start =
            Mathf.Max(
                0,
                lastIndex -
                seedCount +
                1
            );

        Vector2 sum =
            Vector2.zero;

        float totalWeight =
            0f;

        for (
            int i = start;
            i <= lastIndex;
            i++
        )
        {
            Vector2 velocity =
                gesture.points[i]
                    .sourceVelocity;

            float speed =
                velocity.magnitude;

            if (
                speed <
                minimumSourceSpeed ||
                velocity.sqrMagnitude <
                0.000001f
            )
            {
                continue;
            }

            float t =
                Mathf.InverseLerp(
                    start,
                    Mathf.Max(
                        lastIndex,
                        start + 1
                    ),
                    i
                );

            float weight =
                Mathf.Lerp(
                    0.35f,
                    1f,
                    t
                );

            /*
             * Faster recent samples influence the seed
             * somewhat more, without letting one spike dominate.
             */
            float speedWeight =
                Mathf.Sqrt(
                    Mathf.Max(
                        speed /
                        Mathf.Max(
                            minimumSourceSpeed,
                            1f
                        ),
                        1f
                    )
                );

            speedWeight =
                Mathf.Min(
                    speedWeight,
                    4f
                );

            float finalWeight =
                weight *
                speedWeight;

            sum +=
                velocity.normalized *
                finalWeight;

            totalWeight +=
                finalWeight;
        }

        if (totalWeight <= 0.0001f)
        {
            return Vector2.zero;
        }

        return sum /
            totalWeight;
    }

    // =========================================================
    // COHERENT START
    // =========================================================

    private int FindRecentCoherentStart(
        NormalizedShotGesture gesture,
        int lastIndex,
        Vector2 seedDirection
    )
    {
        int start =
            lastIndex;

        int contradictions =
            0;

        for (
            int i = lastIndex;
            i >= 0;
            i--
        )
        {
            Vector2 velocity =
                gesture.points[i]
                    .sourceVelocity;

            float speed =
                velocity.magnitude;

            if (
                speed <
                minimumSourceSpeed ||
                velocity.sqrMagnitude <
                0.000001f
            )
            {
                /*
                 * Quiet samples do not immediately break
                 * the intention.
                 */
                start =
                    i;

                continue;
            }

            float alignment =
                Vector2.Dot(
                    velocity.normalized,
                    seedDirection
                );

            if (
                alignment <
                coherentDirectionDot
            )
            {
                contradictions++;

                if (
                    contradictions >
                    allowedContradictorySamples
                )
                {
                    break;
                }
            }
            else
            {
                contradictions =
                    0;

                start =
                    i;
            }
        }

        return start;
    }

    // =========================================================
    // SPEED METRIC
    // =========================================================

    private float CombineSpeedMetrics(
        float average,
        float peak,
        float displacementSpeed
    )
    {
        float totalConfiguredWeight =
            averageSpeedWeight +
            peakSpeedWeight +
            displacementSpeedWeight;

        if (
            totalConfiguredWeight <=
            0.0001f
        )
        {
            return average;
        }

        return (
            average *
            averageSpeedWeight +
            peak *
            peakSpeedWeight +
            displacementSpeed *
            displacementSpeedWeight
        ) /
        totalConfiguredWeight;
    }

    // =========================================================
    // AXIS VIOLENCE
    // =========================================================

    private float CalculateAxisViolence(
        float speedMetric,
        float axisTravel,
        float coherence
    )
    {
        if (
            axisTravel <
            minimumAxisTravel
        )
        {
            return 0f;
        }

        float speed01 =
            Mathf.InverseLerp(
                axisMinimumSpeed,
                Mathf.Max(
                    axisFullSpeed,
                    axisMinimumSpeed +
                    1f
                ),
                speedMetric
            );

        speed01 =
            Mathf.Clamp01(
                speed01
            );

        speed01 =
            Mathf.Pow(
                speed01,
                Mathf.Max(
                    violenceResponseExponent,
                    0.01f
                )
            );

        float distanceSupport =
            Mathf.InverseLerp(
                minimumAxisTravel,
                Mathf.Max(
                    fullAxisTravelSupport,
                    minimumAxisTravel +
                    0.01f
                ),
                axisTravel
            );

        distanceSupport =
            Mathf.Clamp01(
                distanceSupport
            );

        /*
         * Distance is SUPPORT, not the main source of power.
         *
         * This is what allows:
         *
         * short + violent = strong flick
         *
         * without requiring the player to cross the mouse pad.
         */
        float distanceMultiplier =
            Mathf.Lerp(
                shortFlickSpeedRetention,
                1f,
                distanceSupport
            );

        float coherenceMultiplier =
            Mathf.Lerp(
                minimumCoherenceInfluence,
                1f,
                Mathf.Clamp01(
                    coherence
                )
            );

        return Mathf.Clamp01(
            speed01 *
            distanceMultiplier *
            coherenceMultiplier
        );
    }

    // =========================================================
    // CLEAR INTENT
    // =========================================================

    private void ClearIntent(
        ShotGestureAnalysis result
    )
    {
        result.intentStartTime01 =
            1f;

        result.intentDuration =
            0f;

        result.intentSourceDisplacement =
            Vector2.zero;

        result.intentDirection =
            Vector2.zero;

        result.coherence01 =
            0f;

        result.intentAverageSpeed =
            0f;

        result.intentPeakSpeed =
            0f;

        result.intentAverageHorizontalSpeed =
            0f;

        result.intentAverageVerticalSpeed =
            0f;

        result.intentPeakHorizontalSpeed =
            0f;

        result.intentPeakVerticalSpeed =
            0f;

        result.intentHorizontalDisplacementSpeed =
            0f;

        result.intentVerticalDisplacementSpeed =
            0f;

        result.horizontalViolence01 =
            0f;

        result.verticalViolence01 =
            0f;

        result.horizontalDirection =
            0f;

        result.verticalDirection =
            0f;

        result.overallViolence01 =
            0f;

        result.effectiveSourceSpeed =
            0f;
    }

    // =========================================================
    // NORMALIZED RANGES
    // =========================================================

    private void CalculateRanges(
        NormalizedShotGesture gesture,
        ShotGestureAnalysis result
    )
    {
        float minX =
            float.PositiveInfinity;

        float maxX =
            float.NegativeInfinity;

        float minY =
            float.PositiveInfinity;

        float maxY =
            float.NegativeInfinity;

        float strongestAbsoluteX =
            -1f;

        float strongestAbsoluteY =
            -1f;

        for (
            int i = 0;
            i < gesture.points.Count;
            i++
        )
        {
            NormalizedShotGesturePoint point =
                gesture.points[i];

            float x =
                point.position.x;

            float y =
                point.position.y;

            minX =
                Mathf.Min(
                    minX,
                    x
                );

            maxX =
                Mathf.Max(
                    maxX,
                    x
                );

            minY =
                Mathf.Min(
                    minY,
                    y
                );

            maxY =
                Mathf.Max(
                    maxY,
                    y
                );

            float absoluteX =
                Mathf.Abs(
                    x
                );

            if (
                absoluteX >
                strongestAbsoluteX
            )
            {
                strongestAbsoluteX =
                    absoluteX;

                result.horizontalPeakValue =
                    x;

                result.horizontalPeakTime =
                    point.normalizedTime;
            }

            float absoluteY =
                Mathf.Abs(
                    y
                );

            if (
                absoluteY >
                strongestAbsoluteY
            )
            {
                strongestAbsoluteY =
                    absoluteY;

                result.verticalPeakValue =
                    y;

                result.verticalPeakTime =
                    point.normalizedTime;
            }
        }

        result.horizontalMin =
            minX;

        result.horizontalMax =
            maxX;

        result.horizontalRange =
            maxX -
            minX;

        result.verticalMin =
            minY;

        result.verticalMax =
            maxY;

        result.verticalRange =
            maxY -
            minY;

        NormalizedShotGesturePoint last =
            gesture.points[
                gesture.points.Count - 1
            ];

        result.horizontalFinalValue =
            last.position.x;

        result.verticalFinalValue =
            last.position.y;
    }

    // =========================================================
    // NORMALIZED PEAK SPEED
    // =========================================================

    private void CalculatePeakSpeed(
        NormalizedShotGesture gesture,
        ShotGestureAnalysis result
    )
    {
        float peak =
            0f;

        for (
            int i = 0;
            i < gesture.points.Count;
            i++
        )
        {
            peak =
                Mathf.Max(
                    peak,
                    gesture.points[i]
                        .velocity
                        .magnitude
                );
        }

        result.peakSpeed =
            peak;
    }

    // =========================================================
    // DIRECTION CHANGES
    // =========================================================

    private void CalculateDirectionChanges(
        NormalizedShotGesture gesture,
        ShotGestureAnalysis result
    )
    {
        result.horizontalDirectionChanges =
            CountDirectionChanges(
                gesture,
                true
            );

        result.verticalDirectionChanges =
            CountDirectionChanges(
                gesture,
                false
            );
    }

    private int CountDirectionChanges(
        NormalizedShotGesture gesture,
        bool horizontal
    )
    {
        int changes =
            0;

        int activeSign =
            0;

        float distanceSinceDirectionAccepted =
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
            NormalizedShotGesturePoint point =
                gesture.points[i];

            float position =
                horizontal
                    ? point.position.x
                    : point.position.y;

            float velocity =
                horizontal
                    ? point.velocity.x
                    : point.velocity.y;

            distanceSinceDirectionAccepted +=
                Mathf.Abs(
                    position -
                    previousPosition
                );

            previousPosition =
                position;

            if (
                Mathf.Abs(
                    velocity
                ) <
                minimumRelevantAxisSpeed
            )
            {
                continue;
            }

            int sign =
                velocity > 0f
                    ? 1
                    : -1;

            if (activeSign == 0)
            {
                activeSign =
                    sign;

                distanceSinceDirectionAccepted =
                    0f;

                continue;
            }

            if (
                sign != activeSign &&
                distanceSinceDirectionAccepted >=
                minimumDirectionChangeDistance
            )
            {
                changes++;

                activeSign =
                    sign;

                distanceSinceDirectionAccepted =
                    0f;
            }
        }

        return changes;
    }

    // =========================================================
    // PATH EFFICIENCY
    // =========================================================

    private void CalculatePathEfficiency(
        NormalizedShotGesture gesture,
        ShotGestureAnalysis result
    )
    {
        if (
            gesture.directDistance <
            0.0001f
        )
        {
            result.pathEfficiency =
                gesture.totalDistance >
                0.0001f
                    ? 999f
                    : 1f;

            return;
        }

        result.pathEfficiency =
            gesture.totalDistance /
            gesture.directDistance;
    }

    // =========================================================
    // PATH DEVIATION
    // =========================================================

    private void CalculatePathDeviation(
        NormalizedShotGesture gesture,
        ShotGestureAnalysis result
    )
    {
        Vector2 start =
            gesture.points[0]
                .position;

        Vector2 end =
            gesture.points[
                gesture.points.Count - 1
            ].position;

        Vector2 reference =
            end -
            start;

        float referenceLength =
            reference.magnitude;

        if (
            referenceLength <
            0.05f
        )
        {
            float maximumDistance =
                0f;

            float maximumTime =
                0f;

            for (
                int i = 0;
                i < gesture.points.Count;
                i++
            )
            {
                float distance =
                    Vector2.Distance(
                        start,
                        gesture.points[i]
                            .position
                    );

                if (
                    distance >
                    maximumDistance
                )
                {
                    maximumDistance =
                        distance;

                    maximumTime =
                        gesture.points[i]
                            .normalizedTime;
                }
            }

            result.pathDeviation =
                maximumDistance;

            result.pathDeviationTime =
                maximumTime;

            result.lateralDeviationSign =
                0f;

            return;
        }

        Vector2 referenceDirection =
            reference /
            referenceLength;

        float maximumDeviation =
            0f;

        float maximumDeviationTime =
            0f;

        float strongestSignedDeviation =
            0f;

        for (
            int i = 0;
            i < gesture.points.Count;
            i++
        )
        {
            NormalizedShotGesturePoint point =
                gesture.points[i];

            Vector2 fromStart =
                point.position -
                start;

            float projectedDistance =
                Vector2.Dot(
                    fromStart,
                    referenceDirection
                );

            Vector2 projectedPoint =
                start +
                referenceDirection *
                projectedDistance;

            Vector2 difference =
                point.position -
                projectedPoint;

            float deviation =
                difference.magnitude;

            if (
                deviation >
                maximumDeviation
            )
            {
                maximumDeviation =
                    deviation;

                maximumDeviationTime =
                    point.normalizedTime;

                strongestSignedDeviation =
                    referenceDirection.x *
                    difference.y -
                    referenceDirection.y *
                    difference.x;
            }
        }

        result.pathDeviation =
            maximumDeviation;

        result.pathDeviationTime =
            maximumDeviationTime;

        if (
            Mathf.Abs(
                strongestSignedDeviation
            ) <
            0.0001f
        )
        {
            result.lateralDeviationSign =
                0f;
        }
        else
        {
            result.lateralDeviationSign =
                Mathf.Sign(
                    strongestSignedDeviation
                );
        }
    }

    // =========================================================
    // TURNING
    // =========================================================

    private void CalculateTurning(
        NormalizedShotGesture gesture,
        ShotGestureAnalysis result
    )
    {
        float totalTurning =
            0f;

        Vector2 previousDirection =
            Vector2.zero;

        bool hasPrevious =
            false;

        for (
            int i = 1;
            i < gesture.points.Count;
            i++
        )
        {
            Vector2 movement =
                gesture.points[i]
                    .position -
                gesture.points[i - 1]
                    .position;

            if (
                movement.sqrMagnitude <
                0.000001f
            )
            {
                continue;
            }

            Vector2 direction =
                movement.normalized;

            if (hasPrevious)
            {
                totalTurning +=
                    Vector2.Angle(
                        previousDirection,
                        direction
                    );
            }

            previousDirection =
                direction;

            hasPrevious =
                true;
        }

        result.accumulatedTurning =
            totalTurning;
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    private void CalculateOpeningClosing(
        NormalizedShotGesture gesture,
        ShotGestureAnalysis result
    )
    {
        float finalX =
            gesture.points[
                gesture.points.Count - 1
            ].position.x;

        float strongestOpening =
            0f;

        for (
            int i = 0;
            i < gesture.points.Count;
            i++
        )
        {
            float currentX =
                gesture.points[i]
                    .position.x;

            float excess =
                Mathf.Abs(
                    currentX
                ) -
                Mathf.Abs(
                    finalX
                );

            if (
                excess >
                strongestOpening
            )
            {
                strongestOpening =
                    excess;
            }
        }

        result.openingClosingAmount =
            Mathf.Max(
                strongestOpening,
                0f
            );
    }

    // =========================================================
    // CLOSURE
    // =========================================================

    private void CalculateClosure(
        NormalizedShotGesture gesture,
        ShotGestureAnalysis result
    )
    {
        if (
            gesture.totalDistance <
            0.0001f
        )
        {
            result.closureRatio =
                1f;

            return;
        }

        result.closureRatio =
            Mathf.Clamp01(
                gesture.directDistance /
                gesture.totalDistance
            );
    }

    // =========================================================
    // SIGNED AREA
    // =========================================================

    private void CalculateSignedArea(
        NormalizedShotGesture gesture,
        ShotGestureAnalysis result
    )
    {
        float area =
            0f;

        for (
            int i = 1;
            i < gesture.points.Count;
            i++
        )
        {
            Vector2 a =
                gesture.points[i - 1]
                    .position;

            Vector2 b =
                gesture.points[i]
                    .position;

            area +=
                a.x *
                b.y -
                b.x *
                a.y;
        }

        Vector2 last =
            gesture.points[
                gesture.points.Count - 1
            ].position;

        Vector2 first =
            gesture.points[0]
                .position;

        area +=
            last.x *
            first.y -
            first.x *
            last.y;

        result.signedArea =
            area *
            0.5f;
    }
}