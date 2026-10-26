using System;
using UnityEngine;

/// <summary>
/// LMB contact trajectory preview.
///
/// WHITE:
///     predicted trajectory.
///
/// CYAN:
///     actual Rigidbody trajectory.
///
/// The preview now starts from the REAL resulting
/// contact velocity, including incoming momentum.
/// </summary>
public class ShotTrajectoryBuilder : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public ShotIntentBuilder intentBuilder;

    public FootballBall ball;

    // =========================================================
    // PREVIEW
    // =========================================================

    [Header("Preview")]

    [Range(32, 256)]
    public int previewPointCount = 128;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    public bool logTrajectory = true;

    // =========================================================
    // RESULT
    // =========================================================

    private ShotTrajectoryProfile lastTrajectory;

    public ShotTrajectoryProfile LastTrajectory
    {
        get
        {
            return lastTrajectory;
        }
    }

    public event Action<ShotTrajectoryProfile>
        TrajectoryReady;

    // =========================================================
    // PENDING INTENT
    // =========================================================

    /*
     * We wait until LateUpdate.
     *
     * Reason:
     *
     * FootballBall first executes the contact and stores
     * exactly which incoming velocity it used.
     *
     * Then this preview can use the same snapshot,
     * independently of event subscription order.
     */
    private ShotIntent pendingIntent;

    private bool hasPendingIntent;

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

        if (intentBuilder != null)
        {
            intentBuilder.ShotIntentReady +=
                HandleShotIntent;
        }
    }

    private void OnDisable()
    {
        if (intentBuilder != null)
        {
            intentBuilder.ShotIntentReady -=
                HandleShotIntent;
        }
    }

    private void LateUpdate()
    {
        if (
            !hasPendingIntent ||
            pendingIntent == null
        )
        {
            return;
        }

        ShotIntent intent =
            pendingIntent;

        pendingIntent =
            null;

        hasPendingIntent =
            false;

        ShotTrajectoryProfile trajectory =
            BuildTrajectory(
                intent
            );

        if (trajectory == null)
        {
            return;
        }

        lastTrajectory =
            trajectory;

        if (logTrajectory)
        {
            float side =
                ball != null
                    ? ball.EvaluateHorizontalCurveDirection(
                        intent
                    )
                    : 0f;

            Debug.Log(
                "[CONTACT TARGET MOMENTUM V1] " +

                "Distance: " +
                trajectory.forwardDistance
                    .ToString("F2") +

                " | Curve side: " +
                side.ToString("F0") +

                " | H: " +
                intent.horizontalEffect01
                    .ToString("F3") +

                " | V: " +
                intent.verticalEffect01
                    .ToString("F3")
            );
        }

        TrajectoryReady?.Invoke(
            trajectory
        );
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (intentBuilder == null)
        {
            intentBuilder =
                GetComponent<ShotIntentBuilder>();
        }

        if (ball == null)
        {
            ball =
                FindObjectOfType<FootballBall>();
        }
    }

    // =========================================================
    // EVENT
    // =========================================================

    private void HandleShotIntent(
        ShotIntent intent
    )
    {
        pendingIntent =
            intent;

        hasPendingIntent =
            true;
    }

    // =========================================================
    // BUILD
    // =========================================================

    public ShotTrajectoryProfile BuildTrajectory(
        ShotIntent intent
    )
    {
        if (
            intent == null ||
            ball == null
        )
        {
            return null;
        }

        // =====================================================
        // EXACT STARTING VELOCITY
        // =====================================================

        Vector3 startingVelocity;

        if (
            ball.LastExecutedIntent ==
            intent
        )
        {
            /*
             * Best case:
             *
             * FootballBall already executed this exact
             * contact and stored the real resulting velocity.
             */
            startingVelocity =
                ball.LastContactResultVelocity;
        }
        else
        {
            /*
             * Fallback for development/debug.
             */
            Vector3 incoming =
                ball.rb != null
                    ? ball.rb.velocity
                    : Vector3.zero;

            startingVelocity =
                ball.EvaluateContactVelocity(
                    intent,
                    incoming
                );
        }

        Vector3 planarVelocity =
            new Vector3(
                startingVelocity.x,
                0f,
                startingVelocity.z
            );

        float initialPlanarSpeed =
            planarVelocity.magnitude;

        if (
            initialPlanarSpeed <
            0.0001f
        )
        {
            return null;
        }

        Vector3 initialForward =
            planarVelocity.normalized;

        float curveSide =
            ball.EvaluateHorizontalCurveDirection(
                intent
            );

        // =====================================================
        // PREVIEW DURATION
        // =====================================================

        float nominalShotSpeed =
            ball.EvaluateShotSpeed(
                intent.power01
            );

        float nominalDistance =
            ball.EvaluateCurveDistance(
                intent.power01
            );

        float totalTime =
            nominalDistance /
            Mathf.Max(
                nominalShotSpeed,
                0.01f
            );

        /*
         * If incoming momentum made the result considerably
         * faster/slower, the displayed distance follows it.
         */
        float predictedDistance =
            initialPlanarSpeed *
            totalTime;

        // =====================================================
        // RESULT
        // =====================================================

        ShotTrajectoryProfile result =
            new ShotTrajectoryProfile();

        result.origin =
            intent.contactBallPosition;

        result.forward =
            initialForward;

        result.right =
            Vector3.Cross(
                Vector3.up,
                initialForward
            ).normalized;

        result.up =
            Vector3.up;

        result.forwardDistance =
            predictedDistance;

        result.lateralScale =
            intent.horizontalEffect01;

        result.verticalScale =
            intent.verticalEffect01;

        result.worldPoints.Clear();

        int count =
            Mathf.Clamp(
                previewPointCount,
                32,
                256
            );

        float dt =
            totalTime /
            Mathf.Max(
                count - 1,
                1
            );

        Vector3 position =
            Vector3.zero;

        float verticalVelocity =
            startingVelocity.y;

        // =====================================================
        // SIMULATION
        // =====================================================

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            result.worldPoints.Add(
                result.origin +
                position
            );

            if (
                i >=
                count - 1
            )
            {
                continue;
            }

            float elapsed =
                i *
                dt;

            float planarSpeed =
                planarVelocity.magnitude;

            // -------------------------------------------------
            // CURVE
            // -------------------------------------------------

            if (
                planarSpeed >
                ball.minimumCurveBallSpeed &&
                Mathf.Abs(
                    curveSide
                ) >
                0.01f
            )
            {
                float turnRate =
                    ball.EvaluateCurveTurnRate(
                        intent,
                        planarSpeed,
                        elapsed
                    );

                float turnThisStep =
                    turnRate *
                    curveSide *
                    dt;

                planarVelocity =
                    Quaternion.AngleAxis(
                        turnThisStep,
                        Vector3.up
                    ) *
                    planarVelocity;

                if (
                    planarVelocity.sqrMagnitude >
                    0.000001f
                )
                {
                    planarVelocity =
                        planarVelocity.normalized *
                        planarSpeed;
                }
            }

            // -------------------------------------------------
            // GRAVITY
            // -------------------------------------------------

            verticalVelocity +=
                Physics.gravity.y *
                dt;

            Vector3 completeVelocity =
                planarVelocity +
                Vector3.up *
                verticalVelocity;

            position +=
                completeVelocity *
                dt;

            // -------------------------------------------------
            // DEBUG GROUND
            // -------------------------------------------------

            if (
                position.y <
                0f
            )
            {
                position.y =
                    0f;

                if (
                    verticalVelocity <
                    0f
                )
                {
                    verticalVelocity =
                        0f;
                }
            }
        }

        if (
            result.worldPoints.Count >
            0
        )
        {
            result.worldPoints[0] =
                result.origin;
        }

        return result;
    }
}