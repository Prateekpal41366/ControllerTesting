using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;

/// <summary>
/// Input handler for the car character controller.
/// Reads raw Unity input every frame and forwards it to CarCharacterController.
///
/// SEPARATION OF CONCERNS:
///   CarPlayer    — only knows about input and camera.
///   CarCharacter — only knows about movement logic.
/// This means AI characters can reuse CarCharacterController by sending inputs
/// programmatically without ever touching this class.
///
/// SETUP:
///   1. Create an empty "Player" GameObject in your scene.
///   2. Add this component.
///   3. Assign the Character and CharacterCamera fields in the Inspector.
/// </summary>
public class CarPlayer : MonoBehaviour
{
    // ── Inspector References ─────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("The CarCharacterController this player controls.")]
    public CarCharacterController Character;

    [Tooltip("The ExampleCharacterCamera (from KCC examples) that follows the character.")]
    public ExampleCharacterCamera CharacterCamera;

    // ── Input Key Bindings ───────────────────────────────────────────────────
    [Header("Key Bindings")]
    [Tooltip("Key used to jump.")]
    public KeyCode JumpKey = KeyCode.Space;

    [Tooltip("Key used to start a slide (hold).")]
    public KeyCode SlideKey = KeyCode.C;

    [Tooltip("Mouse button index to re-lock cursor (0 = left click).")]
    public int RelockCursorButton = 0;

    // ─────────────────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Lock and hide cursor for FPS-style mouse look
        Cursor.lockState = CursorLockMode.Locked;

        // Tell the camera which transform to orbit around (empty child on character)
        CharacterCamera.SetFollowTransform(Character.CameraFollowPoint);

        // Make the camera ignore the character's own colliders so it doesn't
        // zoom in when looking straight down at the character.
        CharacterCamera.IgnoredColliders.Clear();
        CharacterCamera.IgnoredColliders.AddRange(Character.GetComponentsInChildren<Collider>());
    }

    private void Update()
    {
        // Re-lock cursor on click (useful after tabbing out)
        if (Input.GetMouseButtonDown(RelockCursorButton))
            Cursor.lockState = CursorLockMode.Locked;

        HandleCharacterInput();
    }

    private void LateUpdate()
    {
        // Camera runs in LateUpdate so it reacts to the character's final
        // position for that frame, preventing one-frame lag.
        HandleCameraInput();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  INPUT HANDLERS
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleCharacterInput()
    {
        CarCharacterInputs inputs = new CarCharacterInputs
        {
            // Raw axis input — GetAxisRaw gives snappy response (no Unity smoothing)
            MoveAxisForward  = Input.GetAxisRaw("Vertical"),
            MoveAxisRight    = Input.GetAxisRaw("Horizontal"),

            // Pass camera rotation so the character knows which way "forward" is
            CameraRotation   = CharacterCamera.Transform.rotation,

            // Jump: fire once on press (JumpDown), track hold for future variable height
            JumpDown         = Input.GetKeyDown(JumpKey),
            JumpHeld         = Input.GetKey(JumpKey),

            // Slide: detect press and release separately so the character can
            // tell when the player starts and stops holding
            SlideDown        = Input.GetKeyDown(SlideKey),
            SlideUp          = Input.GetKeyUp(SlideKey),
        };

        Character.SetInputs(ref inputs);
    }

    private void HandleCameraInput()
    {
        // Only process mouse look when cursor is locked
        Vector3 lookInput = Vector3.zero;
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            lookInput = new Vector3(
                Input.GetAxisRaw("Mouse X"),
                Input.GetAxisRaw("Mouse Y"),
                0f);
        }

        // Scroll wheel for zoom (disabled in WebGL — can cause browser scroll conflicts)
        float scrollInput = 0f;
#if !UNITY_WEBGL
        scrollInput = -Input.GetAxis("Mouse ScrollWheel");
#endif

        CharacterCamera.UpdateWithInput(Time.deltaTime, scrollInput, lookInput);

        // Right-click toggles between orbit and first-person (distance 0)
        if (Input.GetMouseButtonDown(1))
        {
            CharacterCamera.TargetDistance = Mathf.Approximately(CharacterCamera.TargetDistance, 0f)
                ? CharacterCamera.DefaultDistance
                : 0f;
        }
    }
}
