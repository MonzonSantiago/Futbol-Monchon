using UnityEngine;

public class DualRingHUDController : MonoBehaviour
{
    public enum ActionType
    {
        None,
        RMB,
        LMB,
        Wheel
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("HUD References")]

    [SerializeField]
    private UIMeterRing staminaFill;

    [SerializeField]
    private UIMeterRing actionFill;

    // =========================================================
    // COLORS
    // =========================================================

    [Header("Colors")]

    [SerializeField]
    private Color staminaColor =
        new Color(
            0.45f,
            0.82f,
            0.28f,
            1f
        );

    [SerializeField]
    private Color neutralActionColor =
        new Color(
            0.18f,
            0.18f,
            0.18f,
            1f
        );

    [SerializeField]
    private Color rmbColor =
        new Color(
            0.10f,
            0.45f,
            1.00f,
            1f
        );

    [SerializeField]
    private Color lmbColor =
        new Color(
            0.92f,
            0.14f,
            0.14f,
            1f
        );

    [SerializeField]
    private Color wheelColor =
        new Color(
            1.00f,
            0.78f,
            0.08f,
            1f
        );

    // =========================================================
    // STATE
    // =========================================================

    private ActionType currentAction =
        ActionType.None;

    private float displayedActionCharge =
        0f;

    // =========================================================
    // PUBLIC
    // =========================================================

    public ActionType CurrentAction
    {
        get
        {
            return currentAction;
        }
    }

    public float DisplayedActionCharge
    {
        get
        {
            return displayedActionCharge;
        }
    }

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (staminaFill != null)
        {
            staminaFill.color =
                staminaColor;
        }

        ClearActionImmediately();
    }

    // =========================================================
    // STAMINA
    // =========================================================

    public void SetStamina01(
        float stamina01
    )
    {
        if (staminaFill == null)
        {
            return;
        }

        staminaFill.color =
            staminaColor;

        staminaFill.Fill01 =
            Mathf.Clamp01(
                stamina01
            );
    }

    // =========================================================
    // ACTION CHARGE
    // =========================================================

    public void SetActionCharge(
        ActionType action,
        float charge01
    )
    {
        if (actionFill == null)
        {
            return;
        }

        if (
            action ==
            ActionType.None
        )
        {
            ClearActionImmediately();

            return;
        }

        currentAction =
            action;

        displayedActionCharge =
            Mathf.Clamp01(
                charge01
            );

        ApplyActionColor(
            action
        );

        actionFill.Fill01 =
            displayedActionCharge;
    }

    // =========================================================
    // RELEASE
    // =========================================================

    /// <summary>
    /// The action that is currently displayed is released.
    ///
    /// There is NO drain animation anymore.
    ///
    /// Release:
    ///     immediately returns to neutral.
    /// </summary>
    public void ReleaseAction(
        ActionType action
    )
    {
        if (
            currentAction !=
            action
        )
        {
            return;
        }

        ClearActionImmediately();
    }

    // =========================================================
    // CANCEL SPECIFIC ACTION
    // =========================================================

    /// <summary>
    /// Safely clears one action only if that action currently
    /// owns the HUD.
    ///
    /// This prevents an old cancelled input from clearing
    /// a newer action that has already started.
    /// </summary>
    public void CancelAction(
        ActionType action
    )
    {
        if (
            currentAction !=
            action
        )
        {
            return;
        }

        ClearActionImmediately();
    }

    // =========================================================
    // GLOBAL CLEAR
    // =========================================================

    public void ClearActionImmediately()
    {
        currentAction =
            ActionType.None;

        displayedActionCharge =
            0f;

        if (actionFill != null)
        {
            actionFill.color =
                neutralActionColor;

            actionFill.Fill01 =
                0f;
        }
    }

    // =========================================================
    // COLOR
    // =========================================================

    private void ApplyActionColor(
        ActionType action
    )
    {
        if (actionFill == null)
        {
            return;
        }

        switch (action)
        {
            case ActionType.RMB:

                actionFill.color =
                    rmbColor;

                break;

            case ActionType.LMB:

                actionFill.color =
                    lmbColor;

                break;

            case ActionType.Wheel:

                actionFill.color =
                    wheelColor;

                break;

            default:

                actionFill.color =
                    neutralActionColor;

                break;
        }
    }
}