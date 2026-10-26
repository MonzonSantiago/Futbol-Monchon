using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// MIDDLE MOUSE BALL LIFT.
///
/// MMB held:
///     charges lift.
///
/// MMB released:
///     executes lift.
///
/// CONFLICT RULE:
///
/// If LMB or RMB is already charging and MMB is pressed:
///
/// - all actions are cancelled
/// - MMB does NOT begin
/// - player must release all action buttons
/// - only a completely new press can start again
/// </summary>
public class BallLiftControl : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public FootballBall ball;

    public ShotAimContext shotAimContext;

    public PlayerBallControl playerBallControl;

    public BallActionCoordinator actionCoordinator;

    public DualRingHUDController hudController;

    public Camera movementCamera;

    // =========================================================
    // CHARGE
    // =========================================================

    [Header("Charge")]

    [Tooltip(
        "Seconds required to reach maximum lift."
    )]
    public float fullChargeTime = 0.90f;

    [Tooltip(
        "Minimum useful lift height above the ball."
    )]
    public float minimumLiftHeight = 0.32f;

    [Tooltip(
        "Extra clearance above player's detected head."
    )]
    public float headClearance = 0.18f;

    [Tooltip(
        "Response curve from hold duration to height."
    )]
    public float liftChargeExponent = 0.85f;

    // =========================================================
    // CONTACT RANGE
    // =========================================================

    [Header("Contact Range")]

    public float maximumHorizontalDistance = 1.25f;

    [Tooltip(
        "Ball must be relatively low for MMB lift."
    )]
    public float maximumBallHeightAboveGround = 0.42f;

    // =========================================================
    // DIRECTION
    // =========================================================

    [Header("WASD Direction")]

    public float minimumDirectionalSpeed = 0.65f;

    public float maximumDirectionalSpeed = 3.10f;

    [Range(0f, 1f)]
    public float existingPlanarMomentumCarry = 0.35f;

    public float maximumPlanarSpeedAfterLift = 6.5f;

    // =========================================================
    // PLAYER HEIGHT
    // =========================================================

    [Header("Automatic Player Height")]

    public float fallbackPlayerHeight = 1.80f;

    public float minimumValidPlayerHeight = 1.20f;

    public float maximumValidPlayerHeight = 2.40f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    public bool logLift = true;

    // =========================================================
    // STATE
    // =========================================================

    private bool charging;

    private float chargeStartTime;

    private float lockedCharge01;

    // =========================================================
    // PUBLIC
    // =========================================================

    public bool IsCharging
    {
        get
        {
            return charging;
        }
    }

    public float CurrentCharge01
    {
        get
        {
            if (!charging)
            {
                return 0f;
            }

            float heldTime =
                Mathf.Max(
                    0f,
                    Time.unscaledTime -
                    chargeStartTime
                );

            return Mathf.Clamp01(
                heldTime /
                Mathf.Max(
                    fullChargeTime,
                    0.01f
                )
            );
        }
    }

    public float LastCharge01
    {
        get
        {
            return lockedCharge01;
        }
    }

    // =========================================================
    // DEFAULTS
    // =========================================================

    [SerializeField]
    [HideInInspector]
    private int defaultsVersion = 0;

    private const int CurrentDefaultsVersion = 4;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        ApplyDefaults();

        FindReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyDefaults();
    }
