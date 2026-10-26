using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dibuja un anillo circular de UI.
///
/// El centro queda completamente transparente.
///
/// Fill01:
///
/// 0.00 = vacío
/// 0.25 = 25%
/// 0.50 = 50%
/// 0.75 = 75%
/// 1.00 = completo
/// </summary>
[ExecuteAlways]
public class UIMeterRing : MaskableGraphic
{
    [Header("Ring")]

    [Range(0f, 1f)]
    [SerializeField]
    private float fill01 = 1f;

    [Range(0.05f, 0.95f)]
    [SerializeField]
    private float innerRadius01 = 0.67f;

    [Range(12, 256)]
    [SerializeField]
    private int segments = 128;

    [SerializeField]
    private float startAngle = 90f;

    [SerializeField]
    private bool clockwise = true;

    // =========================================================
    // PUBLIC
    // =========================================================

    public float Fill01
    {
        get
        {
            return fill01;
        }

        set
        {
            float newValue =
                Mathf.Clamp01(value);

            if (
                Mathf.Approximately(
                    fill01,
                    newValue
                )
            )
            {
                return;
            }

            fill01 =
                newValue;

            SetVerticesDirty();
        }
    }

    // =========================================================
    // UNITY
    // =========================================================

    protected override void OnEnable()
    {
        base.OnEnable();

        raycastTarget =
            false;

        SetVerticesDirty();
    }

    // =========================================================
    // DRAW
    // =========================================================

    protected override void OnPopulateMesh(
        VertexHelper vh
    )
    {
        vh.Clear();

        float currentFill =
            Mathf.Clamp01(
                fill01
            );

        if (
            currentFill <=
            0.0001f
        )
        {
            return;
        }

        Rect rect =
            rectTransform.rect;

        Vector2 center =
            rect.center;

        float outerRadius =
            Mathf.Min(
                rect.width,
                rect.height
            ) *
            0.5f;

        float innerRadius =
            outerRadius *
            innerRadius01;

        float totalAngle =
            360f *
            currentFill;

        int stepCount =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    segments *
                    currentFill
                )
            );

        float direction =
            clockwise
                ? -1f
                : 1f;

        for (
            int i = 0;
            i < stepCount;
            i++
        )
        {
            float t0 =
                i /
                (float)stepCount;

            float t1 =
                (i + 1) /
                (float)stepCount;

            float angle0 =
                startAngle +
                direction *
                totalAngle *
                t0;

            float angle1 =
                startAngle +
                direction *
                totalAngle *
                t1;

            Vector2 inner0 =
                center +
                AngleToVector(
                    angle0
                ) *
                innerRadius;

            Vector2 outer0 =
                center +
                AngleToVector(
                    angle0
                ) *
                outerRadius;

            Vector2 outer1 =
                center +
                AngleToVector(
                    angle1
                ) *
                outerRadius;

            Vector2 inner1 =
                center +
                AngleToVector(
                    angle1
                ) *
                innerRadius;

            AddQuad(
                vh,
                inner0,
                outer0,
                outer1,
                inner1,
                color
            );
        }
    }

    // =========================================================
    // MATH
    // =========================================================

    private Vector2 AngleToVector(
        float angle
    )
    {
        float radians =
            angle *
            Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Cos(radians),
            Mathf.Sin(radians)
        );
    }

    private void AddQuad(
        VertexHelper vh,
        Vector2 inner0,
        Vector2 outer0,
        Vector2 outer1,
        Vector2 inner1,
        Color32 vertexColor
    )
    {
        int index =
            vh.currentVertCount;

        vh.AddVert(
            inner0,
            vertexColor,
            Vector2.zero
        );

        vh.AddVert(
            outer0,
            vertexColor,
            Vector2.zero
        );

        vh.AddVert(
            outer1,
            vertexColor,
            Vector2.zero
        );

        vh.AddVert(
            inner1,
            vertexColor,
            Vector2.zero
        );

        vh.AddTriangle(
            index,
            index + 1,
            index + 2
        );

        vh.AddTriangle(
            index + 2,
            index + 3,
            index
        );
    }

#if UNITY_EDITOR

    protected override void OnValidate()
    {
        base.OnValidate();

        fill01 =
            Mathf.Clamp01(
                fill01
            );

        innerRadius01 =
            Mathf.Clamp(
                innerRadius01,
                0.05f,
                0.95f
            );

        segments =
            Mathf.Clamp(
                segments,
                12,
                256
            );

        SetVerticesDirty();
    }

#endif
}