using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws the REAL trajectory followed by FootballBall.
///
/// The existing ShotTrajectoryVisualizer draws:
///
///     TARGET TRAJECTORY
///
/// This component draws:
///
///     REAL RIGIDBODY TRAJECTORY
///
/// Having both visible at the same time allows us
/// to calibrate the physical controller objectively.
/// </summary>
public class BallPathVisualizer : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public BallPathRecorder recorder;

    // =========================================================
    // DISPLAY
    // =========================================================

    [Header("Display")]

    public bool showRealPath = true;

    [Tooltip(
        "If enabled, the completed path remains visible " +
        "until the next shot."
    )]
    public bool keepLastPathVisible = true;

    public float lineWidth = 0.06f;

    // =========================================================
    // APPEARANCE
    // =========================================================

    [Header("Appearance")]

    [Tooltip(
        "Debug color for the real Rigidbody trajectory."
    )]
    public Color realPathColor =
        new Color(
            0.1f,
            0.9f,
            1.0f,
            1.0f
        );

    // =========================================================
    // INTERNAL
    // =========================================================

    private LineRenderer pathLine;

    private Material pathMaterial;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        FindReferences();
        CreateLine();
    }

    private void OnEnable()
    {
        FindReferences();

        if (recorder != null)
        {
            recorder.RecordingStarted +=
                HandleRecordingStarted;

            recorder.RecordingStopped +=
                HandleRecordingStopped;
        }
    }

    private void OnDisable()
    {
        if (recorder != null)
        {
            recorder.RecordingStarted -=
                HandleRecordingStarted;

            recorder.RecordingStopped -=
                HandleRecordingStopped;
        }
    }

    private void OnDestroy()
    {
        if (pathMaterial != null)
        {
            Destroy(
                pathMaterial
            );
        }
    }

    private void LateUpdate()
    {
        if (
            recorder == null ||
            !showRealPath
        )
        {
            if (pathLine != null)
            {
                pathLine.positionCount =
                    0;
            }

            return;
        }

        if (
            recorder.IsRecording ||
            keepLastPathVisible
        )
        {
            UpdateLine();
        }
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (recorder == null)
        {
            recorder =
                GetComponent<BallPathRecorder>();
        }
    }

    // =========================================================
    // LINE
    // =========================================================

    private void CreateLine()
    {
        GameObject lineObject =
            new GameObject(
                "Ball_REAL_Path"
            );

        lineObject.transform.SetParent(
            transform,
            false
        );

        pathLine =
            lineObject.AddComponent<LineRenderer>();

        pathLine.useWorldSpace =
            true;

        pathLine.startWidth =
            lineWidth;

        pathLine.endWidth =
            lineWidth;

        pathLine.numCapVertices =
            4;

        pathLine.numCornerVertices =
            4;

        pathLine.positionCount =
            0;

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Unlit"
            );

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Sprites/Default"
                );
        }

        pathMaterial =
            new Material(
                shader
            );

        /*
         * URP Unlit normally exposes _BaseColor.
         * The fallback material can use .color.
         */
        if (
            pathMaterial.HasProperty(
                "_BaseColor"
            )
        )
        {
            pathMaterial.SetColor(
                "_BaseColor",
                realPathColor
            );
        }

        pathMaterial.color =
            realPathColor;

        pathLine.material =
            pathMaterial;
    }

    // =========================================================
    // EVENTS
    // =========================================================

    private void HandleRecordingStarted(
        BallPathRecorder source
    )
    {
        if (pathLine != null)
        {
            pathLine.positionCount =
                0;
        }
    }

    private void HandleRecordingStopped(
        BallPathRecorder source
    )
    {
        UpdateLine();

        if (!keepLastPathVisible)
        {
            pathLine.positionCount =
                0;
        }
    }

    // =========================================================
    // UPDATE LINE
    // =========================================================

    private void UpdateLine()
    {
        if (
            pathLine == null ||
            recorder == null
        )
        {
            return;
        }

        IReadOnlyList<Vector3> points =
            recorder.Points;

        if (
            points == null ||
            points.Count < 2
        )
        {
            pathLine.positionCount =
                0;

            return;
        }

        pathLine.startWidth =
            lineWidth;

        pathLine.endWidth =
            lineWidth;

        pathLine.positionCount =
            points.Count;

        for (
            int i = 0;
            i < points.Count;
            i++
        )
        {
            pathLine.SetPosition(
                i,
                points[i]
            );
        }
    }
}