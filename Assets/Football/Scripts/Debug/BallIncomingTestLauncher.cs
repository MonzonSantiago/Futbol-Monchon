using UnityEngine;

/// <summary>
/// TEMPORARY DEVELOPMENT TOOL.
///
/// Press T during Play Mode:
///
/// - places the ball several meters in front of the player
/// - launches it strongly toward the player's contact area
///
/// This script is ONLY for testing first-time contacts.
/// It does not belong to final gameplay.
/// </summary>
[RequireComponent(typeof(FootballBall))]
public class BallIncomingTestLauncher : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    private FootballBall footballBall;

    private Rigidbody rb;

    private ShotAimContext playerShotContext;

    private Transform player;

    // =========================================================
    // TEST VALUES
    // =========================================================

    [Header("Incoming Ball Test")]

    public KeyCode launchKey = KeyCode.T;

    [Tooltip(
        "How far in front of the player the ball is spawned."
    )]
    public float spawnDistance = 7f;

    [Tooltip(
        "Incoming ball speed."
    )]
    public float incomingSpeed = 16f;

    [Tooltip(
        "Target distance in front of the player."
    )]
    public float targetDistanceFromPlayer = 0.55f;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        footballBall =
            GetComponent<FootballBall>();

        rb =
            GetComponent<Rigidbody>();

        FindPlayer();
    }

    private void Update()
    {
        if (player == null)
        {
            FindPlayer();
        }

        if (
            Input.GetKeyDown(
                launchKey
            )
        )
        {
            LaunchIncomingBall();
        }
    }

    // =========================================================
    // FIND PLAYER
    // =========================================================

    private void FindPlayer()
    {
        playerShotContext =
            FindObjectOfType<ShotAimContext>();

        if (playerShotContext != null)
        {
            player =
                playerShotContext.transform;
        }
    }

    // =========================================================
    // LAUNCH
    // =========================================================

    private void LaunchIncomingBall()
    {
        if (
            player == null ||
            rb == null
        )
        {
            Debug.LogWarning(
                "[TEST INCOMING BALL] Player or Rigidbody not found."
            );

            return;
        }

        if (footballBall != null)
        {
            footballBall.StopShotAssistance();
        }

        Vector3 playerForward =
            player.forward;

        playerForward.y =
            0f;

        if (
            playerForward.sqrMagnitude <
            0.001f
        )
        {
            playerForward =
                Vector3.forward;
        }

        playerForward.Normalize();

        // -----------------------------------------------------
        // SPAWN IN FRONT OF PLAYER
        // -----------------------------------------------------

        Vector3 spawnPosition =
            player.position +
            playerForward *
            spawnDistance;

        /*
         * Keep the current ball ground height.
         *
         * We only move it horizontally.
         */
        spawnPosition.y =
            transform.position.y;

        transform.position =
            spawnPosition;

        // -----------------------------------------------------
        // TARGET JUST IN FRONT OF PLAYER
        // -----------------------------------------------------

        Vector3 targetPosition =
            player.position +
            playerForward *
            targetDistanceFromPlayer;

        targetPosition.y =
            spawnPosition.y;

        Vector3 incomingDirection =
            targetPosition -
            spawnPosition;

        incomingDirection.y =
            0f;

        incomingDirection.Normalize();

        // -----------------------------------------------------
        // RESET BALL PHYSICS
        // -----------------------------------------------------

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        // -----------------------------------------------------
        // FIRE
        // -----------------------------------------------------

        rb.velocity =
            incomingDirection *
            incomingSpeed;

        Debug.Log(
            "[TEST INCOMING BALL] " +
            "Speed: " +
            incomingSpeed.ToString("F2") +
            " m/s" +
            " | Toward player contact area."
        );
    }
}