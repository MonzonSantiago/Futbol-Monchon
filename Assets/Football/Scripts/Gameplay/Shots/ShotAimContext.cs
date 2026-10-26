using System;
using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class ShotAimContext : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public Camera aimCamera;

    public FootballBall ball;

    public ShotGestureRecorder recorder;

    public PlayerBallControl playerBallControl;

    public BallLiftControl ballLiftControl;

    public BallActionCoordinator actionCoordinator;

    public DualRingHUDController hudController;

    // =========================================================
    // LMB CHARGE
    // =========================================================

    [Header("LMB Charge")]

    [Tooltip(
        "Tiempo total aproximado hasta llegar al 100%."
    )]
    public float fullChargeTime = 1.5f;

    [Tooltip(
        "El primer tramo termina en este porcentaje."
    )]
    [Range(0.1f, 0.95f)]
    public float fastChargeEnd = 0.70f;

    [Tooltip(
        "Tiempo para cargar del 0 al 70%."
    )]
    public float fastChargeDuration = 0.90f;

    [Tooltip(
        "Tiempo para cargar del 70 al 100%."
    )]
    public float finalChargeDuration = 0.60f;

    [Tooltip(
        "Tiempo permitido manteniendo 100% antes de perder toda la carga."
    )]
    public float maximumChargeHoldTime = 1.0f;

    // =========================================================
    // UNIVERSAL CONTACT INPUT
    // =========================================================

    [Header("Universal Contact Input")]

    [Tooltip(
        "Ventana de mouse anterior al momento de soltar LMB."
    )]
    public float gestureLookbackTime = 0.20f;

    // =========================================================
    // BALL INTERACTION
    // =========================================================

    [Header("Ball Interaction")]

    [Tooltip(
        "Distancia horizontal máxima en el MISMO instante del release."
    )]
    public float maximumBallDistance = 1.35f;

    [Tooltip(
        "Diferencia vertical máxima en el MISMO instante del release."
    )]
    public float maximumBallVerticalDifference = 1.20f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    public bool logShotTiming = true;

    // =========================================================
    // STATE
    // =========================================================

    private bool charging;

    private bool chargeExpired;

    /*
     * Se mantiene por compatibilidad.
     *
     * En el sistema actual el contacto ocurre
     * inmediatamente al soltar LMB.
     */
    private bool waitingForContact;

    private double chargeStartTimestamp;

    private double releaseTimestamp;

    private float chargeDuration;

    private float charge01;

    private float liveCharge01;

    private float maximumChargeTimer;

    private Vector3 releaseAimForward;

    private Vector3 releaseAimRight;

    // =========================================================
    // LAST RESULT
    // =========================================================

    private ShotContactGesture lastContactGesture;

    public ShotContactGesture LastContactGesture
    {
        get
        {
            return lastContactGesture;
        }
    }

    // =========================================================
    // PUBLIC STATE
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

    public bool IsWaitingForContact
    {
        get
        {
            return waitingForContact;
        }
    }

    public float CurrentChargeDuration
    {
        get
        {
            if (!charging)
            {
                return 0f;
            }

            return Mathf.Max(
                0f,
                (float)(
                    InputState.currentTime -
                    chargeStartTimestamp
                )
            );
        }
    }

    public float CurrentCharge01
    {
        get
        {
            return Mathf.Clamp01(
                liveCharge01
            );
        }
    }

    // =========================================================
    // EVENTS
    // =========================================================

    public event Action<ShotAimContext>
        ShotReleased;

    public event Action<ShotContactGesture>
        ContactGestureReady;

    public event Action
        ShotCancelled;

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

        Mouse mouse =
            Mouse.current;

        if (mouse == null)
        {
            return;
        }

        // =====================================================
        // LMB DOWN
        // =====================================================

        if (
            mouse.leftButton.wasPressedThisFrame
        )
        {
            bool canBegin =
                actionCoordinator == null ||
                actionCoordinator.TryBeginAction(
                    DualRingHUDController.ActionType.LMB
                );

            /*
             * Si otra acción ya estaba siendo utilizada:
             *
             * coordinator:
             *     cancela todo
             *     bloquea inputs
             *
             * y LMB NO comienza.
             */
            if (!canBegin)
            {
                return;
            }

            BeginCharge();
        }

        // =====================================================
        // LMB HELD
        // =====================================================

        if (
            charging &&
            mouse.leftButton.isPressed
        )
        {
            UpdateLiveCharge();
        }

        // =====================================================
        // LMB RELEASE
        // =====================================================

        if (
            charging &&
            mouse.leftButton.wasReleasedThisFrame
        )
        {
            if (chargeExpired)
            {
                FinishExpiredCharge();
            }
            else
            {
                ReleaseContact();
            }
        }
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (aimCamera == null)
        {
            aimCamera =
                Camera.main;
        }

        if (recorder == null)
        {
            recorder =
                GetComponent<ShotGestureRecorder>();
        }

        if (playerBallControl == null)
        {
            playerBallControl =
                GetComponent<PlayerBallControl>();
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
    // BEGIN CHARGE
    // =========================================================

    private void BeginCharge()
    {
        chargeStartTimestamp =
            InputState.currentTime;

        charging =
            true;

        chargeExpired =
            false;

        waitingForContact =
            false;

        chargeDuration =
            0f;

        charge01 =
            0f;

        liveCharge01 =
            0f;

        maximumChargeTimer =
            0f;

        UpdateShotHUD();

        if (logShotTiming)
        {
            Debug.Log(
                "[LMB CONTACT PREPARATION START]"
            );
        }
    }

    // =========================================================
    // LIVE CHARGE
    // =========================================================

    private void UpdateLiveCharge()
    {
        /*
         * Expiró:
         *
         * queda muerto en cero hasta que el usuario
         * suelte físicamente LMB.
         */
        if (chargeExpired)
        {
            liveCharge01 =
                0f;

            return;
        }

        // =====================================================
        // 0 -> 70%
        // =====================================================

        if (
            liveCharge01 <
            fastChargeEnd
        )
        {
            float speed =
                fastChargeEnd /
                Mathf.Max(
                    fastChargeDuration,
                    0.01f
                );

            liveCharge01 =
                Mathf.MoveTowards(
                    liveCharge01,
                    fastChargeEnd,
                    speed *
                    Time.unscaledDeltaTime
                );

            UpdateShotHUD();

            return;
        }

        // =====================================================
        // 70 -> 100%
        // =====================================================

        if (
            liveCharge01 <
            1f
        )
        {
            float remaining =
                1f -
                fastChargeEnd;

            float speed =
                remaining /
                Mathf.Max(
                    finalChargeDuration,
                    0.01f
                );

            liveCharge01 =
                Mathf.MoveTowards(
                    liveCharge01,
                    1f,
                    speed *
                    Time.unscaledDeltaTime
                );

            if (
                liveCharge01 >=
                0.999f
            )
            {
                liveCharge01 =
                    1f;

                maximumChargeTimer =
                    0f;
            }

            UpdateShotHUD();

            return;
        }

        // =====================================================
        // 100% HOLD
        // =====================================================

        liveCharge01 =
            1f;

        maximumChargeTimer +=
            Time.unscaledDeltaTime;

        UpdateShotHUD();

        if (
            maximumChargeTimer >=
            maximumChargeHoldTime
        )
        {
            ExpireCharge();
        }
    }

    // =========================================================
    // EXPIRE
    // =========================================================

    private void ExpireCharge()
    {
        chargeExpired =
            true;

        liveCharge01 =
            0f;

        charge01 =
            0f;

        maximumChargeTimer =
            0f;

        /*
         * IMPORTANTE:
         *
         * NO liberamos el ActionCoordinator acá.
         *
         * LMB sigue físicamente apretado.
         *
         * El slot de LMB solamente se libera cuando
         * finalmente se suelte el botón.
         */
        if (hudController != null)
        {
            hudController.CancelAction(
                DualRingHUDController.ActionType.LMB
            );
        }

        if (logShotTiming)
        {
            Debug.Log(
                "[LMB CHARGE EXPIRED]"
            );
        }
    }

    // =========================================================
    // HUD
    // =========================================================

    private void UpdateShotHUD()
    {
        if (hudController == null)
        {
            return;
        }

        hudController.SetActionCharge(
            DualRingHUDController.ActionType.LMB,
            liveCharge01
        );
    }

    // =========================================================
    // RELEASE = CONTACT
    // =========================================================

    private void ReleaseContact()
    {
        if (!charging)
        {
            return;
        }

        /*
         * CONTACTO EXACTAMENTE AL RELEASE.
         */

        releaseTimestamp =
            InputState.currentTime;

        chargeDuration =
            Mathf.Max(
                0f,
                (float)(
                    releaseTimestamp -
                    chargeStartTimestamp
                )
            );

        charge01 =
            Mathf.Clamp01(
                liveCharge01
            );

        charging =
            false;

        waitingForContact =
            false;

        // -----------------------------------------------------
        // RANGE EXACTO AL SOLTAR
        // -----------------------------------------------------

        if (!CanContactBall())
        {
            liveCharge01 =
                0f;

            charge01 =
                0f;

            if (hudController != null)
            {
                hudController.CancelAction(
                    DualRingHUDController.ActionType.LMB
                );
            }

            if (actionCoordinator != null)
            {
                actionCoordinator.EndAction(
                    DualRingHUDController.ActionType.LMB
                );
            }

            if (logShotTiming)
            {
                Debug.Log(
                    "[LMB CONTACT MISSED] " +
                    "Ball outside contact range at release."
                );
            }

            ShotCancelled?.Invoke();

            return;
        }

        // -----------------------------------------------------
        // AIM
        // -----------------------------------------------------

        CaptureReleaseAim();

        // -----------------------------------------------------
        // HUD
        // -----------------------------------------------------

        if (hudController != null)
        {
            hudController.ReleaseAction(
                DualRingHUDController.ActionType.LMB
            );
        }

        // -----------------------------------------------------
        // END ACTION OWNERSHIP
        // -----------------------------------------------------

        if (actionCoordinator != null)
        {
            actionCoordinator.EndAction(
                DualRingHUDController.ActionType.LMB
            );
        }

        // -----------------------------------------------------
        // RELEASE EVENT
        // -----------------------------------------------------

        ShotReleased?.Invoke(
            this
        );

        // -----------------------------------------------------
        // IMMEDIATE CONTACT
        // -----------------------------------------------------

        ExecuteImmediateContact();
    }

    // =========================================================
    // AIM
    // =========================================================

    private void CaptureReleaseAim()
    {
        Vector3 forward =
            Vector3.forward;

        if (aimCamera != null)
        {
            forward =
                aimCamera.transform.forward;
        }
        else
        {
            forward =
                transform.forward;
        }

        /*
         * La cámara define solamente la dirección horizontal.
         *
         * La altura viene del gesto.
         */

        forward.y =
            0f;

        if (
            forward.sqrMagnitude <
            0.0001f
        )
        {
            forward =
                transform.forward;

            forward.y =
                0f;
        }

        forward.Normalize();

        releaseAimForward =
            forward;

        releaseAimRight =
            Vector3.Cross(
                Vector3.up,
                releaseAimForward
            ).normalized;
    }

    // =========================================================
    // IMMEDIATE CONTACT
    // =========================================================

    private void ExecuteImmediateContact()
    {
        /*
         * releaseTimestamp == contactTimestamp
         */

        double contactTimestamp =
            releaseTimestamp;

        ShotContactGesture gesture =
            new ShotContactGesture();

        gesture.releaseTimestamp =
            releaseTimestamp;

        gesture.contactTimestamp =
            contactTimestamp;

        gesture.preReleaseWindow =
            Mathf.Max(
                gestureLookbackTime,
                0f
            );

        gesture.windowStartTimestamp =
            releaseTimestamp -
            gesture.preReleaseWindow;

        gesture.windowDuration =
            gesture.preReleaseWindow;

        gesture.releaseToContactDuration =
            0f;

        gesture.chargeDuration =
            chargeDuration;

        gesture.charge01 =
            charge01;

        gesture.releaseAimForward =
            releaseAimForward;

        gesture.releaseAimRight =
            releaseAimRight;

        if (ball != null)
        {
            gesture.contactBallPosition =
                ball.transform.position;
        }
        else
        {
            gesture.contactBallPosition =
                transform.position;
        }

        // -----------------------------------------------------
        // LAST ~200ms
        // -----------------------------------------------------

        if (recorder != null)
        {
            gesture.capturedSampleCount =
                recorder.CaptureWindow(
                    gesture.windowStartTimestamp,
                    releaseTimestamp,
                    gesture.samples,
                    out gesture.rawDisplacement,
                    out gesture.rawTravelDistance
                );
        }

        lastContactGesture =
            gesture;

        if (logShotTiming)
        {
            Debug.Log(
                "[LMB IMMEDIATE CONTACT] " +

                "Charge01: " +
                gesture.charge01.ToString("F3") +

                " | Lookback: " +
                gesture.preReleaseWindow.ToString("F3") +
                " s" +

                " | Samples: " +
                gesture.capturedSampleCount +

                " | Raw displacement: " +
                gesture.rawDisplacement +

                " | Raw travel: " +
                gesture.rawTravelDistance.ToString("F2")
            );
        }

        ContactGestureReady?.Invoke(
            gesture
        );

        liveCharge01 =
            0f;

        maximumChargeTimer =
            0f;
    }

    // =========================================================
    // COMPATIBILITY
    // =========================================================

    public void TriggerBallContact()
    {
        if (!waitingForContact)
        {
            return;
        }

        waitingForContact =
            false;

        ExecuteImmediateContact();
    }

    // =========================================================
    // EXPIRED RELEASE
    // =========================================================

    private void FinishExpiredCharge()
    {
        charging =
            false;

        chargeExpired =
            false;

        waitingForContact =
            false;

        liveCharge01 =
            0f;

        charge01 =
            0f;

        chargeDuration =
            0f;

        maximumChargeTimer =
            0f;

        if (hudController != null)
        {
            hudController.CancelAction(
                DualRingHUDController.ActionType.LMB
            );
        }

        if (actionCoordinator != null)
        {
            actionCoordinator.EndAction(
                DualRingHUDController.ActionType.LMB
            );
        }
    }

    // =========================================================
    // CANCEL FROM CONFLICT
    // =========================================================

    public void CancelChargeFromConflict()
    {
        charging =
            false;

        chargeExpired =
            false;

        waitingForContact =
            false;

        liveCharge01 =
            0f;

        charge01 =
            0f;

        chargeDuration =
            0f;

        maximumChargeTimer =
            0f;

        /*
         * No EndAction acá.
         *
         * El BallActionCoordinator es quien está
         * procesando el conflicto y controlando el lock.
         */

        if (hudController != null)
        {
            hudController.CancelAction(
                DualRingHUDController.ActionType.LMB
            );
        }
    }

    // =========================================================
    // BALL RANGE
    // =========================================================

    private bool CanContactBall()
    {
        if (ball == null)
        {
            return false;
        }

        Vector3 difference =
            ball.transform.position -
            transform.position;

        float vertical =
            Mathf.Abs(
                difference.y
            );

        difference.y =
            0f;

        float horizontal =
            difference.magnitude;

        if (
            horizontal >
            maximumBallDistance
        )
        {
            return false;
        }

        if (
            vertical >
            maximumBallVerticalDifference
        )
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // EXTERNAL CANCEL
    // =========================================================

    public void CancelPendingShot()
    {
        charging =
            false;

        chargeExpired =
            false;

        waitingForContact =
            false;

        liveCharge01 =
            0f;

        charge01 =
            0f;

        chargeDuration =
            0f;

        maximumChargeTimer =
            0f;

        if (hudController != null)
        {
            hudController.CancelAction(
                DualRingHUDController.ActionType.LMB
            );
        }

        if (actionCoordinator != null)
        {
            actionCoordinator.EndAction(
                DualRingHUDController.ActionType.LMB
            );
        }

        ShotCancelled?.Invoke();
    }
}