using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central coordinator for mutually exclusive ball actions.
///
/// RULE:
///
/// - Only one ball action can charge at a time.
/// - If another action is pressed while one is active:
///     BOTH are cancelled.
/// - After a conflict, no action can start until
///   LMB, RMB and MMB have all been released.
///
/// This prevents:
///
/// LMB -> press RMB -> RMB starts immediately.
///
/// Instead:
///
/// LMB -> press RMB -> everything cancelled
/// -> release buttons
/// -> press again to start.
/// </summary>
public class BallActionCoordinator : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public ShotAimContext shotAimContext;

    public PlayerBallControl playerBallControl;

    public BallLiftControl ballLiftControl;

    public DualRingHUDController hudController;

    // =========================================================
    // STATE
    // =========================================================

    private DualRingHUDController.ActionType currentAction =
        DualRingHUDController.ActionType.None;

    private bool conflictLocked;

    // =========================================================
    // PUBLIC
    // =========================================================

    public DualRingHUDController.ActionType CurrentAction
    {
        get
        {
            return currentAction;
        }
    }

    public bool IsConflictLocked
    {
        get
        {
            return conflictLocked;
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

        /*
         * After a conflict, ALL action buttons must
         * physically return to released state.
         */
        if (
            conflictLocked &&
            AreAllActionButtonsReleased()
        )
        {
            conflictLocked =
                false;

            currentAction =
                DualRingHUDController.ActionType.None;

            if (hudController != null)
            {
                hudController.ClearActionImmediately();
            }

            Debug.Log(
                "[BALL ACTIONS UNLOCKED] " +
                "All mouse action buttons were released."
            );
        }
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

        if (ballLiftControl == null)
        {
            ballLiftControl =
                GetComponent<BallLiftControl>();
        }

        if (hudController == null)
        {
            hudController =
                FindObjectOfType<DualRingHUDController>();
        }
    }

    // =========================================================
    // TRY BEGIN
    // =========================================================

    public bool TryBeginAction(
        DualRingHUDController.ActionType action
    )
    {
        if (
            action ==
            DualRingHUDController.ActionType.None
        )
        {
            return false;
        }

        // -----------------------------------------------------
        // LOCKED AFTER PREVIOUS CONFLICT
        // -----------------------------------------------------

        if (conflictLocked)
        {
            return false;
        }

        // -----------------------------------------------------
        // NOTHING ACTIVE
        // -----------------------------------------------------

        if (
            currentAction ==
            DualRingHUDController.ActionType.None
        )
        {
            currentAction =
                action;

            return true;
        }

        // -----------------------------------------------------
        // SAME ACTION
        // -----------------------------------------------------

        if (
            currentAction ==
            action
        )
        {
            return true;
        }

        // -----------------------------------------------------
        // CONFLICT
        // -----------------------------------------------------

        /*
         * Another action was already active.
         *
         * Do NOT switch to the new action.
         *
         * Cancel absolutely everything and wait until
         * all action buttons are released.
         */

        CancelEverythingForConflict();

        conflictLocked =
            true;

        currentAction =
            DualRingHUDController.ActionType.None;

        Debug.Log(
            "[BALL ACTION CONFLICT] " +
            "All actions cancelled. " +
            "Release LMB/RMB/MMB before trying again."
        );

        return false;
    }

    // =========================================================
    // NORMAL END
    // =========================================================

    public void EndAction(
        DualRingHUDController.ActionType action
    )
    {
        if (
            conflictLocked
        )
        {
            return;
        }

        if (
            currentAction !=
            action
        )
        {
            return;
        }

        currentAction =
            DualRingHUDController.ActionType.None;
    }

    // =========================================================
    // CONFLICT
    // =========================================================

    private void CancelEverythingForConflict()
    {
        if (shotAimContext != null)
        {
            shotAimContext.CancelChargeFromConflict();
        }

        if (playerBallControl != null)
        {
            playerBallControl.CancelChargeFromConflict();
        }

        if (ballLiftControl != null)
        {
            ballLiftControl.CancelChargeFromConflict();
        }

        if (hudController != null)
        {
            hudController.ClearActionImmediately();
        }
    }

    // =========================================================
    // BUTTON STATE
    // =========================================================

    private bool AreAllActionButtonsReleased()
    {
        Mouse mouse =
            Mouse.current;

        if (mouse != null)
        {
            return
                !mouse.leftButton.isPressed &&
                !mouse.rightButton.isPressed &&
                !mouse.middleButton.isPressed;
        }

        /*
         * Compatibility fallback.
         */
        return
            !Input.GetMouseButton(0) &&
            !Input.GetMouseButton(1) &&
            !Input.GetMouseButton(2);
    }
}