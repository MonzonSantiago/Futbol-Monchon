using UnityEngine;

/// <summary>
/// Debug-only visualizer for ShotTrajectoryProfile.
///
/// It renders the intended 3D trajectory.
///
/// This has ZERO effect on ball physics.
/// </summary>
public class ShotTrajectoryVisualizer : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public ShotTrajectoryBuilder builder;

    // =========================================================
    // DISPLAY
    // =========================================================

    [Header("Display")]

    public bool showTrajectory = true;

    public float lineWidth = 0.04f;

    // =========================================================
    // INTERNAL
    // =========================================================

    private LineRenderer trajectoryLine;

    private Material trajectoryMaterial;

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

        if (builder != null)
        {
            builder.TrajectoryReady +=
                HandleTrajectory;
        }
    }

    private void OnDisable()
    {
        if (builder != null)
        {
            builder.TrajectoryReady -=
                HandleTrajectory;
        }
    }

    private void OnDestroy()
    {
        if (trajectoryMaterial != null)
        {
            Destroy(
                trajectoryMaterial
            );
        }
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (builder == null)
        {
            builder =
                GetComponent<ShotTrajectoryBuilder>();
        }
    }

    // =========================================================
    // LINE
    // =========================================================

    private void CreateLine()
    {
        GameObject lineObject =
            new GameObject(
                "ShotTrajectory_PREVIEW"
            );

        lineObject.transform.SetParent(
            transform,
            false
        );

        trajectoryLine =
            lineObject.AddComponent<LineRenderer>();

        trajectoryLine.useWorldSpace =
            true;

        trajectoryLine.startWidth =
            lineWidth;

        trajectoryLine.endWidth =
            lineWidth;

        trajectoryLine.numCapVertices =
            4;

        trajectoryLine.numCornerVertices =
            4;

        trajectoryLine.positionCount =
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

        trajectoryMaterial =
            new Material(
                shader
            );

        trajectoryLine.material =
            trajectoryMaterial;
    }

    // =========================================================
    // EVENT
    // =========================================================

    private void HandleTrajectory(
        ShotTrajectoryProfile trajectory
    )
    {
        if (!showTrajectory)
        {
            trajectoryLine.positionCount =
                0;

            return;
        }

        if (
            trajectory == null ||
            trajectory.worldPoints == null ||
            trajectory.worldPoints.Count < 2
        )
        {
            trajectoryLine.positionCount =
                0;

            return;
        }

        trajectoryLine.startWidth =
            lineWidth;

        trajectoryLine.endWidth =
            lineWidth;

        trajectoryLine.positionCount =
            trajectory.worldPoints.Count;

        for (
            int i = 0;
            i < trajectory.worldPoints.Count;
            i++
        )
        {
            trajectoryLine.SetPosition(
                i,
                trajectory.worldPoints[i]
            );
        }
    }
}