using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Records the REAL world-space path followed by FootballBall.
///
/// PURPOSE:
///
/// We already have:
///
///     desired shot trajectory
///
/// generated from the player's mouse gesture.
///
/// This component records:
///
///     actual Rigidbody trajectory
///
/// so we can compare:
///
///     TARGET
///         vs
///     REAL PHYSICS
///
/// ---------------------------------------------------------
///
/// IMPORTANT:
///
/// Recording happens in FixedUpdate because the ball itself
/// is simulated by Unity Physics in FixedUpdate.
///
/// Therefore there is no advantage in recording the ball
/// at the ~1000 Hz mouse frequency.
///
/// Mouse input:
///     ~1000 samples / second
///
/// Physics:
///     normally 50 simulation steps / second
///
/// Those are two different layers of the system.
/// </summary>
public class BallPathRecorder : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public FootballBall ball;

    public ShotIntentBuilder intentBuilder;

    // =========================================================
    // RECORDING
    // =========================================================

    [Header("Recording")]

    [Tooltip(
        "Maximum time recorded after a shot."
    )]
    public float maximumRecordTime = 4.0f;

    [Tooltip(
        "Do not allow automatic stop before this time."
    )]
    public float minimumRecordTime = 0.20f;

    [Tooltip(
        "If ball speed remains below this value, " +
        "recording can finish."
    )]
    public float stopSpeed = 0.15f;

    [Tooltip(
        "Ball must stay below Stop Speed for this long " +
        "before recording finishes."
    )]
    public float stopConfirmationTime = 0.30f;

    [Tooltip(
        "Minimum world-space movement required before " +
        "adding another point."
    )]
    public float minimumPointDistance = 0.002f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    public bool logPathSummary = true;

    // =========================================================
    // STATE
    // =========================================================

    private readonly List<Vector3> points =
        new List<Vector3>();

    private bool recording;

    private float recordStartTime;

    private float lowSpeedTimer;

    private Vector3 shotOrigin;

    private float maximumHeight;

    private float maximumDistanceFromOrigin;

    // =========================================================
    // EVENTS
    // =========================================================

    public event Action<BallPathRecorder>
        RecordingStarted;

    public event Action<BallPathRecorder>
        RecordingStopped;

    // =========================================================
    // PUBLIC INFORMATION
    // =========================================================

    public IReadOnlyList<Vector3> Points
    {
        get { return points; }
    }

    public bool IsRecording
    {
        get { return recording; }
    }

    public Vector3 ShotOrigin
    {
        get { return shotOrigin; }
    }

    public float MaximumHeight
    {
        get { return maximumHeight; }
    }

    public float MaximumDistanceFromOrigin
    {
        get { return maximumDistanceFromOrigin; }
    }

    public float RecordingDuration
    {
        get
        {
            if (!recording)
                return 0f;

            return
                Time.time -
                recordStartTime;
        }
    }

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

    private void FixedUpdate()
    {
        if (!recording)
            return;

        if (
            ball == null ||
            ball.rb == null
        )
        {
            StopRecording();
            return;
        }

        RecordCurrentPosition();

        float elapsed =
            Time.time -
            recordStartTime;

        float speed =
            ball.rb.velocity.magnitude;

        // -----------------------------------------------------
        // LOW SPEED DETECTION
        // -----------------------------------------------------

        if (
            elapsed >= minimumRecordTime &&
            speed <= stopSpeed
        )
        {
            lowSpeedTimer +=
                Time.fixedDeltaTime;
        }
        else
        {
            lowSpeedTimer =
                0f;
        }

        // -----------------------------------------------------
        // STOP CONDITIONS
        // -----------------------------------------------------

        if (
            elapsed >= maximumRecordTime ||
            lowSpeedTimer >= stopConfirmationTime
        )
        {
            StopRecording();
        }
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (ball == null)
        {
            ball =
                FindObjectOfType<FootballBall>();
        }

        if (intentBuilder == null)
        {
            intentBuilder =
                GetComponent<ShotIntentBuilder>();
        }
    }

    // =========================================================
    // SHOT
    // =========================================================

    private void HandleShotIntent(
        ShotIntent intent
    )
    {
        if (
            intent == null ||
            ball == null
        )
        {
            return;
        }

        StartRecording();
    }

    // =========================================================
    // START
    // =========================================================

    public void StartRecording()
    {
        if (ball == null)
            return;

        points.Clear();

        recording =
            true;

        recordStartTime =
            Time.time;

        lowSpeedTimer =
            0f;

        shotOrigin =
            ball.transform.position;

        maximumHeight =
            shotOrigin.y;

        maximumDistanceFromOrigin =
            0f;

        /*
         * Store the exact starting point immediately.
         */
        points.Add(
            shotOrigin
        );

        RecordingStarted?.Invoke(
            this
        );
    }

    // =========================================================
    // RECORD
    // =========================================================

    private void RecordCurrentPosition()
    {
        Vector3 position =
            ball.rb.position;

        if (points.Count > 0)
        {
            Vector3 previous =
                points[
                    points.Count - 1
                ];

            float distance =
                Vector3.Distance(
                    previous,
                    position
                );

            if (
                distance <
                minimumPointDistance
            )
            {
                return;
            }
        }

        points.Add(
            position
        );

        maximumHeight =
            Mathf.Max(
                maximumHeight,
                position.y
            );

        float distanceFromOrigin =
            Vector3.Distance(
                shotOrigin,
                position
            );

        maximumDistanceFromOrigin =
            Mathf.Max(
                maximumDistanceFromOrigin,
                distanceFromOrigin
            );
    }

    // =========================================================
    // STOP
    // =========================================================

    public void StopRecording()
    {
        if (!recording)
            return;

        /*
         * Capture one final point.
         */
        if (
            ball != null &&
            ball.rb != null
        )
        {
            RecordCurrentPosition();
        }

        recording =
            false;

        if (logPathSummary)
        {
            Debug.Log(
                "[BALL REAL PATH] " +
                "Points: " +
                points.Count +
                " | Max height: " +
                maximumHeight.ToString("F2") +
                " m" +
                " | Max distance: " +
                maximumDistanceFromOrigin.ToString("F2") +
                " m"
            );
        }

        RecordingStopped?.Invoke(
            this
        );
    }
}