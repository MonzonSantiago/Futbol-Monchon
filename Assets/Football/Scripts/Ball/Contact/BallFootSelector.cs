using System;
using UnityEngine;

/// <summary>
/// Foot selected automatically for a valid LMB contact.
///
/// This does NOT affect ball physics.
///
/// It exists only so future systems know:
///
/// - which foot should contact the ball
/// - where the ball was relative to the player
/// - how high the ball was
///
/// There is NO contact quality.
/// There is NO positioning penalty.
/// There is NO shot accuracy penalty.
/// </summary>
public enum BallContactFoot
{
    Left = 0,
    Right = 1
}

/// <summary>
/// Simple deterministic foot selector.
///
/// DESIGN:
///
/// Ball clearly on the right:
///     Right foot.
///
/// Ball clearly on the left:
///     Left foot.
///
/// Ball near the middle:
///     Preferred foot.
///
/// That's all.
///
/// The player never has to think about geometric
/// sweet spots around the body.
/// </summary>
[RequireComponent(typeof(ShotAimContext))]
public class BallFootSelector : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public ShotAimContext shotAimContext;

    public FootballBall ball;

    public Transform playerRoot;

    // =========================================================
    // FOOT SELECTION
    // =========================================================

    [Header("Foot Selection")]

    [Tooltip(
        "If the ball is this close to body center, " +
        "use the preferred foot instead of swapping due to tiny differences."
    )]
    public float centerDeadZone = 0.12f;

    [Tooltip(
        "Default foot when the ball is approximately centered."
    )]
    public BallContactFoot preferredFoot =
        BallContactFoot.Right;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    public bool logFootSelection = true;

    // =========================================================
    // CURRENT / LAST RESULT
    // =========================================================

    public BallContactFoot LastSelectedFoot
    {
        get;
        private set;
    }

    public Vector3 LastBallLocalPosition
    {
        get;
        private set;
    }

    public float LastBallHeightAboveGround
    {
        get;
        private set;
    }

    // =========================================================
    // EVENT
    // =========================================================

    public event Action<BallContactFoot>
        FootSelected;

    // =========================================================
    // DEFAULT VERSION
    // =========================================================

    [SerializeField]
    [HideInInspector]
    private int defaultsVersion = 0;

    private const int CurrentDefaultsVersion = 1;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        ApplyDefaults();
        FindReferences();
    }

    private void OnEnable()
    {
        FindReferences();

        if (shotAimContext != null)
        {
            shotAimContext.ContactGestureReady +=
                HandleContact;
        }
    }

    private void OnDisable()
    {
        if (shotAimContext != null)
        {
            shotAimContext.ContactGestureReady -=
                HandleContact;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyDefaults();
    }
#endif

    // =========================================================
    // DEFAULTS
    // =========================================================

    private void ApplyDefaults()
    {
        if (
            defaultsVersion >=
            CurrentDefaultsVersion
        )
        {
            return;
        }

        centerDeadZone =
            0.12f;

        preferredFoot =
            BallContactFoot.Right;

        defaultsVersion =
            CurrentDefaultsVersion;
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (shotAimContext == null)
        {
            shotAimContext =
                GetComponent<ShotAimContext>();
        }

        if (
            ball == null &&
            shotAimContext != null
        )
        {
            ball =
                shotAimContext.ball;
        }

        if (ball == null)
        {
            ball =
                FindObjectOfType<FootballBall>();
        }

        if (playerRoot == null)
        {
            playerRoot =
                transform;
        }
    }

    // =========================================================
    // CONTACT
    // =========================================================

    private void HandleContact(
        ShotContactGesture gesture
    )
    {
        if (
            gesture == null ||
            playerRoot == null
        )
        {
            return;
        }

        EvaluateFoot(
            gesture.contactBallPosition
        );
    }

    // =========================================================
    // EVALUATE
    // =========================================================

    public BallContactFoot EvaluateFoot(
        Vector3 ballWorldPosition
    )
    {
        Vector3 localPosition =
            WorldToBodyLocal(
                ballWorldPosition
            );

        LastBallLocalPosition =
            localPosition;

        LastBallHeightAboveGround =
            CalculateBallHeightAboveGround(
                ballWorldPosition
            );

        BallContactFoot selected;

        // -----------------------------------------------------
        // CLEAR RIGHT SIDE
        // -----------------------------------------------------

        if (
            localPosition.x >
            centerDeadZone
        )
        {
            selected =
                BallContactFoot.Right;
        }

        // -----------------------------------------------------
        // CLEAR LEFT SIDE
        // -----------------------------------------------------

        else if (
            localPosition.x <
            -centerDeadZone
        )
        {
            selected =
                BallContactFoot.Left;
        }

        // -----------------------------------------------------
        // CENTER
        // -----------------------------------------------------

        else
        {
            selected =
                preferredFoot;
        }

        LastSelectedFoot =
            selected;

        if (logFootSelection)
        {
            Debug.Log(
                "[FOOT SELECTOR V1] " +

                "Ball local: " +
                localPosition.ToString("F2") +

                " | Selected: " +
                selected +

                " | Height: " +
                LastBallHeightAboveGround
                    .ToString("F2") +
                " m"
            );
        }

        FootSelected?.Invoke(
            selected
        );

        return selected;
    }

    // =========================================================
    // BODY-LOCAL POSITION
    // =========================================================

    private Vector3 WorldToBodyLocal(
        Vector3 worldPosition
    )
    {
        /*
         * Only player's Y rotation matters.
         *
         * We intentionally ignore pitch and roll.
         */

        Quaternion bodyYaw =
            Quaternion.Euler(
                0f,
                playerRoot.eulerAngles.y,
                0f
            );

        Quaternion inverseYaw =
            Quaternion.Inverse(
                bodyYaw
            );

        Vector3 difference =
            worldPosition -
            playerRoot.position;

        Vector3 local =
            inverseYaw *
            difference;

        /*
         * X and Z are what we mainly care about here.
         *
         * Y is left available as raw relative height,
         * while actual height above the pitch is calculated
         * independently.
         */

        return local;
    }

    // =========================================================
    // BALL HEIGHT
    // =========================================================

    private float CalculateBallHeightAboveGround(
        Vector3 ballWorldPosition
    )
    {
        RaycastHit[] hits =
            Physics.RaycastAll(
                ballWorldPosition +
                Vector3.up * 0.1f,
                Vector3.down,
                5f,
                ~0,
                QueryTriggerInteraction.Ignore
            );

        float nearestDistance =
            float.PositiveInfinity;

        bool foundGround =
            false;

        float groundY =
            ballWorldPosition.y;

        for (
            int i = 0;
            i < hits.Length;
            i++
        )
        {
            RaycastHit hit =
                hits[i];

            if (hit.collider == null)
            {
                continue;
            }

            Transform hitTransform =
                hit.collider.transform;

            // Ignore player.
            if (
                playerRoot != null &&
                (
                    hitTransform ==
                    playerRoot ||
                    hitTransform.IsChildOf(
                        playerRoot
                    )
                )
            )
            {
                continue;
            }

            // Ignore ball.
            if (
                ball != null &&
                (
                    hitTransform ==
                    ball.transform ||
                    hitTransform.IsChildOf(
                        ball.transform
                    )
                )
            )
            {
                continue;
            }

            if (
                hit.distance <
                nearestDistance
            )
            {
                nearestDistance =
                    hit.distance;

                groundY =
                    hit.point.y;

                foundGround =
                    true;
            }
        }

        if (!foundGround)
        {
            return 0f;
        }

        return Mathf.Max(
            0f,
            ballWorldPosition.y -
            groundY
        );
    }
}