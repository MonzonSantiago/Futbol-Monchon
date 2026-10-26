using UnityEngine;

/// <summary>
/// Local execution bridge between ShotIntent and FootballBall.
///
/// CURRENT SINGLEPLAYER:
///
/// ShotIntent
///     -> ShotBallExecutor
///     -> FootballBall
///
/// FUTURE MULTIPLAYER:
///
/// ShotIntent
///     -> network request
///     -> server validation
///     -> authoritative FootballBall
///
/// Local prediction can later reuse this same component.
/// </summary>
public class ShotBallExecutor : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public ShotIntentBuilder intentBuilder;

    public FootballBall ball;

    // =========================================================
    // EXECUTION
    // =========================================================

    [Header("Execution")]

    [Tooltip(
        "Execute the shot immediately on the local ball. " +
        "Later this becomes local prediction in multiplayer."
    )]
    public bool executeLocally = true;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    public bool logExecution = true;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        FindReferences();
    }

    private void OnEnable()
    {
        FindReferences();

        if (intentBuilder != null)
        {
            intentBuilder.ShotIntentReady +=
                HandleShotIntent;
        }
    }

    private void OnDisable()
    {
        if (intentBuilder != null)
        {
            intentBuilder.ShotIntentReady -=
                HandleShotIntent;
        }
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (intentBuilder == null)
        {
            intentBuilder =
                GetComponent<ShotIntentBuilder>();
        }
    }

    // =========================================================
    // EXECUTE
    // =========================================================

    private void HandleShotIntent(
        ShotIntent intent
    )
    {
        if (
            intent == null ||
            ball == null
        )
        {
            return;
        }

        if (logExecution)
        {
            Debug.Log(
                "[SHOT EXECUTOR] " +
                "Power: " +
                intent.power01.ToString("F3") +
                " | Curve H: " +
                intent.horizontalEffect01.ToString("F3") +
                " | Height V: " +
                intent.verticalEffect01.ToString("F3")
            );
        }

        if (executeLocally)
        {
            ball.ExecuteShotIntent(
                intent
            );
        }

        /*
         * MULTIPLAYER LATER:
         *
         * SendShotIntentToServer(intent);
         */
    }
}