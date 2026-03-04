using UnityEngine;

/// <summary>
/// ScriptableObject containing all tunable settings for the Car Character Controller.
/// Create one per character type: right-click in Project > Character > Car Character Settings
/// Designers can tweak every number, curve, and flag without touching code.
/// </summary>
[CreateAssetMenu(fileName = "NewCarCharacterSettings", menuName = "Character/Car Character Settings")]
public class CarCharacterSettings : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────────────────
    //  GROUND MOVEMENT
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Ground Movement")]

    [Tooltip("Top forward speed in m/s (e.g. 20 = fast car feel, 10 = heavy tank feel)")]
    public float MaxForwardSpeed = 20f;

    [Tooltip("Top reverse speed in m/s")]
    public float MaxReverseSpeed = 8f;

    [Tooltip(
        "Controls how quickly the character accelerates as speed increases.\n" +
        "X axis = current speed / max speed (0 = stopped, 1 = full speed)\n" +
        "Y axis = acceleration multiplier (1 = full power, 0 = no power)\n" +
        "Tip: Steep drop at high X = hard to go faster once near top speed (realistic).\n" +
        "     Flat line = constant acceleration all the way (arcade feel).")]
    public AnimationCurve AccelerationCurve;

    [Tooltip("Flat acceleration sharpness — higher = snappier response to input")]
    [Range(1f, 50f)]
    public float AccelerationSharpness = 15f;

    [Tooltip(
        "Controls braking strength at different speed ratios.\n" +
        "X = current speed ratio, Y = braking force multiplier.\n" +
        "Tip: High Y at high X = strong braking at speed (ABS feel).")]
    public AnimationCurve DecelerationCurve;

    [Tooltip("Flat deceleration force in m/s² applied when no input")]
    [Range(1f, 80f)]
    public float DecelerationForce = 25f;

    [Tooltip(
        "Steering responsiveness at different speed ratios.\n" +
        "X = speed ratio, Y = turn speed multiplier.\n" +
        "Tip: High Y at low X (easy to turn while slow) and low Y at high X (harder to turn at speed).\n" +
        "     Flat line = same turning at any speed (arcade).")]
    public AnimationCurve TurnSpeedCurve;

    [Tooltip("Base turning speed in degrees per second")]
    [Range(60f, 720f)]
    public float BaseTurnSpeed = 240f;

    // ─────────────────────────────────────────────────────────────────────────
    //  JUMPING
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Jumping")]

    [Tooltip("Upward launch speed when jumping in m/s")]
    [Range(1f, 30f)]
    public float JumpUpSpeed = 12f;

    [Tooltip("Extra forward momentum added on jump (preserves car feel in the air)")]
    [Range(0f, 20f)]
    public float JumpForwardBoost = 2f;

    [Tooltip(
        "Coyote time: seconds after walking off a ledge where you can still jump.\n" +
        "0.1–0.15 is a good game-feel range.")]
    [Range(0f, 0.5f)]
    public float CoyoteTime = 0.12f;

    [Tooltip(
        "Jump buffer time: if jump is pressed this many seconds before landing, " +
        "it will auto-execute on touchdown. Makes controls feel more responsive.")]
    [Range(0f, 0.3f)]
    public float JumpBufferTime = 0.12f;

    // ─────────────────────────────────────────────────────────────────────────
    //  AIR MOVEMENT
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Air Movement")]

    [Tooltip("How strongly the player can steer in mid-air")]
    [Range(0f, 30f)]
    public float AirAcceleration = 8f;

    [Tooltip("Max horizontal speed while airborne (prevents infinite air acceleration)")]
    [Range(1f, 40f)]
    public float MaxAirSpeed = 18f;

    [Tooltip("Air drag coefficient — higher = more resistance, shorter hang time")]
    [Range(0f, 1f)]
    public float AirDrag = 0.08f;

    [Tooltip("Gravity vector. Increase Y magnitude for heavier feel, reduce for floaty.")]
    public Vector3 Gravity = new Vector3(0f, -30f, 0f);

    // ─────────────────────────────────────────────────────────────────────────
    //  WALL RUNNING
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Wall Running")]

    [Tooltip("How far from the capsule surface to scan for walls (slightly larger than capsule radius)")]
    [Range(0.1f, 2f)]
    public float WallDetectionDistance = 0.75f;

    [Tooltip("Minimum speed the character must be travelling to trigger a wall run")]
    [Range(1f, 20f)]
    public float WallRunMinEntrySpeed = 6f;

    [Tooltip("Maximum total time a single wall run can last")]
    [Range(0.5f, 10f)]
    public float WallRunMaxDuration = 2.5f;

    [Tooltip("Target horizontal speed while wall running")]
    [Range(1f, 40f)]
    public float WallRunSpeed = 16f;

    [Tooltip(
        "Gravity multiplier over wall run duration.\n" +
        "X = normalised time (0 = start, 1 = end of WallRunMaxDuration)\n" +
        "Y = gravity scale (0 = no gravity, 1 = full gravity)\n" +
        "Tip: Start at 0 and ramp up to let the player float at first then fall.")]
    public AnimationCurve WallRunGravityCurve;

    [Tooltip(
        "Wall run speed multiplier over duration.\n" +
        "X = normalised time, Y = speed multiplier.\n" +
        "Tip: Slight decay (1 → 0.7) gives a natural 'slowing down' feel.")]
    public AnimationCurve WallRunSpeedCurve;

    [Tooltip("Upward speed when jumping off a wall")]
    [Range(1f, 25f)]
    public float WallJumpUpSpeed = 10f;

    [Tooltip("Away-from-wall speed when jumping off a wall")]
    [Range(1f, 25f)]
    public float WallJumpAwaySpeed = 9f;

    [Tooltip("Seconds before the same wall can be run on again after leaving it")]
    [Range(0f, 2f)]
    public float WallRunCooldown = 0.4f;

    [Tooltip(
        "Minimum angle between the wall's normal and the character's up direction " +
        "for the wall to be considered runnable. ~70° means near-vertical walls only.")]
    [Range(45f, 89f)]
    public float MinWallAngle = 70f;

    // ─────────────────────────────────────────────────────────────────────────
    //  SLIDING
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Sliding")]

    [Tooltip("Minimum ground speed required to enter a slide")]
    [Range(1f, 20f)]
    public float MinSlideEntrySpeed = 7f;

    [Tooltip("Instant speed boost applied when entering a slide")]
    [Range(0f, 15f)]
    public float SlideEntryBoost = 5f;

    [Tooltip("Maximum duration of a slide before it forces the player to stand")]
    [Range(0.5f, 6f)]
    public float MaxSlideDuration = 2f;

    [Tooltip(
        "Friction force applied over the slide duration.\n" +
        "X = normalised slide time (0 = start, 1 = end of MaxSlideDuration)\n" +
        "Y = friction force in m/s²\n" +
        "Tip: Low friction early (fast entry) then high friction later (natural slowdown).")]
    public AnimationCurve SlideFrictionCurve;

    [Tooltip("Capsule height while sliding (half of normal height is a good start)")]
    [Range(0.4f, 1.5f)]
    public float SlideCapsuleHeight = 0.8f;

    [Tooltip("Extra downhill pull while sliding on slopes (makes slopes feel faster)")]
    [Range(0f, 5f)]
    public float SlopeSlideBoost = 2f;

    [Tooltip("Allow the player to jump while sliding")]
    public bool AllowJumpFromSlide = true;

    // ─────────────────────────────────────────────────────────────────────────
    //  ORIENTATION
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Orientation")]

    [Tooltip("How quickly the character body rotates to face the movement direction")]
    [Range(1f, 50f)]
    public float RotationSharpness = 15f;

    [Tooltip("If true, the character tilts to match the slope of the ground it stands on")]
    public bool OrientToSlopes = false;

    [Tooltip("How quickly the character tilts to match slopes (only relevant if OrientToSlopes is true)")]
    [Range(1f, 30f)]
    public float SlopeOrientSharpness = 8f;

    // ─────────────────────────────────────────────────────────────────────────
    //  DEFAULT CURVE VALUES  (called when the asset is first created)
    // ─────────────────────────────────────────────────────────────────────────
    private void Reset()
    {
        // Acceleration drops off as you approach top speed
        AccelerationCurve = new AnimationCurve(
            new Keyframe(0.0f, 1.0f,  0f,   -1.2f),
            new Keyframe(0.6f, 0.55f, -0.8f, -0.5f),
            new Keyframe(1.0f, 0.05f, -0.4f,  0f));

        // Braking is soft at low speed, hard at high speed
        DecelerationCurve = new AnimationCurve(
            new Keyframe(0.0f, 0.2f, 0f, 1.2f),
            new Keyframe(1.0f, 1.0f, 0.5f, 0f));

        // Easier to turn when slow, harder at full speed
        TurnSpeedCurve = new AnimationCurve(
            new Keyframe(0.0f, 1.6f, 0f,  -0.8f),
            new Keyframe(0.4f, 1.0f, -0.5f, -0.3f),
            new Keyframe(1.0f, 0.45f, -0.3f, 0f));

        // Wall run: no gravity at first, then it kicks in near the end
        WallRunGravityCurve = new AnimationCurve(
            new Keyframe(0.0f, 0.0f, 0f, 0.1f),
            new Keyframe(0.6f, 0.15f, 0.3f, 0.8f),
            new Keyframe(1.0f, 0.85f, 1.2f, 0f));

        // Wall run speed: stays strong then tapers
        WallRunSpeedCurve = new AnimationCurve(
            new Keyframe(0.0f, 1.0f, 0f,  -0.1f),
            new Keyframe(0.75f, 0.9f, -0.2f, -0.6f),
            new Keyframe(1.0f,  0.6f, -0.7f, 0f));

        // Slide friction: starts very low, builds up quickly near the end
        SlideFrictionCurve = new AnimationCurve(
            new Keyframe(0.0f, 0.5f, 0f, 2f),
            new Keyframe(0.4f, 4.0f, 6f, 8f),
            new Keyframe(1.0f, 22f,  12f, 0f));
    }
}
