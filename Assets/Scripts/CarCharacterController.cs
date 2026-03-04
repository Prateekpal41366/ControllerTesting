using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;

// ─────────────────────────────────────────────────────────────────────────────
//  ENUMS & DATA STRUCTS
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>All possible movement states for the car character.</summary>
public enum CarCharacterState
{
    Default,      // Grounded movement + airborne (handled via GroundingStatus)
    Sliding,      // Low crouch slide
    WallRunning   // Running along a vertical wall
}

/// <summary>
/// Input snapshot sent each frame from CarPlayer to CarCharacterController.
/// Using a struct keeps the interface clean and garbage-free.
/// </summary>
public struct CarCharacterInputs
{
    public float    MoveAxisForward;   // W/S  : -1 to 1
    public float    MoveAxisRight;     // A/D  : -1 to 1
    public Quaternion CameraRotation;  // full camera orientation for relative movement
    public bool     JumpDown;          // pressed this frame
    public bool     JumpHeld;          // held (for future variable-height jump)
    public bool     SlideDown;         // pressed this frame
    public bool     SlideUp;           // released this frame
}

// ─────────────────────────────────────────────────────────────────────────────
//  MAIN CONTROLLER
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Car-like character controller built on top of KinematicCharacterMotor.
/// Features: car-feel ground movement · jump with coyote time & buffer ·
///           wall running with gravity/speed curves · crouch slide.
///
/// HOW TO SET UP:
///   1. Add KinematicCharacterMotor to your character GameObject.
///   2. Add this component — it will auto-link to the motor.
///   3. Assign a CarCharacterSettings ScriptableObject.
///   4. Assign MeshRoot (child holding all visual meshes) and CameraFollowPoint.
/// </summary>
[RequireComponent(typeof(KinematicCharacterMotor))]
public class CarCharacterController : MonoBehaviour, ICharacterController
{
    // ── Inspector References ─────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("Auto-fetched on Awake, but can be overridden.")]
    public KinematicCharacterMotor Motor;

    [Tooltip("ScriptableObject with all tunable settings. Swap per character type.")]
    public CarCharacterSettings Settings;

    [Header("Visuals")]
    [Tooltip("Transform that holds all character meshes. Scaled for crouch/slide.")]
    public Transform MeshRoot;

    [Tooltip("Empty child transform the camera orbits around.")]
    public Transform CameraFollowPoint;

    // ── Public Read-only State ───────────────────────────────────────────────
    public CarCharacterState CurrentState  { get; private set; }
    public bool              IsGrounded    => Motor.GroundingStatus.IsStableOnGround;
    public bool              IsWallRunning => CurrentState == CarCharacterState.WallRunning;
    public bool              IsSliding     => CurrentState == CarCharacterState.Sliding;
    public Vector3           WallNormal    => _wallNormal;

    // ── Input Storage ────────────────────────────────────────────────────────
    private Vector3   _moveInputVector;      // camera-relative horizontal movement direction
    private Vector3   _lookInputVector;      // direction the character should face
    private bool      _jumpRequested;
    private bool      _jumpHeld;
    private bool      _jumpConsumed;
    private bool      _jumpedThisFrame;
    private float     _timeSinceJumpRequested = Mathf.Infinity;
    private float     _timeSinceLastAbleToJump;
    private bool      _shouldSlide;          // player is holding slide key

    // ── Velocity Impulses ────────────────────────────────────────────────────
    /// <summary>
    /// External systems (explosions, launch pads, etc.) can call AddVelocity()
    /// to push the character. The impulse is applied at the next UpdateVelocity.
    /// </summary>
    private Vector3   _internalVelocityAdd;

    // ── Sliding State ────────────────────────────────────────────────────────
    private float     _slideTimer;

    // ── Wall Run State ───────────────────────────────────────────────────────
    private float     _wallRunTimer;
    private float     _wallRunCooldownTimer;   // prevents instant re-attachment
    private Vector3   _wallNormal;             // normal of the wall being run on
    private Vector3   _wallRunDirection;       // unit direction along the wall
    private bool      _wallRunJumpUsed;        // can only wall-jump once per wall run

