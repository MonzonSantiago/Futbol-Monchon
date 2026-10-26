using UnityEngine;

/// <summary>
/// Physical football.
///
/// UNIVERSAL LMB CONTACT MODEL
///
/// ---------------------------------------------------------
/// INPUT
/// ---------------------------------------------------------
///
/// LMB charge:
///     forward contact authority / velocity.
///
/// Horizontal flick:
///     continuous curve.
///
/// Vertical flick:
///     vertical velocity contribution.
///
/// Incoming ball momentum:
///     preserved, redirected or cancelled depending on contact.
///
/// ---------------------------------------------------------
/// FIRST-TIME CONTACT
/// ---------------------------------------------------------
///
/// A fast incoming ball is NOT automatically converted
/// into a brand-new shot.
///
/// Weak contact:
///     can preserve, redirect or cancel incoming momentum.
///
/// Strong contact:
///     player's desired direction increasingly dominates.
///
/// If incoming velocity opposes the desired direction,
/// a low-power contact naturally approaches momentum
/// cancellation.
///
/// This produces trapping / cushioning WITHOUT a
/// predefined "trap" action.
///
/// ---------------------------------------------------------
/// IMPORTANT
/// ---------------------------------------------------------
///
/// There are NO predefined:
///
/// - pass
/// - shot
/// - trap
/// - first-time shot
/// - volley
/// - chip
///
/// Everything is continuous and deterministic.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class FootballBall : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public Rigidbody rb;

    private SphereCollider sphereCollider;

    // =========================================================
    // ROLLING
    // =========================================================

    [Header("Rolling")]

    public float groundDeceleration = 0.75f;

    public float speedDecelerationFactor = 0.10f;

    public float stopSpeed = 0.08f;

    // =========================================================
    // RMB
    // =========================================================

    [Header("RMB - Dribble")]

    public float minTouchSpeed = 2.35f;

    public float maxTouchSpeed = 5.90f;

    public float touchSpeedExponent = 1.18f;

    [Header("RMB - Player Velocity Carry")]

    public float playerVelocityCarry = 1.0f;

    public float maxPlayerVelocityCarry = 10.0f;

    [Header("RMB - Steering")]

    [Range(0f, 1f)]
    public float minSteeringAuthority = 0.28f;

    [Range(0f, 1f)]
    public float maxSteeringAuthority = 0.78f;

    [Range(0f, 1f)]
    public float minReverseMomentumCancel = 0.42f;

    [Range(0f, 1f)]
    public float maxReverseMomentumCancel = 0.88f;

    // =========================================================
    // LMB FORWARD POWER
    // =========================================================

    [Header("LMB - Forward Power")]

    public float minShotSpeed = 6.20f;

    public float maxShotSpeed = 28f;

    public float shotPowerExponent = 0.72f;

    // =========================================================
    // LMB VERTICAL FLICK
    // =========================================================

    [Header("LMB - Vertical Flick")]

    public float baseVerticalLaunchSpeed = 0.12f;

    public float maxUpwardFlickSpeed = 6.50f;

    public float maxDownwardFlickSpeed = 4.50f;

    public float verticalFlickResponseExponent = 0.90f;

    [Range(0f, 0.25f)]
    public float minimumVerticalEffect = 0.025f;

    // =========================================================
    // AIRBORNE CONTACT
    // =========================================================

    [Header("LMB - Airborne Contact")]

    public float airborneBlendStartHeight = 0.22f;

    public float fullAirborneBlendHeight = 0.85f;

    [Range(0f, 1f)]
    public float aerialIncomingVerticalCarryLowAuthority = 0.92f;

    [Range(0f, 1f)]
    public float aerialIncomingVerticalCarryHighAuthority = 0.58f;

    public float aerialFlickVelocityMultiplier = 1.0f;

    public float maximumAerialVerticalSpeed = 9.0f;

    // =========================================================
    // INCOMING MOMENTUM
    // =========================================================

    [Header("LMB - Incoming Momentum")]

    public float incomingMomentumActivationSpeed = 0.75f;

    [Range(0f, 1f)]
    public float minimumContactAuthority = 0.18f;

    [Range(0f, 1f)]
    public float maximumContactAuthority = 0.96f;

    public float contactAuthorityExponent = 0.85f;

    [Range(0f, 1f)]
    public float verticalGestureAuthorityFloor = 0.35f;

    // =========================================================
    // FIRST-TIME / SOFT CONTACT
    // =========================================================

    [Header("LMB - First Time Contact")]

    [Tooltip(
        "Incoming planar speed where first-time momentum " +
        "cancellation starts becoming relevant."
    )]
    public float firstTimeIncomingSpeedStart = 3.5f;

    [Tooltip(
        "Incoming planar speed where first-time momentum " +
        "response is fully active."
    )]
    public float firstTimeIncomingSpeedFull = 14f;

    [Tooltip(
        "Above this LMB power, automatic momentum cancellation " +
        "fades away and the player's normal strong contact dominates."
    )]
    [Range(0.05f, 0.8f)]
    public float softContactPowerEnd = 0.45f;

    [Tooltip(
        "Incoming direction must oppose the desired direction " +
        "by at least this amount before cushioning begins."
    )]
    [Range(0f, 1f)]
    public float oppositionStart = 0.10f;

    [Tooltip(
        "Opposition amount considered fully opposing."
    )]
    [Range(0.1f, 1f)]
    public float oppositionFull = 0.85f;

    [Tooltip(
        "Small amount of desired follow-through even during " +
        "an almost perfect momentum cancellation."
    )]
    [Range(0f, 1f)]
    public float minimumSoftFollowThrough = 0.08f;

    [Tooltip(
        "Maximum follow-through while still inside the " +
        "low-power cushioning range."
    )]
    [Range(0f, 1f)]
    public float maximumSoftFollowThrough = 0.36f;

    public float softFollowThroughExponent = 0.85f;

    public float softContactInfluenceExponent = 0.85f;

    // =========================================================
    // PREVIEW DISTANCE
    // =========================================================

    [Header("LMB - Preview Distance")]

    public float minCurveDistance = 10f;

    public float maxCurveDistance = 28f;

    // =========================================================
    // CURVE
    // =========================================================

    [Header("LMB - Curve")]

    public float maxCurveTurnRateDegrees = 30f;

    public float curveReferenceSpeed = 24f;

    [Range(0f, 1f)]
    public float minimumCurveSpeedFactor = 0.55f;

    public float curveBuildTime = 0.055f;

    public float curveDecayPerSecond = 0.10f;

    public float maximumCurveDuration = 2.5f;

    public float minimumCurveBallSpeed = 0.25f;

    [Range(0f, 0.25f)]
    public float minimumHorizontalCurveEffect = 0.035f;

    [Range(0.35f, 2f)]
    public float curveResponseExponent = 0.78f;

    // =========================================================
    // DIAGONAL
    // =========================================================

    [Header("LMB - Diagonal")]

    [Range(1f, 1.5f)]
    public float diagonalCurveBoost = 1.10f;

    [Range(0f, 2f)]
    public float diagonalBoostExponent = 1f;

    // =========================================================
    // VISUAL SPIN
    // =========================================================

    [Header("Visual Spin")]

    public bool enableSideSpinTorque = true;

    public float maxSideSpinVelocity = 20f;

    // =========================================================
    // GROUND
    // =========================================================

    [Header("Ground Detection")]

    public LayerMask groundMask = ~0;

    public float groundCheckExtra = 0.03f;

    public float maxGroundVerticalSpeed = 1.4f;

    // =========================================================
    // GROUND HEIGHT REFERENCE
    // =========================================================

    private bool hasGroundHeightReference;

    private float lastGroundedBallCenterY;

    // =========================================================
    // ACTIVE CURVE
    // =========================================================

    private bool horizontalCurveActive;

    private ShotIntent activeShotIntent;

    private float activeCurveDirection;

    private float activeCurveStartTime;

    private bool loggedFirstCurveStep;

    // =========================================================
    // LAST CONTACT DEBUG
    // =========================================================

    public ShotIntent LastExecutedIntent
    {
        get;
        private set;
    }

    public Vector3 LastIncomingContactVelocity
    {
        get;
        private set;
    }

    public Vector3 LastDesiredContactVelocity
    {
        get;
        private set;
    }

    public Vector3 LastContactResultVelocity
    {
        get;
        private set;
    }

    public float LastContactAuthority
    {
        get;
        private set;
    }

    public float LastBaseContactAuthority
    {
        get;
        private set;
    }

    public float LastPlanarContactAuthority
    {
        get;
        private set;
    }

    public float LastVerticalContactAuthority
    {
        get;
        private set;
    }

    public float LastContactBallHeight
    {
        get;
        private set;
    }

    public float LastAirborneBlend
    {
        get;
        private set;
    }

    public float LastOpposition01
    {
        get;
        private set;
    }

    public float LastSoftContactInfluence01
    {
        get;
        private set;
    }

    public float LastNeutralizationAuthority
    {
        get;
        private set;
    }

    // =========================================================
    // DEFAULT VERSION
    // =========================================================

    [SerializeField]
    [HideInInspector]
    private int contactDefaultsVersion = 0;

    private const int CurrentContactDefaultsVersion = 11;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        ApplyCurrentDefaults();

        if (rb == null)
        {
            rb =
                GetComponent<Rigidbody>();
        }

        sphereCollider =
            GetComponent<SphereCollider>();

        if (rb != null)
        {
            rb.maxAngularVelocity =
                Mathf.Max(
                    rb.maxAngularVelocity,
                    50f
                );
        }

        InitializeGroundHeightReference();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyCurrentDefaults();
    }
