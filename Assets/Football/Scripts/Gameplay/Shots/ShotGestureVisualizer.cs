using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Debug visualization of the SHORT BALL CONTACT gesture.
///
/// LEFT:
///     raw mouse movement from contact window.
///
/// RIGHT:
///     processed contact gesture.
///
/// This visualizer has zero effect on gameplay.
/// </summary>
public class ShotGestureVisualizer : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public ShotAimContext shotContext;

    public ShotGestureProcessor processor;

    public Camera targetCamera;

    // =========================================================
    // DISPLAY
    // =========================================================

    [Header("Display")]

    public bool showRawGesture = true;

    public bool showProcessedGesture = true;

    public float distanceFromCamera = 2.5f;

    public float verticalOffset = 0.15f;

    public float horizontalSeparation = 0.8f;

    // =========================================================
    // SCALE
    // =========================================================

    [Header("Scale")]

    public float gestureScale = 0.001f;

    public float maxDisplayExtent = 0.75f;

    // =========================================================
    // LINES
    // =========================================================

    [Header("Lines")]

    public float rawLineWidth = 0.008f;

    public float processedLineWidth = 0.014f;

    public int maxRawDisplayPoints = 1000;

    // =========================================================
    // INTERNAL
    // =========================================================

    private LineRenderer rawLine;

    private LineRenderer processedLine;

    private Material rawMaterial;

    private Material processedMaterial;

    private readonly List<Vector2> rawDisplayPoints =
        new List<Vector2>();

    private readonly List<Vector2> processedDisplayPoints =
        new List<Vector2>();

    private bool hasGesture;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        FindReferences();
        CreateLineRenderers();
    }

    private void OnEnable()
    {
        FindReferences();

        if (shotContext != null)
        {
            shotContext.ContactGestureReady +=
                HandleRawContactGesture;
        }

        if (processor != null)
        {
            processor.ProcessedGestureReady +=
                HandleProcessedGesture;
        }
    }

    private void OnDisable()
    {
        if (shotContext != null)
        {
            shotContext.ContactGestureReady -=
                HandleRawContactGesture;
        }

        if (processor != null)
        {
            processor.ProcessedGestureReady -=
                HandleProcessedGesture;
        }
    }

    private void OnDestroy()
    {
        if (rawMaterial != null)
        {
            Destroy(
                rawMaterial
            );
        }

        if (
            processedMaterial != null
        )
        {
            Destroy(
                processedMaterial
            );
        }
    }

    private void LateUpdate()
    {
        if (
            !hasGesture ||
            targetCamera == null
        )
        {
            return;
        }

        UpdateLines();
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

        if (processor == null)
        {
            processor =
                GetComponent<ShotGestureProcessor>();
        }

        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }
    }

    // =========================================================
    // LINE CREATION
    // =========================================================

    private void CreateLineRenderers()
    {
        GameObject rawObject =
            new GameObject(
                "ShotContact_RAW"
            );

        rawObject.transform.SetParent(
            transform,
            false
        );

        rawLine =
            rawObject.AddComponent<LineRenderer>();

        ConfigureLine(
            rawLine,
            rawLineWidth,
            out rawMaterial
        );

        GameObject processedObject =
            new GameObject(
                "ShotContact_PROCESSED"
            );

        processedObject.transform.SetParent(
            transform,
            false
        );

        processedLine =
            processedObject
                .AddComponent<LineRenderer>();

        ConfigureLine(
            processedLine,
            processedLineWidth,
            out processedMaterial
        );
    }

    private void ConfigureLine(
        LineRenderer line,
        float width,
        out Material material
    )
    {
        line.useWorldSpace =
            true;

        line.startWidth =
            width;

        line.endWidth =
            width;

        line.numCapVertices =
            4;

        line.numCornerVertices =
            4;

        line.positionCount =
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

        material =
            new Material(
                shader
            );

        line.material =
            material;
    }

    // =========================================================
    // RAW CONTACT
    // =========================================================

    private void HandleRawContactGesture(
        ShotContactGesture gesture
    )
    {
        rawDisplayPoints.Clear();

        processedDisplayPoints.Clear();

        if (processedLine != null)
        {
            processedLine.positionCount =
                0;
        }

        if (
            gesture == null ||
            gesture.samples == null ||
            gesture.samples.Count == 0
        )
        {
            rawLine.positionCount =
                0;

            hasGesture =
                true;

            return;
        }

        int desiredCount =
            Mathf.Min(
                gesture.samples.Count,
                Mathf.Max(
                    maxRawDisplayPoints,
                    2
                )
            );

        for (
            int i = 0;
            i < desiredCount;
            i++
        )
        {
            float normalized =
                desiredCount <= 1
                    ? 0f
                    : i /
                      (float)(
                          desiredCount -
                          1
                      );

            int sourceIndex =
                Mathf.RoundToInt(
                    normalized *
                    (
                        gesture.samples.Count -
                        1
                    )
                );

            rawDisplayPoints.Add(
                gesture.samples[
                    sourceIndex
                ].cumulativePosition
            );
        }

        hasGesture =
            true;

        UpdateLines();
    }

    // =========================================================
    // PROCESSED
    // =========================================================

    private void HandleProcessedGesture(
        ProcessedShotGesture gesture
    )
    {
        processedDisplayPoints.Clear();

        if (
            gesture == null ||
            gesture.points == null
        )
        {
            return;
        }

        for (
            int i = 0;
            i < gesture.points.Count;
            i++
        )
        {
            processedDisplayPoints.Add(
                gesture.points[i]
                    .position
            );
        }

        hasGesture =
            true;

        UpdateLines();
    }

    // =========================================================
    // DRAW
    // =========================================================

    private void UpdateLines()
    {
        if (targetCamera == null)
            return;

        Transform cameraTransform =
            targetCamera.transform;

        Vector3 center =
            cameraTransform.position +
            cameraTransform.forward *
            distanceFromCamera +
            cameraTransform.up *
            verticalOffset;

        Vector3 rawCenter =
            center -
            cameraTransform.right *
            horizontalSeparation *
            0.5f;

        Vector3 processedCenter =
            center +
            cameraTransform.right *
            horizontalSeparation *
            0.5f;

        float sharedScale =
            CalculateSharedScale();

        if (showRawGesture)
        {
            DrawGesture(
                rawLine,
                rawDisplayPoints,
                rawCenter,
                cameraTransform,
                sharedScale
            );
        }
        else
        {
            rawLine.positionCount =
                0;
        }

        if (showProcessedGesture)
        {
            DrawGesture(
                processedLine,
                processedDisplayPoints,
                processedCenter,
                cameraTransform,
                sharedScale
            );
        }
        else
        {
            processedLine.positionCount =
                0;
        }
    }

    // =========================================================
    // SCALE
    // =========================================================

    private float CalculateSharedScale()
    {
        float largest =
            0f;

        MeasureExtent(
            rawDisplayPoints,
            ref largest
        );

        MeasureExtent(
            processedDisplayPoints,
            ref largest
        );

        float scale =
            gestureScale;

        if (
            largest > 0.0001f &&
            largest * scale >
            maxDisplayExtent
        )
        {
            scale =
                maxDisplayExtent /
                largest;
        }

        return scale;
    }

    private void MeasureExtent(
        List<Vector2> points,
        ref float largest
    )
    {
        if (
            points == null ||
            points.Count == 0
        )
        {
            return;
        }

        Vector2 origin =
            points[0];

        for (
            int i = 0;
            i < points.Count;
            i++
        )
        {
            Vector2 difference =
                points[i] -
                origin;

            largest =
                Mathf.Max(
                    largest,
                    Mathf.Abs(
                        difference.x
                    ),
                    Mathf.Abs(
                        difference.y
                    )
                );
        }
    }

    // =========================================================
    // DRAW GESTURE
    // =========================================================

    private void DrawGesture(
        LineRenderer line,
        List<Vector2> points,
        Vector3 center,
        Transform cameraTransform,
        float scale
    )
    {
        if (
            points == null ||
            points.Count < 2
        )
        {
            line.positionCount =
                0;

            return;
        }

        Vector2 origin =
            points[0];

        line.positionCount =
            points.Count;

        for (
            int i = 0;
            i < points.Count;
            i++
        )
        {
            Vector2 local =
                (
                    points[i] -
                    origin
                ) *
                scale;

            Vector3 world =
                center +
                cameraTransform.right *
                local.x +
                cameraTransform.up *
                local.y;

            line.SetPosition(
                i,
                world
            );
        }
    }
}