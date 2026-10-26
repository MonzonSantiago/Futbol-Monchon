using UnityEngine;

/// <summary>
/// Third-person camera for the player.
///
/// DESIGN RULE:
///
/// The camera is ALWAYS free.
///
/// Even while LMB is being held for a shot:
///
/// - Mouse still rotates the camera.
/// - ShotGestureRecorder simultaneously records
///   the complete mouse gesture.
/// - The camera is never frozen by the shot system.
///
/// This is intentional because the mouse movement
/// is part of the player's shooting technique.
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    // =========================================================
    // TARGET
    // =========================================================

    [Header("Target")]

    public Transform target;

    // =========================================================
    // DISTANCE
    // =========================================================

    [Header("Distance")]

    public float distance = 5.5f;

    public float targetHeight = 1.3f;

    // =========================================================
    // MOUSE
    // =========================================================

    [Header("Mouse")]

    public float mouseSensitivity = 180f;

    // =========================================================
    // VERTICAL LIMITS
    // =========================================================

    [Header("Vertical Limits")]

    public float minPitch = -15f;

    public float maxPitch = 65f;

    // =========================================================
    // SMOOTHING
    // =========================================================

    [Header("Smoothing")]

    public float smoothTime = 0.04f;

    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private float yaw;

    private float pitch = 15f;

    private Vector3 smoothVelocity;

    // =========================================================
    // PUBLIC INFORMATION
    // =========================================================

    /// <summary>
    /// Current camera yaw.
    ///
    /// Later this can also be useful for multiplayer
    /// shot debugging and replay systems.
    /// </summary>
    public float Yaw
    {
        get { return yaw; }
    }

    /// <summary>
    /// Current camera pitch.
    /// </summary>
    public float Pitch
    {
        get { return pitch; }
    }

    // =========================================================
    // UNITY
    // =========================================================

    private void Start()
    {
        yaw =
            transform.eulerAngles.y;

        /*
         * Recover a useful starting pitch from the
         * current camera orientation.
         */
        float currentPitch =
            transform.eulerAngles.x;

        if (currentPitch > 180f)
        {
            currentPitch -= 360f;
        }

        pitch =
            Mathf.Clamp(
                currentPitch,
                minPitch,
                maxPitch
            );

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;
    }

    private void LateUpdate()
    {
        HandleCursor();

        if (target == null)
            return;

        /*
         * IMPORTANT:
         *
         * Mouse rotation is ALWAYS processed.
         *
         * There is intentionally no condition here
         * checking whether a shot is being charged.
         */
        HandleMouse();

        UpdateCamera();
    }

    // =========================================================
    // MOUSE
    // =========================================================

    private void HandleMouse()
    {
        if (
            Cursor.lockState !=
            CursorLockMode.Locked
        )
        {
            return;
        }

        float mouseX =
            Input.GetAxis(
                "Mouse X"
            );

        float mouseY =
            Input.GetAxis(
                "Mouse Y"
            );

        yaw +=
            mouseX *
            mouseSensitivity *
            Time.deltaTime;

        pitch -=
            mouseY *
            mouseSensitivity *
            Time.deltaTime;

        pitch =
            Mathf.Clamp(
                pitch,
                minPitch,
                maxPitch
            );
    }

    // =========================================================
    // CAMERA POSITION
    // =========================================================

    private void UpdateCamera()
    {
        Vector3 focusPoint =
            target.position +
            Vector3.up *
            targetHeight;

        Quaternion cameraRotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );

        Vector3 desiredPosition =
            focusPoint -
            cameraRotation *
            Vector3.forward *
            distance;

        transform.position =
            Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref smoothVelocity,
                smoothTime
            );

        Vector3 lookDirection =
            focusPoint -
            transform.position;

        if (
            lookDirection.sqrMagnitude >
            0.0001f
        )
        {
            transform.rotation =
                Quaternion.LookRotation(
                    lookDirection,
                    Vector3.up
                );
        }
    }

    // =========================================================
    // CURSOR
    // =========================================================

    private void HandleCursor()
    {
        if (
            Input.GetKeyDown(
                KeyCode.Escape
            )
        )
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible =
                true;
        }

        if (
            Input.GetMouseButtonDown(0) &&
            Cursor.lockState !=
            CursorLockMode.Locked
        )
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible =
                false;
        }
    }
}