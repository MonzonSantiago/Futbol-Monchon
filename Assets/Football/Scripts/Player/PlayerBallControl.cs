using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
public class PlayerBallControl : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Referencias")]

    public FootballBall ball;

    public PlayerMotor playerMotor;

    public ShotAimContext shotAimContext;

    public BallLiftControl ballLiftControl;

    public BallActionCoordinator actionCoordinator;

    public DualRingHUDController hudController;

    // =========================================================
    // DISTANCE
    // =========================================================

    [Header("Distancia de control")]

    public float interactionDistance = 1.25f;

    public float verticalTolerance = 1.20f;

    // =========================================================
    // RMB CHARGE
    // =========================================================

    [Header("Carga RMB")]

    public float chargeTime = 1.5f;

    public float maximumChargeHoldTime = 0.5f;

    // =========================================================
    // INPUT
    // =========================================================

    [Header("Input")]

    public float inputDeadZone = 0.08f;

    public bool useCameraForwardWhenNoInput = true;

    // =========================================================
    // STATE
    // =========================================================

    private bool charging;

    private bool chargeExpired;

    private float currentCharge01;

    private float maximumChargeTimer;

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

    public bool IsChargeExpired
    {
        get
        {
            return chargeExpired;
        }
    }

    public float CurrentCharge01
    {
        get
        {
            return currentCharge01;
        }
    }

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        FindReferences();
    }

    private void Update()
    {
        FindReferences();

        HandleDribbleInput();
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (playerMotor == null)
        {
            playerMotor =
                GetComponent<PlayerMotor>();
        }

        if (shotAimContext == null)
        {
            shotAimContext =
                GetComponent<ShotAimContext>();
        }

        if (ballLiftControl == null)
        {
            ballLiftControl =
                GetComponent<BallLiftControl>();
        }

        if (actionCoordinator == null)
        {
            actionCoordinator =
                GetComponent<BallActionCoordinator>();
        }

        if (hudController == null)
        {
            hudController =
                FindObjectOfType<DualRingHUDController>();
        }
    }

    // =========================================================
    // RMB
    // =========================================================

    private void HandleDribbleInput()
    {
        // -----------------------------------------------------
        // PRESS
        // -----------------------------------------------------

        if (
            Input.GetMouseButtonDown(1)
        )
        {
            bool canBegin =
                actionCoordinator == null ||
                actionCoordinator.TryBeginAction(
                    DualRingHUDController.ActionType.RMB
                );

            /*
             * Important:
             *
             * if another action was active,
             * TryBeginAction returns FALSE after cancelling all.
             *
             * RMB DOES NOT start.
             */
            if (!canBegin)
            {
                return;
            }

            BeginCharge();
        }

        // -----------------------------------------------------
        // HOLD
        // -----------------------------------------------------

        if (
            charging &&
            Input.GetMouseButton(1)
        )
        {
            UpdateCharge();
        }

        // -----------------------------------------------------
        // RELEASE
        // -----------------------------------------------------

        if (
            charging &&
            Input.GetMouseButtonUp(1)
        )
        {
            if (!chargeExpired)
            {
                ExecuteDribbleTouch();

                if (hudController != null)
                {
                    hudController.ReleaseAction(
                        DualRingHUDController.ActionType.RMB
                    );
                }
            }
            else
            {
                if (hudController != null)
                {
                    hudController.CancelAction(
                        DualRingHUDController.ActionType.RMB
                    );
                }
            }

            ResetChargeState();

            if (actionCoordinator != null)
            {
                actionCoordinator.EndAction(
                    DualRingHUDController.ActionType.RMB
                );
            }
        }
    }

    // =========================================================
    // BEGIN
    // =========================================================

    private void BeginCharge()
    {
        charging =
            true;

        chargeExpired =
            false;

        currentCharge01 =
            0f;

        maximumChargeTimer =
            0f;

        UpdateHUD();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void UpdateCharge()
    {
        if (chargeExpired)
        {
            currentCharge01 =
                0f;

            return;
        }

        if (
            currentCharge01 <
            1f
        )
        {
            float chargeSpeed =
                1f /
                Mathf.Max(
                    chargeTime,
                    0.01f
                );

            currentCharge01 =
                Mathf.MoveTowards(
                    currentCharge01,
                    1f,
                    chargeSpeed *
                    Time.deltaTime
                );

            if (
                currentCharge01 >=
                0.999f
            )
            {
                currentCharge01 =
                    1f;

                maximumChargeTimer =
                    0f;
            }

            UpdateHUD();

            return;
        }

        maximumChargeTimer +=
            Time.deltaTime;

        if (
            maximumChargeTimer <
            maximumChargeHoldTime
        )
        {
            currentCharge01 =
                1f;

            UpdateHUD();

            return;
        }

        ExpireCharge();
    }

    // =========================================================
    // EXPIRE
    // =========================================================

    private void ExpireCharge()
    {
        chargeExpired =
            true;

        currentCharge01 =
            0f;

        maximumChargeTimer =
            0f;

        if (hudController != null)
        {
            hudController.CancelAction(
                DualRingHUDController.ActionType.RMB
            );
        }

        if (actionCoordinator != null)
        {
            actionCoordinator.EndAction(
                DualRingHUDController.ActionType.RMB
            );
        }
    }

    // =========================================================
    // RESET
    // =========================================================

    private void ResetChargeState()
    {
        charging =
            false;

        chargeExpired =
            false;

        currentCharge01 =
            0f;

        maximumChargeTimer =
            0f;
    }

    // =========================================================
    // CONFLICT CANCEL
    // =========================================================

    public void CancelChargeFromConflict()
    {
        ResetChargeState();

        if (hudController != null)
        {
            hudController.CancelAction(
                DualRingHUDController.ActionType.RMB
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
            DualRingHUDController.ActionType.RMB,
            currentCharge01
        );
    }

    // =========================================================
    // EXECUTE
    // =========================================================

    private void ExecuteDribbleTouch()
    {
        if (ball == null)
        {
            return;
        }

        if (!CanInteractWithBall())
        {
            return;
        }

        float charge =
            Mathf.Clamp01(
                currentCharge01
            );

        Vector3 direction =
            GetCurrentInputDirection();

        if (
            direction.sqrMagnitude <
            0.001f
        )
        {
            return;
        }

        Vector3 playerVelocity =
            Vector3.zero;

        if (playerMotor != null)
        {
            playerVelocity =
                playerMotor.HorizontalVelocity;
        }

        ball.DribbleTouch(
            direction,
            charge,
            playerVelocity
        );
    }

    // =========================================================
    // DIRECTION
    // =========================================================

    private Vector3 GetCurrentInputDirection()
    {
        float horizontal =
            Input.GetAxisRaw(
                "Horizontal"
            );

        float vertical =
            Input.GetAxisRaw(
                "Vertical"
            );

        Vector3 direction =
            Vector3.zero;

        Transform cameraTransform =
            null;

        if (playerMotor != null)
        {
            cameraTransform =
                playerMotor.cameraTransform;
        }

        if (cameraTransform != null)
        {
            Vector3 forward =
                cameraTransform.forward;

            Vector3 right =
                cameraTransform.right;

            forward.y =
                0f;

            right.y =
                0f;

            forward.Normalize();

            right.Normalize();

            direction =
                forward *
                vertical +
                right *
                horizontal;
        }
        else
        {
            direction =
                new Vector3(
                    horizontal,
                    0f,
                    vertical
                );
        }

        if (
            direction.magnitude <
            inputDeadZone
        )
        {
            if (
                useCameraForwardWhenNoInput &&
                cameraTransform != null
            )
            {
                direction =
                    cameraTransform.forward;

                direction.y =
                    0f;
            }
            else
            {
                return Vector3.zero;
            }
        }

        return direction.normalized;
    }

    // =========================================================
    // RANGE
    // =========================================================

    private bool CanInteractWithBall()
    {
        if (ball == null)
        {
            return false;
        }

        Vector3 difference =
            ball.transform.position -
            transform.position;

        float verticalDifference =
            Mathf.Abs(
                difference.y
            );

        difference.y =
            0f;

        float horizontalDistance =
            difference.magnitude;

        if (
            horizontalDistance >
            interactionDistance
        )
        {
            return false;
        }

        if (
            verticalDifference >
            verticalTolerance
        )
        {
            return false;
        }

        return true;
    }
}