using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    // =========================================================
    // MOVIMIENTO
    // =========================================================

    [Header("Velocidad")]
    public float walkSpeed = 5.5f;
    public float sprintSpeed = 8.0f;

    [Header("Aceleracion")]
    public float acceleration = 18.0f;
    public float deceleration = 24.0f;

    [Header("Rotacion")]
    public float rotationSpeed = 1080.0f;

    [Header("Salto")]
    public float jumpHeight = 1.2f;
    public float gravity = -24.0f;

    // =========================================================
    // STAMINA
    // =========================================================

    [Header("Stamina")]

    [Tooltip("Cantidad máxima de stamina.")]
    public float maxStamina = 100.0f;

    [Tooltip("Cuánta stamina gasta por segundo mientras corre.")]
    public float staminaDrainPerSecond = 20.0f;

    [Tooltip("Cuánta stamina recupera por segundo cuando no está corriendo.")]
    public float staminaRecoveryPerSecond = 18.0f;

    [Tooltip("Tiempo después de dejar de correr antes de empezar a recuperar stamina.")]
    public float staminaRecoveryDelay = 0.4f;

    [Tooltip(
        "Porcentaje mínimo de stamina necesario para volver a correr " +
        "después de quedarse completamente sin stamina."
    )]
    [Range(0.0f, 1.0f)]
    public float staminaToResumeSprint = 0.25f;

    // =========================================================
    // REFERENCIAS
    // =========================================================

    [Header("Referencias")]

    public Transform cameraTransform;

    [Tooltip("HUD de los dos círculos.")]
    public DualRingHUDController hudController;

    // =========================================================
    // PRIVATE
    // =========================================================

    private CharacterController controller;

    private Vector3 horizontalVelocity;

    private float verticalVelocity;

    private float currentStamina;

    private float staminaRecoveryTimer;

    private bool exhausted;

    private bool isSprinting;

    // =========================================================
    // PUBLIC
    // =========================================================

    public Vector3 HorizontalVelocity
    {
        get
        {
            return horizontalVelocity;
        }
    }

    public float CurrentStamina
    {
        get
        {
            return currentStamina;
        }
    }

    public float Stamina01
    {
        get
        {
            if (maxStamina <= 0.0f)
            {
                return 0.0f;
            }

            return currentStamina / maxStamina;
        }
    }

    public bool IsSprinting
    {
        get
        {
            return isSprinting;
        }
    }

    public bool IsExhausted
    {
        get
        {
            return exhausted;
        }
    }

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        controller =
            GetComponent<CharacterController>();

        currentStamina =
            maxStamina;

        staminaRecoveryTimer =
            0.0f;

        exhausted =
            false;

        isSprinting =
            false;
    }

    private void Start()
    {
        UpdateStaminaHUD();
    }

    private void Update()
    {
        HandleFacing();

        HandleMovementAndStamina();

        HandleGravityAndJump();

        UpdateStaminaHUD();
    }

    // =========================================================
    // FACING
    // =========================================================

    private void HandleFacing()
    {
        if (cameraTransform == null)
        {
            return;
        }

        // El jugador mira en la misma dirección horizontal
        // que la cámara.

        Vector3 cameraForward =
            cameraTransform.forward;

        cameraForward.y =
            0.0f;

        if (
            cameraForward.sqrMagnitude <
            0.001f
        )
        {
            return;
        }

        cameraForward.Normalize();

        Quaternion targetRotation =
            Quaternion.LookRotation(
                cameraForward,
                Vector3.up
            );

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );
    }

    // =========================================================
    // MOVIMIENTO + STAMINA
    // =========================================================

    private void HandleMovementAndStamina()
    {
        float horizontal =
            Input.GetAxisRaw(
                "Horizontal"
            );

        float vertical =
            Input.GetAxisRaw(
                "Vertical"
            );

        Vector3 input =
            new Vector3(
                horizontal,
                0.0f,
                vertical
            );

        input =
            Vector3.ClampMagnitude(
                input,
                1.0f
            );

        Vector3 movementDirection =
            Vector3.zero;

        if (cameraTransform != null)
        {
            Vector3 forward =
                cameraTransform.forward;

            Vector3 right =
                cameraTransform.right;

            forward.y =
                0.0f;

            right.y =
                0.0f;

            forward.Normalize();

            right.Normalize();

            movementDirection =
                forward *
                vertical +
                right *
                horizontal;

            movementDirection =
                Vector3.ClampMagnitude(
                    movementDirection,
                    1.0f
                );
        }
        else
        {
            movementDirection =
                input;
        }

        bool hasMovementInput =
            movementDirection.sqrMagnitude >
            0.01f;

        bool wantsToSprint =
            Input.GetKey(
                KeyCode.LeftShift
            );

        // =====================================================
        // RECUPERAR DEL AGOTAMIENTO
        // =====================================================

        if (exhausted)
        {
            float requiredStamina =
                maxStamina *
                staminaToResumeSprint;

            if (
                currentStamina >=
                requiredStamina
            )
            {
                exhausted =
                    false;
            }
        }

        // =====================================================
        // ¿PUEDE CORRER?
        // =====================================================

        bool canSprint =
            !exhausted &&
            currentStamina >
            0.0f;

        isSprinting =
            wantsToSprint &&
            hasMovementInput &&
            canSprint;

        // =====================================================
        // STAMINA
        // =====================================================

        if (isSprinting)
        {
            currentStamina -=
                staminaDrainPerSecond *
                Time.deltaTime;

            currentStamina =
                Mathf.Max(
                    currentStamina,
                    0.0f
                );

            staminaRecoveryTimer =
                staminaRecoveryDelay;

            // Llegamos a 0.
            if (
                currentStamina <=
                0.0f
            )
            {
                currentStamina =
                    0.0f;

                exhausted =
                    true;

                isSprinting =
                    false;
            }
        }
        else
        {
            // Pequeño retraso antes de comenzar
            // a regenerar stamina.

            if (
                staminaRecoveryTimer >
                0.0f
            )
            {
                staminaRecoveryTimer -=
                    Time.deltaTime;
            }
            else
            {
                currentStamina +=
                    staminaRecoveryPerSecond *
                    Time.deltaTime;

                currentStamina =
                    Mathf.Min(
                        currentStamina,
                        maxStamina
                    );
            }
        }

        // =====================================================
        // VELOCIDAD
        // =====================================================

        float targetSpeed =
            isSprinting
                ? sprintSpeed
                : walkSpeed;

        Vector3 targetVelocity =
            movementDirection *
            targetSpeed;

        float currentAcceleration =
            movementDirection.sqrMagnitude >
            0.01f
                ? acceleration
                : deceleration;

        horizontalVelocity =
            Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                currentAcceleration *
                Time.deltaTime
            );

        Vector3 movement =
            horizontalVelocity;

        movement.y =
            verticalVelocity;

        controller.Move(
            movement *
            Time.deltaTime
        );
    }

    // =========================================================
    // GRAVEDAD + SALTO
    // =========================================================

    private void HandleGravityAndJump()
    {
        if (
            controller.isGrounded &&
            verticalVelocity <
            0.0f
        )
        {
            verticalVelocity =
                -2.0f;
        }

        if (
            controller.isGrounded &&
            Input.GetKeyDown(
                KeyCode.Space
            )
        )
        {
            verticalVelocity =
                Mathf.Sqrt(
                    jumpHeight *
                    -2.0f *
                    gravity
                );
        }

        verticalVelocity +=
            gravity *
            Time.deltaTime;
    }

    // =========================================================
    // HUD
    // =========================================================

    private void UpdateStaminaHUD()
    {
        if (hudController == null)
        {
            return;
        }

        hudController.SetStamina01(
            Stamina01
        );
    }
}