#endif

    private void FixedUpdate()
    {
        bool grounded =
            IsGrounded();

        if (grounded)
        {
            UpdateGroundHeightReference();
        }

        if (horizontalCurveActive)
        {
            ApplyContinuousCurve();
        }

        if (grounded)
        {
            ApplyRollingResistance();
        }
    }

    // =========================================================
    // DEFAULTS
    // =========================================================

    private void ApplyCurrentDefaults()
    {
        if (
            contactDefaultsVersion >=
            CurrentContactDefaultsVersion
        )
        {
            return;
        }

        // -----------------------------------------------------
        // FORWARD
        // -----------------------------------------------------

        minShotSpeed =
            6.20f;

        maxShotSpeed =
            28f;

        shotPowerExponent =
            0.72f;

        // -----------------------------------------------------
        // VERTICAL
        // -----------------------------------------------------

        baseVerticalLaunchSpeed =
            0.12f;

        maxUpwardFlickSpeed =
            6.50f;

        maxDownwardFlickSpeed =
            4.50f;

        verticalFlickResponseExponent =
            0.90f;

        minimumVerticalEffect =
            0.025f;

        // -----------------------------------------------------
        // AIRBORNE
        // -----------------------------------------------------

        airborneBlendStartHeight =
            0.22f;

        fullAirborneBlendHeight =
            0.85f;

        aerialIncomingVerticalCarryLowAuthority =
            0.92f;

        aerialIncomingVerticalCarryHighAuthority =
            0.58f;

        aerialFlickVelocityMultiplier =
            1.0f;

        maximumAerialVerticalSpeed =
            9.0f;

        // -----------------------------------------------------
        // MOMENTUM
        // -----------------------------------------------------

        incomingMomentumActivationSpeed =
            0.75f;

        minimumContactAuthority =
            0.18f;

        maximumContactAuthority =
            0.96f;

        contactAuthorityExponent =
            0.85f;

        verticalGestureAuthorityFloor =
            0.35f;

        // -----------------------------------------------------
        // FIRST TIME / SOFT
        // -----------------------------------------------------

        firstTimeIncomingSpeedStart =
            3.5f;

        firstTimeIncomingSpeedFull =
            14f;

        softContactPowerEnd =
            0.45f;

        oppositionStart =
            0.10f;

        oppositionFull =
            0.85f;

        minimumSoftFollowThrough =
            0.08f;

        maximumSoftFollowThrough =
            0.36f;

        softFollowThroughExponent =
            0.85f;

        softContactInfluenceExponent =
            0.85f;

        // -----------------------------------------------------
        // CURVE
        // -----------------------------------------------------

        maxCurveTurnRateDegrees =
            30f;

        curveReferenceSpeed =
            24f;

        minimumCurveSpeedFactor =
            0.55f;

        curveBuildTime =
            0.055f;

        curveDecayPerSecond =
            0.10f;

        maximumCurveDuration =
            2.5f;

        minimumCurveBallSpeed =
            0.25f;

        minimumHorizontalCurveEffect =
            0.035f;

        curveResponseExponent =
            0.78f;

        diagonalCurveBoost =
            1.10f;

        diagonalBoostExponent =
            1f;

        enableSideSpinTorque =
            true;

        maxSideSpinVelocity =
            20f;

        contactDefaultsVersion =
            CurrentContactDefaultsVersion;
    }

    // =========================================================
    // SHOT SPEED
    // =========================================================

    public float EvaluateShotSpeed(
        float power01
    )
    {
        float power =
            Mathf.Clamp01(
                power01
            );

        float shapedPower =
            Mathf.Pow(
                power,
                Mathf.Max(
                    shotPowerExponent,
                    0.01f
                )
            );

        return Mathf.Lerp(
            minShotSpeed,
            maxShotSpeed,
            shapedPower
        );
    }

    // =========================================================
    // PREVIEW DISTANCE
    // =========================================================

    public float EvaluateCurveDistance(
        float power01
    )
    {
        return Mathf.Lerp(
            minCurveDistance,
            maxCurveDistance,
            Mathf.Clamp01(
                power01
            )
        );
    }

    // =========================================================
    // VERTICAL FLICK
    // =========================================================

    public float EvaluateVerticalFlickVelocity(
        ShotIntent intent
    )
    {
        if (intent == null)
        {
            return 0f;
        }

        float effect =
            Mathf.Clamp01(
                intent.verticalEffect01
            );

        if (
            effect <
            minimumVerticalEffect ||
            Mathf.Abs(
                intent.verticalDirection
            ) <
            0.01f
        )
        {
            return 0f;
        }

        float shapedEffect =
            Mathf.Pow(
                effect,
                Mathf.Max(
                    verticalFlickResponseExponent,
                    0.01f
                )
            );

        if (
            intent.verticalDirection >
            0f
        )
        {
            return
                maxUpwardFlickSpeed *
                shapedEffect;
        }

        return
            -maxDownwardFlickSpeed *
            shapedEffect;
    }

    // =========================================================
    // GROUND VERTICAL
    // =========================================================

    public float EvaluateVerticalLaunchSpeed(
        ShotIntent intent
    )
    {
        float flickVelocity =
            EvaluateVerticalFlickVelocity(
                intent
            );

        if (
            Mathf.Abs(
                flickVelocity
            ) <
            0.0001f
        )
        {
            return baseVerticalLaunchSpeed;
        }

        return flickVelocity;
    }

    // =========================================================
    // BASE CONTACT AUTHORITY
    // =========================================================

    public float EvaluateContactAuthority(
        float power01
    )
    {
        float power =
            Mathf.Pow(
                Mathf.Clamp01(
                    power01
                ),
                Mathf.Max(
                    contactAuthorityExponent,
                    0.01f
                )
            );

        return Mathf.Lerp(
            minimumContactAuthority,
            maximumContactAuthority,
            power
        );
    }

    // =========================================================
    // VERTICAL AUTHORITY
    // =========================================================

    private float EvaluateVerticalContactAuthority(
        ShotIntent intent,
        float normalAuthority
    )
    {
        if (
            intent == null ||
            Mathf.Abs(
                intent.verticalDirection
            ) <
            0.01f ||
            intent.verticalEffect01 <=
            minimumVerticalEffect
        )
        {
            return normalAuthority;
        }

        float gestureAuthority =
            Mathf.Lerp(
                verticalGestureAuthorityFloor,
                1f,
                Mathf.Clamp01(
                    intent.verticalEffect01
                )
            );

        return Mathf.Max(
            normalAuthority,
            gestureAuthority
        );
    }

    // =========================================================
    // DESIRED CONTACT VELOCITY
    // =========================================================

    public Vector3 EvaluateDesiredContactVelocity(
        ShotIntent intent
    )
    {
        if (intent == null)
        {
            return Vector3.zero;
        }

        Vector3 forward =
            intent.aimForward;

        forward.y =
            0f;

        if (
            forward.sqrMagnitude <
            0.0001f
        )
        {
            return Vector3.zero;
        }

        forward.Normalize();

        float forwardSpeed =
            EvaluateShotSpeed(
                intent.power01
            );

        float verticalSpeed =
            EvaluateVerticalLaunchSpeed(
                intent
            );

        return
            forward *
            forwardSpeed +
            Vector3.up *
            verticalSpeed;
    }

    // =========================================================
    // FIRST-TIME PLANAR AUTHORITY
    // =========================================================

    private float EvaluateEffectivePlanarAuthority(
        ShotIntent intent,
        Vector3 incomingPlanar,
        Vector3 desiredPlanar,
        out float opposition01,
        out float softInfluence01,
        out float neutralizationAuthority
    )
    {
        opposition01 =
            0f;

        softInfluence01 =
            0f;

        neutralizationAuthority =
            0f;

        if (intent == null)
        {
            return 0f;
        }

        float baseAuthority =
            EvaluateContactAuthority(
                intent.power01
            );

        float incomingSpeed =
            incomingPlanar.magnitude;

        float desiredSpeed =
            desiredPlanar.magnitude;

        if (
            incomingSpeed <
            0.001f ||
            desiredSpeed <
            0.001f
        )
        {
            return baseAuthority;
        }

        Vector3 incomingDirection =
            incomingPlanar /
            incomingSpeed;

        Vector3 desiredDirection =
            desiredPlanar /
            desiredSpeed;

        float directionDot =
            Mathf.Clamp(
                Vector3.Dot(
                    incomingDirection,
                    desiredDirection
                ),
                -1f,
                1f
            );

        // -----------------------------------------------------
        // OPPOSITION
        // -----------------------------------------------------

        float rawOpposition =
            Mathf.Max(
                0f,
                -directionDot
            );

        opposition01 =
            Mathf.InverseLerp(
                oppositionStart,
                Mathf.Max(
                    oppositionFull,
                    oppositionStart +
                    0.01f
                ),
                rawOpposition
            );

        opposition01 =
            Mathf.SmoothStep(
                0f,
                1f,
                opposition01
            );

        // -----------------------------------------------------
        // INCOMING SPEED INFLUENCE
        // -----------------------------------------------------

        float incomingSpeed01 =
            Mathf.InverseLerp(
                firstTimeIncomingSpeedStart,
                Mathf.Max(
                    firstTimeIncomingSpeedFull,
                    firstTimeIncomingSpeedStart +
                    0.01f
                ),
                incomingSpeed
            );

        incomingSpeed01 =
            Mathf.SmoothStep(
                0f,
                1f,
                incomingSpeed01
            );

        // -----------------------------------------------------
        // LOW POWER INFLUENCE
        // -----------------------------------------------------

        float softPower01 =
            Mathf.InverseLerp(
                0f,
                Mathf.Max(
                    softContactPowerEnd,
                    0.01f
                ),
                intent.power01
            );

        softPower01 =
            Mathf.SmoothStep(
                0f,
                1f,
                softPower01
            );

        float lowPowerInfluence =
            1f -
            softPower01;

        // -----------------------------------------------------
        // TOTAL SOFT CONTACT INFLUENCE
        // -----------------------------------------------------

        float combinedSoftInfluence =
            opposition01 *
            incomingSpeed01 *
            lowPowerInfluence;

        softInfluence01 =
            Mathf.Pow(
                Mathf.Clamp01(
                    combinedSoftInfluence
                ),
                Mathf.Max(
                    softContactInfluenceExponent,
                    0.01f
                )
            );

        if (
            softInfluence01 <=
            0.0001f
        )
        {
            return baseAuthority;
        }

        // -----------------------------------------------------
        // OPPOSING COMPONENT
        // -----------------------------------------------------

        float incomingAlongDesired =
            Vector3.Dot(
                incomingPlanar,
                desiredDirection
            );

        float opposingSpeed =
            Mathf.Max(
                0f,
                -incomingAlongDesired
            );

        if (
            opposingSpeed <=
            0.0001f
        )
        {
            return baseAuthority;
        }

        /*
         * Exact authority that would cancel the incoming
         * velocity component opposing the desired direction.
         *
         * For a perfectly head-on incoming ball:
         *
         * incoming = -15 m/s
         * desired  = +10 m/s
         *
         * the correct cancellation point is NOT arbitrary.
         * It is derived directly from both velocities.
         */
        neutralizationAuthority =
            opposingSpeed /
            Mathf.Max(
                opposingSpeed +
                desiredSpeed,
                0.001f
            );

        // -----------------------------------------------------
        // SMALL FOLLOW-THROUGH
        // -----------------------------------------------------

        float powerInsideSoftRange =
            Mathf.Clamp01(
                intent.power01 /
                Mathf.Max(
                    softContactPowerEnd,
                    0.01f
                )
            );

        float shapedFollowPower =
            Mathf.Pow(
                powerInsideSoftRange,
                Mathf.Max(
                    softFollowThroughExponent,
                    0.01f
                )
            );

        float followThrough =
            Mathf.Lerp(
                minimumSoftFollowThrough,
                maximumSoftFollowThrough,
                shapedFollowPower
            );

        /*
         * Move slightly beyond exact cancellation.
         *
         * This means a soft touch tends to leave the ball
         * gently on the intended side rather than creating
         * a mathematically frozen ball every time.
         */
        float softTargetAuthority =
            neutralizationAuthority +
            (
                1f -
                neutralizationAuthority
            ) *
            followThrough;

        /*
         * Never make a strong player's contact weaker than
         * the normal authority it already had.
         */
        softTargetAuthority =
            Mathf.Max(
                softTargetAuthority,
                baseAuthority
            );

        return Mathf.Lerp(
            baseAuthority,
            softTargetAuthority,
            softInfluence01
        );
    }

    // =========================================================
    // GROUND HEIGHT REFERENCE
    // =========================================================

    private void InitializeGroundHeightReference()
    {
        if (rb == null)
        {
            return;
        }

        if (IsGrounded())
        {
            lastGroundedBallCenterY =
                rb.position.y;

            hasGroundHeightReference =
                true;
        }
    }

    private void UpdateGroundHeightReference()
    {
        if (rb == null)
        {
            return;
        }

        lastGroundedBallCenterY =
            rb.position.y;

        hasGroundHeightReference =
            true;
    }

    private float GetWorldBallRadius()
    {
        if (sphereCollider == null)
        {
            return 0.11f;
        }

        float scale =
            Mathf.Max(
                Mathf.Abs(
                    transform.lossyScale.x
                ),
                Mathf.Abs(
                    transform.lossyScale.z
                )
            );

        return
            sphereCollider.radius *
            scale;
    }

    public float EvaluateCurrentBallHeightAboveGround()
    {
        float radius =
            GetWorldBallRadius();

        if (
            rb == null ||
            !hasGroundHeightReference
        )
        {
            return radius;
        }

        float additionalHeight =
            Mathf.Max(
                0f,
                rb.position.y -
                lastGroundedBallCenterY
            );

        return
            radius +
            additionalHeight;
    }

    public float EvaluateAirborneBlend(
        float ballHeight
    )
    {
        float start =
            Mathf.Max(
                airborneBlendStartHeight,
                0f
            );

        float full =
            Mathf.Max(
                fullAirborneBlendHeight,
                start +
                0.01f
            );

        float t =
            Mathf.InverseLerp(
                start,
                full,
                ballHeight
            );

        return Mathf.SmoothStep(
            0f,
            1f,
            Mathf.Clamp01(
                t
            )
        );
    }

    // =========================================================
    // UNIVERSAL CONTACT VELOCITY
    // =========================================================

    public Vector3 EvaluateContactVelocity(
        ShotIntent intent,
        Vector3 incomingVelocity
    )
    {
        if (intent == null)
        {
            return incomingVelocity;
        }

        Vector3 desiredVelocity =
            EvaluateDesiredContactVelocity(
                intent
            );

        float baseAuthority =
            EvaluateContactAuthority(
                intent.power01
            );

        float verticalAuthority =
            EvaluateVerticalContactAuthority(
                intent,
                baseAuthority
            );

        float ballHeight =
            EvaluateCurrentBallHeightAboveGround();

        float airborne01 =
            EvaluateAirborneBlend(
                ballHeight
            );

        // =====================================================
        // PLANAR
        // =====================================================

        Vector3 incomingPlanar =
            new Vector3(
                incomingVelocity.x,
                0f,
                incomingVelocity.z
            );

        Vector3 desiredPlanar =
            new Vector3(
                desiredVelocity.x,
                0f,
                desiredVelocity.z
            );

        bool meaningfulIncomingMomentum =
            incomingPlanar.magnitude >=
            incomingMomentumActivationSpeed;

        Vector3 resultPlanar;

        if (meaningfulIncomingMomentum)
        {
            float opposition;
            float softInfluence;
            float neutralization;

            float effectiveAuthority =
                EvaluateEffectivePlanarAuthority(
                    intent,
                    incomingPlanar,
                    desiredPlanar,
                    out opposition,
                    out softInfluence,
                    out neutralization
                );

            resultPlanar =
                Vector3.Lerp(
                    incomingPlanar,
                    desiredPlanar,
                    effectiveAuthority
                );
        }
        else
        {
            resultPlanar =
                desiredPlanar;
        }

        // =====================================================
        // GROUND VERTICAL
        // =====================================================

        float groundVerticalResult;

        if (
            incomingVelocity.magnitude >=
            incomingMomentumActivationSpeed
        )
        {
            groundVerticalResult =
                Mathf.Lerp(
                    incomingVelocity.y,
                    desiredVelocity.y,
                    verticalAuthority
                );
        }
        else
        {
            groundVerticalResult =
                desiredVelocity.y;
        }

        // =====================================================
        // AIRBORNE VERTICAL
        // =====================================================

        float incomingVerticalCarry =
            Mathf.Lerp(
                aerialIncomingVerticalCarryLowAuthority,
                aerialIncomingVerticalCarryHighAuthority,
                baseAuthority
            );

        float carriedIncomingVertical =
            incomingVelocity.y *
            incomingVerticalCarry;

        float flickVerticalVelocity =
            EvaluateVerticalFlickVelocity(
                intent
            ) *
            aerialFlickVelocityMultiplier;

        float airborneVerticalResult =
            carriedIncomingVertical +
            flickVerticalVelocity;

        airborneVerticalResult =
            Mathf.Clamp(
                airborneVerticalResult,
                -Mathf.Abs(
                    maximumAerialVerticalSpeed
                ),
                Mathf.Abs(
                    maximumAerialVerticalSpeed
                )
            );

        float resultVertical =
            Mathf.Lerp(
                groundVerticalResult,
                airborneVerticalResult,
                airborne01
            );

        return new Vector3(
            resultPlanar.x,
            resultVertical,
            resultPlanar.z
        );
    }

    // =========================================================
    // DIAGONAL CURVE
    // =========================================================

    public float EvaluateDiagonalCurveMultiplier(
        ShotIntent intent
    )
    {
        if (
            intent == null ||
            intent.verticalDirection <= 0f ||
            intent.verticalEffect01 <= 0.001f
        )
        {
            return 1f;
        }

        float verticalContribution =
            Mathf.Pow(
                Mathf.Clamp01(
                    intent.verticalEffect01
                ),
                Mathf.Max(
                    diagonalBoostExponent,
                    0.01f
                )
            );

        return Mathf.Lerp(
            1f,
            diagonalCurveBoost,
            verticalContribution
        );
    }

    // =========================================================
    // CURVE DIRECTION
    // =========================================================

    public float EvaluateHorizontalCurveDirection(
        ShotIntent intent
    )
    {
        if (
            intent == null ||
            intent.profile == null ||
            intent.profile.Count == 0
        )
        {
            return 0f;
        }

        float finalX =
            intent.profile[
                intent.profile.Count - 1
            ].position.x;

        if (
            Mathf.Abs(
                finalX
            ) <
            0.001f
        )
        {
            return 0f;
        }

        return Mathf.Sign(
            finalX
        );
    }

    // =========================================================
    // CURVE RATE
    // =========================================================

    public float EvaluateCurveTurnRate(
        ShotIntent intent,
        float horizontalSpeed,
        float elapsedTime
    )
    {
        if (intent == null)
        {
            return 0f;
        }

        float rawEffect =
            Mathf.Clamp01(
                intent.horizontalEffect01
            );

        if (
            !intent.hasFlick ||
            rawEffect <
            minimumHorizontalCurveEffect
        )
        {
            return 0f;
        }

        float effect =
            Mathf.Pow(
                rawEffect,
                Mathf.Max(
                    curveResponseExponent,
                    0.01f
                )
            );

        float speed01 =
            Mathf.Clamp01(
                horizontalSpeed /
                Mathf.Max(
                    curveReferenceSpeed,
                    0.01f
                )
            );

        float speedFactor =
            Mathf.Lerp(
                minimumCurveSpeedFactor,
                1f,
                speed01
            );

        float buildFactor =
            curveBuildTime <=
            0.0001f
                ? 1f
                : Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        elapsedTime /
                        curveBuildTime
                    )
                );

        float decay =
            Mathf.Exp(
                -Mathf.Max(
                    curveDecayPerSecond,
                    0f
                ) *
                Mathf.Max(
                    elapsedTime,
                    0f
                )
            );

        float diagonalMultiplier =
            EvaluateDiagonalCurveMultiplier(
                intent
            );

        return
            maxCurveTurnRateDegrees *
            effect *
            speedFactor *
            buildFactor *
            decay *
            diagonalMultiplier;
    }

    // =========================================================
    // COMPATIBILITY
    // =========================================================

    public float EvaluateCurveAcceleration(
        ShotIntent intent,
        float horizontalSpeed,
        float elapsedTime
    )
    {
        float turnRateDegrees =
            EvaluateCurveTurnRate(
                intent,
                horizontalSpeed,
                elapsedTime
            );

        float turnRateRadians =
            turnRateDegrees *
            Mathf.Deg2Rad;

        return
            horizontalSpeed *
            turnRateRadians;
    }

    // =========================================================
    // RMB
    // =========================================================

    public void DribbleTouch(
        Vector3 direction,
        float charge,
        Vector3 playerVelocity
    )
    {
        StopShotAssistance();

        direction.y =
            0f;

        if (
            direction.sqrMagnitude <
            0.001f
        )
        {
            return;
        }

        direction.Normalize();

        charge =
            Mathf.Clamp01(
                charge
            );

        float curvedCharge =
            Mathf.Pow(
                charge,
                touchSpeedExponent
            );

        float relativeTouchSpeed =
            Mathf.Lerp(
                minTouchSpeed,
                maxTouchSpeed,
                curvedCharge
            );

        Vector3 playerPlanarVelocity =
            new Vector3(
                playerVelocity.x,
                0f,
                playerVelocity.z
            );

        if (
            playerPlanarVelocity.magnitude >
            maxPlayerVelocityCarry
        )
        {
            playerPlanarVelocity =
                playerPlanarVelocity.normalized *
                maxPlayerVelocityCarry;
        }

        playerPlanarVelocity *=
            playerVelocityCarry;

        Vector3 desiredVelocity =
            direction *
            relativeTouchSpeed +
            playerPlanarVelocity;

        Vector3 currentPlanarVelocity =
            new Vector3(
                rb.velocity.x,
                0f,
                rb.velocity.z
            );

        float steering =
            Mathf.Lerp(
                minSteeringAuthority,
                maxSteeringAuthority,
                curvedCharge
            );

        float reverseCancel =
            Mathf.Lerp(
                minReverseMomentumCancel,
                maxReverseMomentumCancel,
                curvedCharge
            );

        float movementDot =
            Vector3.Dot(
                currentPlanarVelocity,
                direction
            );

        float effectiveSteering =
            steering;

        if (movementDot < 0f)
        {
            effectiveSteering =
                Mathf.Max(
                    effectiveSteering,
                    reverseCancel
                );
        }

        Vector3 targetPlanarVelocity =
            Vector3.Lerp(
                currentPlanarVelocity,
                desiredVelocity,
                effectiveSteering
            );

        Vector3 deltaVelocity =
            targetPlanarVelocity -
            currentPlanarVelocity;

        rb.AddForce(
            deltaVelocity *
            rb.mass,
            ForceMode.Impulse
        );
    }

    // =========================================================
    // EXECUTE LMB
    // =========================================================

    public void ExecuteShotIntent(
        ShotIntent intent
    )
    {
        if (
            intent == null ||
            rb == null
        )
        {
            return;
        }

        StopShotAssistance();

        Vector3 incomingVelocity =
            rb.velocity;

        Vector3 desiredVelocity =
            EvaluateDesiredContactVelocity(
                intent
            );

        Vector3 incomingPlanar =
            new Vector3(
                incomingVelocity.x,
                0f,
                incomingVelocity.z
            );

        Vector3 desiredPlanar =
            new Vector3(
                desiredVelocity.x,
                0f,
                desiredVelocity.z
            );

        float opposition01;
        float softInfluence01;
        float neutralizationAuthority;

        float planarAuthority =
            EvaluateEffectivePlanarAuthority(
                intent,
                incomingPlanar,
                desiredPlanar,
                out opposition01,
                out softInfluence01,
                out neutralizationAuthority
            );

        float baseAuthority =
            EvaluateContactAuthority(
                intent.power01
            );

        float verticalAuthority =
            EvaluateVerticalContactAuthority(
                intent,
                baseAuthority
            );

        float ballHeight =
            EvaluateCurrentBallHeightAboveGround();

        float airborne01 =
            EvaluateAirborneBlend(
                ballHeight
            );

        Vector3 resultVelocity =
            EvaluateContactVelocity(
                intent,
                incomingVelocity
            );

        // -----------------------------------------------------
        // DEBUG SNAPSHOT
        // -----------------------------------------------------

        LastExecutedIntent =
            intent;

        LastIncomingContactVelocity =
            incomingVelocity;

        LastDesiredContactVelocity =
            desiredVelocity;

        LastContactResultVelocity =
            resultVelocity;

        LastContactAuthority =
            planarAuthority;

        LastBaseContactAuthority =
            baseAuthority;

        LastPlanarContactAuthority =
            planarAuthority;

        LastVerticalContactAuthority =
            verticalAuthority;

        LastContactBallHeight =
            ballHeight;

        LastAirborneBlend =
            airborne01;

        LastOpposition01 =
            opposition01;

        LastSoftContactInfluence01 =
            softInfluence01;

        LastNeutralizationAuthority =
            neutralizationAuthority;

        // -----------------------------------------------------
        // APPLY VELOCITY
        // -----------------------------------------------------

        rb.velocity =
            resultVelocity;

        // -----------------------------------------------------
        // CURVE
        // -----------------------------------------------------

        float curveDirection =
            EvaluateHorizontalCurveDirection(
                intent
            );

        bool validCurve =
            intent.hasFlick &&
            intent.horizontalEffect01 >=
            minimumHorizontalCurveEffect &&
            Mathf.Abs(
                curveDirection
            ) >
            0.01f;

        if (validCurve)
        {
            activeShotIntent =
                intent;

            activeCurveDirection =
                curveDirection;

            activeCurveStartTime =
                Time.time;

            horizontalCurveActive =
                true;

            loggedFirstCurveStep =
                false;
        }

        if (
            enableSideSpinTorque &&
            validCurve
        )
        {
            rb.AddTorque(
                Vector3.up *
                curveDirection *
                maxSideSpinVelocity *
                intent.horizontalEffect01,
                ForceMode.VelocityChange
            );
        }

        // -----------------------------------------------------
        // LOG
        // -----------------------------------------------------

        Debug.Log(
            "[BALL CONTACT V14 FIRST TOUCH] " +

            "Power: " +
            intent.power01.ToString("F3") +

            " | Incoming: " +
            incomingVelocity.ToString("F2") +

            " | Desired: " +
            desiredVelocity.ToString("F2") +

            " | Result: " +
            resultVelocity.ToString("F2") +

            " | BaseAuth: " +
            baseAuthority.ToString("F3") +

            " | PlanarAuth: " +
            planarAuthority.ToString("F3") +

            " | Opposition: " +
            opposition01.ToString("F3") +

            " | Soft: " +
            softInfluence01.ToString("F3") +

            " | NeutralAuth: " +
            neutralizationAuthority.ToString("F3") +

            " | Air: " +
            airborne01.ToString("F3") +

            " | H: " +
            intent.horizontalEffect01.ToString("F3") +

            " | V: " +
            intent.verticalEffect01.ToString("F3")
        );
    }

    // =========================================================
    // CONTINUOUS CURVE
    // =========================================================

    private void ApplyContinuousCurve()
    {
        if (
            activeShotIntent == null ||
            rb == null
        )
        {
            StopShotAssistance();

            return;
        }

        float elapsed =
            Time.time -
            activeCurveStartTime;

        if (
            elapsed >
            maximumCurveDuration
        )
        {
            StopShotAssistance();

            return;
        }

        Vector3 velocity =
            rb.velocity;

        Vector3 planarVelocity =
            new Vector3(
                velocity.x,
                0f,
                velocity.z
            );

        float planarSpeed =
            planarVelocity.magnitude;

        if (
            planarSpeed <
            minimumCurveBallSpeed
        )
        {
            StopShotAssistance();

            return;
        }

        float turnRate =
            EvaluateCurveTurnRate(
                activeShotIntent,
                planarSpeed,
                elapsed
            );

        if (
            turnRate <=
            0.0001f
        )
        {
            return;
        }

        float turnThisStep =
            turnRate *
            activeCurveDirection *
            Time.fixedDeltaTime;

        Vector3 curvedPlanarVelocity =
            Quaternion.AngleAxis(
                turnThisStep,
                Vector3.up
            ) *
            planarVelocity;

        if (
            curvedPlanarVelocity.sqrMagnitude >
            0.000001f
        )
        {
            curvedPlanarVelocity =
                curvedPlanarVelocity.normalized *
                planarSpeed;
        }

        rb.velocity =
            new Vector3(
                curvedPlanarVelocity.x,
                velocity.y,
                curvedPlanarVelocity.z
            );

        if (!loggedFirstCurveStep)
        {
            loggedFirstCurveStep =
                true;

            Debug.Log(
                "[CURVE FIRST STEP] " +

                "H: " +
                activeShotIntent
                    .horizontalEffect01
                    .ToString("F3") +

                " | Side: " +
                activeCurveDirection
                    .ToString("F0") +

                " | Turn rate: " +
                turnRate.ToString("F2") +
                " deg/s"
            );
        }
    }

    // =========================================================
    // STOP
    // =========================================================

    public void StopShotAssistance()
    {
        horizontalCurveActive =
            false;

        activeShotIntent =
            null;

        activeCurveDirection =
            0f;

        loggedFirstCurveStep =
            false;
    }

    // =========================================================
    // ROLLING
    // =========================================================

    private void ApplyRollingResistance()
    {
        Vector3 velocity =
            rb.velocity;

        Vector3 horizontalVelocity =
            new Vector3(
                velocity.x,
                0f,
                velocity.z
            );

        float speed =
            horizontalVelocity.magnitude;

        if (
            speed <=
            stopSpeed
        )
        {
            rb.velocity =
                new Vector3(
                    0f,
                    velocity.y,
                    0f
                );

            return;
        }

        float deceleration =
            groundDeceleration +
            speed *
            speedDecelerationFactor;

        Vector3 newHorizontalVelocity =
            Vector3.MoveTowards(
                horizontalVelocity,
                Vector3.zero,
                deceleration *
                Time.fixedDeltaTime
            );

        rb.velocity =
            new Vector3(
                newHorizontalVelocity.x,
                velocity.y,
                newHorizontalVelocity.z
            );
    }

    // =========================================================
    // GROUND
    // =========================================================

    private bool IsGrounded()
    {
        if (
            sphereCollider == null ||
            rb == null
        )
        {
            return false;
        }

        if (
            Mathf.Abs(
                rb.velocity.y
            ) >
            maxGroundVerticalSpeed
        )
        {
            return false;
        }

        float radius =
            sphereCollider.radius *
            Mathf.Max(
                transform.lossyScale.x,
                transform.lossyScale.z
            );

        Vector3 origin =
            transform.position +
            Vector3.up *
            0.01f;

        float distance =
            radius +
            groundCheckExtra;

        return Physics.Raycast(
            origin,
            Vector3.down,
            distance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }
}