#endif

    private void Update()
    {
        FindReferences();

        Mouse mouse =
            Mouse.current;

        if (mouse == null)
        {
            return;
        }

        // =====================================================
        // MMB DOWN
        // =====================================================

        if (
            mouse.middleButton.wasPressedThisFrame
        )
        {
            bool canBegin =
                actionCoordinator == null ||
                actionCoordinator.TryBeginAction(
                    DualRingHUDController.ActionType.Wheel
                );

            /*
             * Conflict:
             *
             * coordinator cancels everything and returns false.
             *
             * MMB does NOT start.
             */
            if (!canBegin)
            {
                return;
            }

            BeginCharge();
        }

        // =====================================================
        // MMB HELD
        // =====================================================

        if (
            charging &&
            mouse.middleButton.isPressed
        )
        {
            UpdateHUD();
        }

        // =====================================================
        // MMB RELEASE
        // =====================================================

        if (
            charging &&
            mouse.middleButton.wasReleasedThisFrame
        )
        {
            ReleaseLift();
        }
    }

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

        fullChargeTime =
            0.90f;

        minimumLiftHeight =
            0.32f;

        headClearance =
            0.18f;

        liftChargeExponent =
            0.85f;

        maximumHorizontalDistance =
            1.25f;

        maximumBallHeightAboveGround =
            0.42f;

        minimumDirectionalSpeed =
            0.65f;

        maximumDirectionalSpeed =
            3.10f;

        existingPlanarMomentumCarry =
            0.35f;

        maximumPlanarSpeedAfterLift =
            6.5f;

        fallbackPlayerHeight =
            1.80f;

        minimumValidPlayerHeight =
            1.20f;

        maximumValidPlayerHeight =
            2.40f;

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

        if (playerBallControl == null)
        {
            playerBallControl =
                GetComponent<PlayerBallControl>();
        }

        if (actionCoordinator == null)
        {
            actionCoordinator =
                GetComponent<BallActionCoordinator>();
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

        if (hudController == null)
        {
            hudController =
                FindObjectOfType<DualRingHUDController>();
        }

        if (
            movementCamera == null &&
            shotAimContext != null
        )
        {
            movementCamera =
                shotAimContext.aimCamera;
        }

        if (movementCamera == null)
        {
            movementCamera =
                Camera.main;
        }
    }

    // =========================================================
    // BEGIN CHARGE
    // =========================================================

    private void BeginCharge()
    {
        charging =
            true;

        chargeStartTime =
            Time.unscaledTime;

        lockedCharge01 =
            0f;

        UpdateHUD();

        if (logLift)
        {
            Debug.Log(
                "[MMB LIFT CHARGE START]"
            );
        }
    }

    // =========================================================
    // RELEASE
    // =========================================================

    private void ReleaseLift()
    {
        if (!charging)
        {
            return;
        }

        lockedCharge01 =
            CurrentCharge01;

        charging =
            false;

        TryExecuteLift(
            lockedCharge01
        );

        if (hudController != null)
        {
            hudController.ReleaseAction(
                DualRingHUDController.ActionType.Wheel
            );
        }

        if (actionCoordinator != null)
        {
            actionCoordinator.EndAction(
                DualRingHUDController.ActionType.Wheel
            );
        }
    }

    // =========================================================
    // CONFLICT CANCEL
    // =========================================================

    public void CancelChargeFromConflict()
    {
        charging =
            false;

        lockedCharge01 =
            0f;

        chargeStartTime =
            0f;

        /*
         * Coordinator retains responsibility for
         * conflict lock/unlock.
         *
         * No EndAction here.
         */

        if (hudController != null)
        {
            hudController.CancelAction(
                DualRingHUDController.ActionType.Wheel
            );
        }
    }

    // =========================================================
    // HUD
    // =========================================================

    private void UpdateHUD()
    {
        if (hudController == null)
        {
            return;
        }

        hudController.SetActionCharge(
            DualRingHUDController.ActionType.Wheel,
            CurrentCharge01
        );
    }

    // =========================================================
    // EXECUTE
    // =========================================================

    private void TryExecuteLift(
        float charge01
    )
    {
        if (
            ball == null ||
            ball.rb == null
        )
        {
            return;
        }

        // -----------------------------------------------------
        // DISTANCE
        // -----------------------------------------------------

        Vector3 difference =
            ball.transform.position -
            transform.position;

        Vector3 planarDifference =
            new Vector3(
                difference.x,
                0f,
                difference.z
            );

        float horizontalDistance =
            planarDifference.magnitude;

        if (
            horizontalDistance >
            maximumHorizontalDistance
        )
        {
            if (logLift)
            {
                Debug.Log(
                    "[MMB LIFT MISSED] " +
                    "Horizontal distance: " +
                    horizontalDistance.ToString("F2")
                );
            }

            return;
        }

        // -----------------------------------------------------
        // BALL HEIGHT
        // -----------------------------------------------------

        float ballHeight =
            ball.EvaluateCurrentBallHeightAboveGround();

        if (
            ballHeight >
            maximumBallHeightAboveGround
        )
        {
            if (logLift)
            {
                Debug.Log(
                    "[MMB LIFT MISSED] " +
                    "Ball height: " +
                    ballHeight.ToString("F2") +
                    " m"
                );
            }

            return;
        }

        ball.StopShotAssistance();

        // -----------------------------------------------------
        // PLAYER HEIGHT
        // -----------------------------------------------------

        float playerHeight =
            EvaluatePlayerHeight();

        float maximumApexHeight =
            playerHeight +
            headClearance;

        /*
         * Additional distance the ball center has to rise.
         */
        float maximumRise =
            Mathf.Max(
                minimumLiftHeight,
                maximumApexHeight -
                ballHeight
            );

        float shapedCharge =
            Mathf.Pow(
                Mathf.Clamp01(
                    charge01
                ),
                Mathf.Max(
                    liftChargeExponent,
                    0.01f
                )
            );

        float desiredRise =
            Mathf.Lerp(
                minimumLiftHeight,
                maximumRise,
                shapedCharge
            );

        // -----------------------------------------------------
        // BALLISTIC Y VELOCITY
        // -----------------------------------------------------

        float gravity =
            Mathf.Abs(
                Physics.gravity.y
            );

        float verticalVelocity =
            Mathf.Sqrt(
                2f *
                gravity *
                desiredRise
            );

        // -----------------------------------------------------
        // WASD
        // -----------------------------------------------------

        Vector3 inputDirection =
            ReadMovementDirection();

        Vector3 incoming =
            ball.rb.velocity;

        Vector3 incomingPlanar =
            new Vector3(
                incoming.x,
                0f,
                incoming.z
            );

        Vector3 resultingPlanar =
            incomingPlanar *
            existingPlanarMomentumCarry;

        if (
            inputDirection.sqrMagnitude >
            0.0001f
        )
        {
            float directionalSpeed =
                Mathf.Lerp(
                    minimumDirectionalSpeed,
                    maximumDirectionalSpeed,
                    shapedCharge
                );

            resultingPlanar +=
                inputDirection.normalized *
                directionalSpeed;
        }

        if (
            resultingPlanar.magnitude >
            maximumPlanarSpeedAfterLift
        )
        {
            resultingPlanar =
                resultingPlanar.normalized *
                maximumPlanarSpeedAfterLift;
        }

        // -----------------------------------------------------
        // RESULT
        // -----------------------------------------------------

        Vector3 resultVelocity =
            new Vector3(
                resultingPlanar.x,
                verticalVelocity,
                resultingPlanar.z
            );

        ball.rb.velocity =
            resultVelocity;

        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        if (logLift)
        {
            Debug.Log(
                "[MMB LIFT V4] " +

                "Charge: " +
                charge01.ToString("F3") +

                " | Player height: " +
                playerHeight.ToString("F2") +
                " m" +

                " | Ball height: " +
                ballHeight.ToString("F2") +
                " m" +

                " | Target rise: " +
                desiredRise.ToString("F2") +
                " m" +

                " | Vertical speed: " +
                verticalVelocity.ToString("F2") +

                " | Input: " +
                inputDirection.ToString("F2") +

                " | Result: " +
                resultVelocity.ToString("F2")
            );
        }
    }

    // =========================================================
    // PLAYER HEIGHT
    // =========================================================

    private float EvaluatePlayerHeight()
    {
        float lowestY =
            float.PositiveInfinity;

        float highestY =
            float.NegativeInfinity;

        bool foundBounds =
            false;

        // -----------------------------------------------------
        // COLLIDERS
        // -----------------------------------------------------

        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        for (
            int i = 0;
            i < colliders.Length;
            i++
        )
        {
            Collider current =
                colliders[i];

            if (
                current == null ||
                !current.enabled
            )
            {
                continue;
            }

            if (
                ball != null &&
                (
                    current.transform ==
                    ball.transform ||
                    current.transform.IsChildOf(
                        ball.transform
                    )
                )
            )
            {
                continue;
            }

            Bounds bounds =
                current.bounds;

            lowestY =
                Mathf.Min(
                    lowestY,
                    bounds.min.y
                );

            highestY =
                Mathf.Max(
                    highestY,
                    bounds.max.y
                );

            foundBounds =
                true;
        }

        // -----------------------------------------------------
        // RENDERERS
        // -----------------------------------------------------

        Renderer[] renderers =
            GetComponentsInChildren<Renderer>();

        for (
            int i = 0;
            i < renderers.Length;
            i++
        )
        {
            Renderer current =
                renderers[i];

            if (
                current == null ||
                !current.enabled
            )
            {
                continue;
            }

            Bounds bounds =
                current.bounds;

            lowestY =
                Mathf.Min(
                    lowestY,
                    bounds.min.y
                );

            highestY =
                Mathf.Max(
                    highestY,
                    bounds.max.y
                );

            foundBounds =
                true;
        }

        if (foundBounds)
        {
            float detectedHeight =
                highestY -
                lowestY;

            if (
                detectedHeight >=
                minimumValidPlayerHeight &&
                detectedHeight <=
                maximumValidPlayerHeight
            )
            {
                return detectedHeight;
            }
        }

        return fallbackPlayerHeight;
    }

    // =========================================================
    // WASD DIRECTION
    // =========================================================

    private Vector3 ReadMovementDirection()
    {
        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null)
        {
            return Vector3.zero;
        }

        float x =
            0f;

        float y =
            0f;

        if (keyboard.aKey.isPressed)
        {
            x -=
                1f;
        }

        if (keyboard.dKey.isPressed)
        {
            x +=
                1f;
        }

        if (keyboard.sKey.isPressed)
        {
            y -=
                1f;
        }

        if (keyboard.wKey.isPressed)
        {
            y +=
                1f;
        }

        Vector2 input =
            new Vector2(
                x,
                y
            );

        if (
            input.sqrMagnitude >
            1f
        )
        {
            input.Normalize();
        }

        if (
            input.sqrMagnitude <
            0.0001f
        )
        {
            return Vector3.zero;
        }

        Vector3 forward =
            transform.forward;

        Vector3 right =
            transform.right;

        if (movementCamera != null)
        {
            forward =
                movementCamera.transform.forward;

            right =
                movementCamera.transform.right;
        }

        forward.y =
            0f;

        right.y =
            0f;

        if (
            forward.sqrMagnitude >
            0.0001f
        )
        {
            forward.Normalize();
        }

        if (
            right.sqrMagnitude >
            0.0001f
        )
        {
            right.Normalize();
        }

        Vector3 result =
            forward *
            input.y +
            right *
            input.x;

        if (
            result.sqrMagnitude >
            1f
        )
        {
            result.Normalize();
        }

        return result;
    }
}