    // ── Misc Flags ───────────────────────────────────────────────────────────
    private bool      _shouldExitWallRun;      // flagged inside UpdateVelocity, acted on in After

    // ── Physics Buffers (pre-allocated, no GC) ───────────────────────────────
    private Collider[]   _overlapBuffer = new Collider[8];
    private RaycastHit[] _wallHitBuffer = new RaycastHit[4];

    // ─────────────────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        Motor = GetComponent<KinematicCharacterMotor>();
        Motor.CharacterController = this;
        TransitionToState(CarCharacterState.Default);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  INPUT  (called by CarPlayer every Update)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called every frame by CarPlayer with raw input values.
    /// Converts raw input into processed movement & look vectors.
    /// </summary>
    public void SetInputs(ref CarCharacterInputs inputs)
    {
        // Build a movement vector clamped to unit length (diagonal ≠ faster)
        Vector3 rawInput = Vector3.ClampMagnitude(
            new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

        // Project camera forward onto the character's horizontal plane so that
        // pressing W always moves "away from camera" regardless of camera pitch.
        Vector3 camForward = Vector3.ProjectOnPlane(
            inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;

        if (camForward.sqrMagnitude < 0.001f)
        {
            // Fallback when camera is looking straight up/down
            camForward = Vector3.ProjectOnPlane(
                inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
        }

        Quaternion camPlanarRotation = Quaternion.LookRotation(camForward, Motor.CharacterUp);
        _moveInputVector = camPlanarRotation * rawInput;

        // Car-like: always face the direction you're moving
        if (_moveInputVector.sqrMagnitude > 0.001f)
            _lookInputVector = _moveInputVector.normalized;

        // ── Jump ─────────────────────────────────────────────────────────────
        if (inputs.JumpDown)
        {
            _timeSinceJumpRequested = 0f;
            _jumpRequested = true;
        }
        _jumpHeld = inputs.JumpHeld;

        // ── Slide ─────────────────────────────────────────────────────────────
        if (inputs.SlideDown)  _shouldSlide = true;
        if (inputs.SlideUp)    _shouldSlide = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  STATE MACHINE
    // ─────────────────────────────────────────────────────────────────────────

    public void TransitionToState(CarCharacterState newState)
    {
        CarCharacterState previous = CurrentState;
        OnStateExit(previous, newState);
        CurrentState = newState;
        OnStateEnter(newState, previous);
    }

    private void OnStateEnter(CarCharacterState state, CarCharacterState from)
    {
        switch (state)
        {
            case CarCharacterState.Default:
                // When returning from slide, capsule is restored in OnStateExit(Sliding)
                break;

            case CarCharacterState.Sliding:
                _slideTimer = 0f;
                // Shrink capsule to crouch height
                Motor.SetCapsuleDimensions(0.5f, Settings.SlideCapsuleHeight, Settings.SlideCapsuleHeight * 0.5f);
                // Squash the mesh root visually
                if (MeshRoot) MeshRoot.localScale = new Vector3(1f, 0.5f, 1f);
                // Burst of speed in the current facing direction
                _internalVelocityAdd += Motor.CharacterForward * Settings.SlideEntryBoost;
                break;

            case CarCharacterState.WallRunning:
                _wallRunTimer    = 0f;
                _wallRunJumpUsed = false;
                _shouldExitWallRun = false;
                // Detach from ground so the character can run along the wall
                Motor.ForceUnground();
                break;
        }
    }

    private void OnStateExit(CarCharacterState state, CarCharacterState to)
    {
        switch (state)
        {
            case CarCharacterState.Sliding:
                _shouldSlide = false;
                Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                if (MeshRoot) MeshRoot.localScale = Vector3.one;
                break;

            case CarCharacterState.WallRunning:
                // Start cooldown so we can't immediately re-attach to the same wall
                _wallRunCooldownTimer = Settings.WallRunCooldown;
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  KCC CALLBACKS  (called by KinematicCharacterMotor in order)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called FIRST in the motor's update cycle — before any movement is solved.
    /// Use this for probing (wall detection) and timer management.
    /// </summary>
    public void BeforeCharacterUpdate(float deltaTime)
    {
        // Tick cooldown timers
        if (_wallRunCooldownTimer > 0f)
            _wallRunCooldownTimer = Mathf.Max(0f, _wallRunCooldownTimer - deltaTime);

        // Attempt wall run entry while airborne in Default state
        if (CurrentState == CarCharacterState.Default && !Motor.GroundingStatus.IsStableOnGround)
            TryEnterWallRun();
    }

    /// <summary>
    /// Tell the motor what ROTATION the character should have this frame.
    /// Modify currentRotation (passed by ref) to set the character's orientation.
    /// </summary>
    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        switch (CurrentState)
        {
            case CarCharacterState.Default:
            case CarCharacterState.Sliding:
                HandleDefaultRotation(ref currentRotation, deltaTime);
                break;

            case CarCharacterState.WallRunning:
                HandleWallRunRotation(ref currentRotation, deltaTime);
                break;
        }
    }

    /// <summary>
    /// Tell the motor what VELOCITY the character should have this frame.
    /// Modify currentVelocity (passed by ref) — this is the ONLY place to set velocity.
    /// </summary>
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        switch (CurrentState)
        {
            case CarCharacterState.Default:
                HandleDefaultVelocity(ref currentVelocity, deltaTime);
                break;

            case CarCharacterState.Sliding:
                HandleSlidingVelocity(ref currentVelocity, deltaTime);
                break;

            case CarCharacterState.WallRunning:
                HandleWallRunVelocity(ref currentVelocity, deltaTime);
                break;
        }

        // Jump is processed after the base velocity so it cleanly overrides vertical component
        HandleJump(ref currentVelocity, deltaTime);

        // Apply any externally added impulse velocities (explosions, launch pads, etc.)
        if (_internalVelocityAdd.sqrMagnitude > 0f)
        {
            currentVelocity       += _internalVelocityAdd;
            _internalVelocityAdd   = Vector3.zero;
        }
    }

    /// <summary>
    /// Called LAST in the motor's update cycle — after movement has been fully solved.
    /// Use this for state transitions that depend on final movement results.
    /// </summary>
    public void AfterCharacterUpdate(float deltaTime)
    {
        // ── Jump timer & reset ────────────────────────────────────────────────
        _timeSinceJumpRequested += deltaTime;

        // If jump was buffered but hasn't fired within the buffer window, discard it
        if (_jumpRequested && _timeSinceJumpRequested > Settings.JumpBufferTime)
            _jumpRequested = false;

        // Reset jump availability when grounded (but not the same frame we jumped)
        if (Motor.GroundingStatus.IsStableOnGround)
        {
            if (!_jumpedThisFrame) _jumpConsumed = false;
            _timeSinceLastAbleToJump = 0f;
        }
        else
        {
            _timeSinceLastAbleToJump += deltaTime;
        }

        // ── State-specific after-update logic ────────────────────────────────
        switch (CurrentState)
        {
            case CarCharacterState.Default:
                AfterDefaultUpdate(deltaTime);
                break;

            case CarCharacterState.Sliding:
                AfterSlidingUpdate(deltaTime);
                break;

            case CarCharacterState.WallRunning:
                // Exit flagged inside HandleWallRunVelocity — process it here
                if (_shouldExitWallRun)
                {
                    _shouldExitWallRun = false;
                    TransitionToState(CarCharacterState.Default);
                }
                break;
        }

        _jumpedThisFrame = false;
    }

    /// <summary>
    /// Called right after the motor evaluates grounding status for this frame.
    /// Good place to trigger landing/airborne events (sound, particles, animation).
    /// </summary>
    public void PostGroundingUpdate(float deltaTime)
    {
        bool justLanded   = Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround;
        bool justLeftGround = !Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround;

        if (justLanded)    OnLanded();
        if (justLeftGround) OnLeftGround();
    }

    /// <summary>
    /// Return false here to make the character pass through a specific collider.
    /// Useful for one-way platforms, team-mates, etc.
    /// </summary>
    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true; // Override for custom filtering (e.g. IgnoredColliders list)
    }

    /// <summary>Called when the character's ground probe hits something.</summary>
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport) { }

    /// <summary>
    /// Called each time the character's movement sweep hits something.
    /// Perfect place to detect wall-run entry when physically running into a wall.
    /// </summary>
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        ref HitStabilityReport hitStabilityReport)
    {
        // If airborne and in Default state, check if this hit qualifies as a wall run
        if (CurrentState != CarCharacterState.Default) return;
        if (Motor.GroundingStatus.IsStableOnGround)    return;
        if (_wallRunCooldownTimer > 0f)                return;

        float wallAngle = Vector3.Angle(hitNormal, Motor.CharacterUp);
        if (wallAngle < Settings.MinWallAngle)         return;

        float speed = Motor.Velocity.magnitude;
        if (speed < Settings.WallRunMinEntrySpeed)     return;

        // Valid wall — set up wall run direction and transition
        _wallNormal       = hitNormal;
        _wallRunDirection = Vector3.ProjectOnPlane(Motor.CharacterForward, hitNormal).normalized;

        if (_wallRunDirection.sqrMagnitude > 0.1f)
            TransitionToState(CarCharacterState.WallRunning);
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
        Vector3 atCharacterPosition, Quaternion atCharacterRotation,
        ref HitStabilityReport hitStabilityReport) { }

    public void OnDiscreteCollisionDetected(Collider hitCollider) { }

    // ─────────────────────────────────────────────────────────────────────────
    //  PUBLIC UTILITY
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Add an instant velocity impulse (explosion, launch pad, etc.).
    /// Will be applied on the next UpdateVelocity call.
    /// </summary>
    public void AddVelocity(Vector3 velocity)
    {
        _internalVelocityAdd += velocity;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ROTATION HANDLERS
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleDefaultRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (_lookInputVector.sqrMagnitude < 0.001f) return;

        // Scale turn sharpness using the designer's curve based on current speed
        float currentSpeed = Vector3.ProjectOnPlane(Motor.BaseVelocity, Motor.CharacterUp).magnitude;
        float speedRatio   = Mathf.Clamp01(currentSpeed / Settings.MaxForwardSpeed);
        float turnMult     = Settings.TurnSpeedCurve.Evaluate(speedRatio);

        // Convert degrees/second to the sharpness value used by exponential smoothing
        // At RotationSharpness=1 the turn rate is slow; multiply by turnMult for curves
        float sharpness = (Settings.BaseTurnSpeed / 180f) * turnMult * Settings.RotationSharpness;

        Vector3 smoothedLook = Vector3.Slerp(
            Motor.CharacterForward, _lookInputVector,
            1f - Mathf.Exp(-sharpness * deltaTime)).normalized;

        currentRotation = Quaternion.LookRotation(smoothedLook, Motor.CharacterUp);

        // Optional: tilt the character to match the ground slope
        if (Settings.OrientToSlopes && Motor.GroundingStatus.IsStableOnGround)
        {
            Vector3 currentUp   = currentRotation * Vector3.up;
            Vector3 smoothedUp  = Vector3.Slerp(currentUp, Motor.GroundingStatus.GroundNormal,
                1f - Mathf.Exp(-Settings.SlopeOrientSharpness * deltaTime));
            currentRotation = Quaternion.FromToRotation(currentUp, smoothedUp) * currentRotation;
        }
    }

    private void HandleWallRunRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (_wallRunDirection.sqrMagnitude < 0.001f) return;

        Vector3 smoothedDir = Vector3.Slerp(
            Motor.CharacterForward, _wallRunDirection,
            1f - Mathf.Exp(-Settings.RotationSharpness * deltaTime)).normalized;

        currentRotation = Quaternion.LookRotation(smoothedDir, Motor.CharacterUp);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  VELOCITY HANDLERS
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleDefaultVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (Motor.GroundingStatus.IsStableOnGround)
        {
            // ── GROUNDED ─────────────────────────────────────────────────────
            // Preserve speed magnitude but reorient velocity along slope so the
            // character doesn't bounce off ramps or clip into them.
            float speed = currentVelocity.magnitude;
            currentVelocity = Motor.GetDirectionTangentToSurface(
                currentVelocity, Motor.GroundingStatus.GroundNormal) * speed;

            // --- Horizontal speed for curve sampling ---
            float hSpeed    = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp).magnitude;
            float speedRatio = Mathf.Clamp01(hSpeed / Settings.MaxForwardSpeed);

            if (_moveInputVector.sqrMagnitude > 0.001f)
            {
                // Reorient input vector along slope so uphill input actually goes uphill
                Vector3 inputRight      = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                Vector3 reorientedInput = Vector3.Cross(
                    Motor.GroundingStatus.GroundNormal, inputRight).normalized * _moveInputVector.magnitude;

                // Choose max speed based on direction of input
                bool    movingForward  = Vector3.Dot(_moveInputVector, Motor.CharacterForward) >= 0f;
                float   targetSpeed    = movingForward ? Settings.MaxForwardSpeed : Settings.MaxReverseSpeed;
                Vector3 targetVelocity = reorientedInput * targetSpeed;

                // Acceleration sharpness is reduced at high speeds by the AccelerationCurve
                float accelMult    = Settings.AccelerationCurve.Evaluate(speedRatio);
                float sharpness    = Settings.AccelerationSharpness * accelMult;
                currentVelocity    = Vector3.Lerp(currentVelocity, targetVelocity,
                    1f - Mathf.Exp(-sharpness * deltaTime));
            }
            else
            {
                // No input → decelerate toward zero
                // DecelerationCurve can make high-speed braking harder (tank-like)
                float decelMult = Settings.DecelerationCurve.Evaluate(speedRatio);
                currentVelocity = Vector3.MoveTowards(
                    currentVelocity, Vector3.zero,
                    Settings.DecelerationForce * decelMult * deltaTime);
            }
        }
        else
        {
            // ── AIRBORNE ─────────────────────────────────────────────────────
            if (_moveInputVector.sqrMagnitude > 0.001f)
            {
                Vector3 addedVel  = _moveInputVector * Settings.AirAcceleration * deltaTime;
                Vector3 horizVel  = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                if (horizVel.magnitude < Settings.MaxAirSpeed)
                {
                    // Clamp so total horizontal velocity stays within MaxAirSpeed
                    Vector3 clamped = Vector3.ClampMagnitude(horizVel + addedVel, Settings.MaxAirSpeed);
                    addedVel = clamped - horizVel;
                }
                else if (Vector3.Dot(horizVel, addedVel) > 0f)
                {
                    // Already above max — only allow steering, not further acceleration
                    addedVel = Vector3.ProjectOnPlane(addedVel, horizVel.normalized);
                }

                currentVelocity += addedVel;
            }

            // Apply gravity and drag every air frame
            currentVelocity += Settings.Gravity * deltaTime;
            currentVelocity *= 1f / (1f + Settings.AirDrag * deltaTime);
        }
    }

    private void HandleSlidingVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _slideTimer += deltaTime;
        float normTime = Mathf.Clamp01(_slideTimer / Settings.MaxSlideDuration);
        float friction = Settings.SlideFrictionCurve.Evaluate(normTime);

        if (Motor.GroundingStatus.IsStableOnGround)
        {
            // Keep velocity hugging the slope surface
            float speed = currentVelocity.magnitude;
            currentVelocity = Motor.GetDirectionTangentToSurface(
                currentVelocity, Motor.GroundingStatus.GroundNormal) * speed;

            // Downhill boost — project gravity direction onto slope for extra pull
            Vector3 gravityOnSlope = Vector3.ProjectOnPlane(
                Settings.Gravity.normalized, Motor.GroundingStatus.GroundNormal);
            currentVelocity += gravityOnSlope * Settings.SlopeSlideBoost * deltaTime;

            // Friction slows down the slide over time (curve-driven)
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, friction * deltaTime);
        }
        else
        {
            // Became airborne mid-slide (ran off a ledge) — behave like normal air movement
            currentVelocity += Settings.Gravity * deltaTime;
            currentVelocity *= 1f / (1f + Settings.AirDrag * deltaTime);
        }
    }

    private void HandleWallRunVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _wallRunTimer += deltaTime;
        float normTime = Mathf.Clamp01(_wallRunTimer / Settings.WallRunMaxDuration);

        // Check the wall is still there — exit if it disappeared
        if (!DetectWall(out Vector3 refreshedNormal))
        {
            _shouldExitWallRun = true;
            // Let gravity take over while we're flagged for exit this frame
            currentVelocity += Settings.Gravity * deltaTime;
            return;
        }

        // Update wall normal in case the surface is curved
        _wallNormal = refreshedNormal;

        // Refresh the run direction from actual velocity so the character
        // naturally follows curved walls and user steering input
        Vector3 projectedVel = Vector3.ProjectOnPlane(currentVelocity, _wallNormal);
        if (projectedVel.sqrMagnitude > 0.01f)
        {
            // Slightly bias toward the move input direction for player control
            Vector3 inputAlongWall = Vector3.ProjectOnPlane(_moveInputVector, _wallNormal).normalized;
            Vector3 blendedDir     = (projectedVel.normalized + inputAlongWall * 0.3f).normalized;
            _wallRunDirection      = blendedDir;
        }

        // Sample speed and gravity multipliers from the designer's curves
        float gravScale  = Settings.WallRunGravityCurve.Evaluate(normTime);
        float speedScale = Settings.WallRunSpeedCurve.Evaluate(normTime);
        float targetSpeed = Settings.WallRunSpeed * speedScale;

        // Smoothly interpolate toward the target wall-run velocity
        Vector3 targetVelocity = _wallRunDirection * targetSpeed;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity,
            1f - Mathf.Exp(-Settings.AccelerationSharpness * deltaTime));

        // Apply gravity scaled by curve (starts low, increases as timer runs out)
        currentVelocity += Settings.Gravity * gravScale * deltaTime;

        // Time's up — flag for exit (actual transition happens in AfterCharacterUpdate)
        if (_wallRunTimer >= Settings.WallRunMaxDuration)
            _shouldExitWallRun = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  JUMP HANDLER  (shared across states)
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleJump(ref Vector3 currentVelocity, float deltaTime)
    {
        _jumpedThisFrame = false;

        if (!_jumpRequested || _jumpConsumed) return;

        bool   canJump    = false;
        bool   wallJump   = false;
        Vector3 jumpDir   = Motor.CharacterUp;

        switch (CurrentState)
        {
            case CarCharacterState.Default:
                // Standard jump: grounded OR within coyote time
                canJump = Motor.GroundingStatus.IsStableOnGround ||
                          _timeSinceLastAbleToJump <= Settings.CoyoteTime;
                break;

            case CarCharacterState.Sliding:
                if (!Settings.AllowJumpFromSlide) break;
                canJump = Motor.GroundingStatus.IsStableOnGround ||
                          _timeSinceLastAbleToJump <= Settings.CoyoteTime;
                if (canJump)
                    TransitionToState(CarCharacterState.Default); // exit slide on jump
                break;

            case CarCharacterState.WallRunning:
                if (_wallRunJumpUsed) break;    // only one wall jump per wall run
                // Wall jump: up + away from wall
                wallJump   = true;
                canJump    = true;
                _wallRunJumpUsed = true;
                break;
        }

        if (!canJump) return;

        Motor.ForceUnground(); // Detach from ground — without this the probe snaps you back

        if (wallJump)
        {
            // Wall jump replaces current velocity completely
            currentVelocity  = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp); // clear vertical
            currentVelocity += Motor.CharacterUp  * Settings.WallJumpUpSpeed;
            currentVelocity += _wallNormal        * Settings.WallJumpAwaySpeed;
            TransitionToState(CarCharacterState.Default);
        }
        else
        {
            // Standard jump: clear vertical component then apply jump speed
            currentVelocity -= Vector3.Project(currentVelocity, Motor.CharacterUp);
            currentVelocity += Motor.CharacterUp * Settings.JumpUpSpeed;
            currentVelocity += _moveInputVector  * Settings.JumpForwardBoost;
        }

        _jumpRequested = false;
        _jumpConsumed  = true;
        _jumpedThisFrame = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  AFTER-UPDATE HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private void AfterDefaultUpdate(float deltaTime)
    {
        // Enter slide if player is holding slide and is fast enough on the ground
        if (_shouldSlide && Motor.GroundingStatus.IsStableOnGround)
        {
            float hSpeed = Vector3.ProjectOnPlane(Motor.BaseVelocity, Motor.CharacterUp).magnitude;
            if (hSpeed >= Settings.MinSlideEntrySpeed)
                TransitionToState(CarCharacterState.Sliding);
        }
    }

    private void AfterSlidingUpdate(float deltaTime)
    {
        bool shouldExit = false;

        // Designer-set duration expired
        if (_slideTimer >= Settings.MaxSlideDuration) shouldExit = true;

        // Player released slide key
        if (!_shouldSlide) shouldExit = true;

        // Character has almost stopped (avoid getting stuck)
        float hSpeed = Vector3.ProjectOnPlane(Motor.BaseVelocity, Motor.CharacterUp).magnitude;
        if (hSpeed < 0.8f && _slideTimer > 0.25f) shouldExit = true;

        if (!shouldExit) return;

        // Before standing up, make sure there is room above the character
        Motor.SetCapsuleDimensions(0.5f, 2f, 1f); // temporarily try full height
        bool blocked = Motor.CharacterOverlap(
            Motor.TransientPosition, Motor.TransientRotation,
            _overlapBuffer, Motor.CollidableLayers, QueryTriggerInteraction.Ignore) > 0;

        if (blocked)
        {
            // Not enough room — keep crouched for now
            Motor.SetCapsuleDimensions(0.5f, Settings.SlideCapsuleHeight, Settings.SlideCapsuleHeight * 0.5f);
        }
        else
        {
            TransitionToState(CarCharacterState.Default);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  WALL DETECTION
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempt to enter wall run while in the air.
    /// Uses side raycasts from the capsule centre for efficiency.
    /// </summary>
    private void TryEnterWallRun()
    {
        if (_wallRunCooldownTimer > 0f) return;
        if (Motor.Velocity.magnitude < Settings.WallRunMinEntrySpeed) return;

        if (DetectWall(out Vector3 normal))
        {
            _wallNormal       = normal;
            _wallRunDirection = Vector3.ProjectOnPlane(Motor.CharacterForward, normal).normalized;
            if (_wallRunDirection.sqrMagnitude > 0.1f)
                TransitionToState(CarCharacterState.WallRunning);
        }
    }

    /// <summary>
    /// Casts a ray to the left and right of the character looking for a valid wall.
    /// Returns true and fills wallNormal if a suitable wall is found.
    /// </summary>
    private bool DetectWall(out Vector3 wallNormal)
    {
        wallNormal = Vector3.zero;

        // Raycast origin: capsule centre in world space
        Vector3 origin = Motor.TransientPosition + (Motor.TransientRotation * Motor.CharacterTransformToCapsuleCenter);

        Vector3[] checkDirections = { Motor.CharacterRight, -Motor.CharacterRight };

        foreach (Vector3 dir in checkDirections)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit,
                Settings.WallDetectionDistance,
                Motor.CollidableLayers,
                QueryTriggerInteraction.Ignore))
            {
                float wallAngle = Vector3.Angle(hit.normal, Motor.CharacterUp);
                if (wallAngle >= Settings.MinWallAngle)
                {
                    wallNormal = hit.normal;
                    return true;
                }
            }
        }

        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  GROUND EVENTS
    // ─────────────────────────────────────────────────────────────────────────

    private void OnLanded()
    {
        // Exit wall run if we somehow landed while wall running
        if (CurrentState == CarCharacterState.WallRunning)
            TransitionToState(CarCharacterState.Default);

        // Hook: add landing effects here (camera shake, sound, particles, etc.)
    }

    private void OnLeftGround()
    {
        // Hook: play jump/fall audio, trigger animation, etc.
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  DEBUG GIZMOS
    // ─────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (Motor == null) return;

        Vector3 origin = transform.position +
            (transform.rotation * (Motor != null ? Motor.CharacterTransformToCapsuleCenter : Vector3.up));

        // Wall detection rays
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, transform.right  * (Settings != null ? Settings.WallDetectionDistance : 0.75f));
        Gizmos.DrawRay(origin, -transform.right * (Settings != null ? Settings.WallDetectionDistance : 0.75f));

        // State indicator
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f,
                $"State: {CurrentState}\nSpeed: {Motor.BaseVelocity.magnitude:F1} m/s");
        }
    }
#endif
}
