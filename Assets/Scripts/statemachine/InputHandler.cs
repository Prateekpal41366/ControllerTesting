using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    //input refrences
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveInput;
    [SerializeField] private InputActionReference mouseInput;
    [SerializeField] private InputActionReference jumpInput;
    [SerializeField] private InputActionReference dashInput;

    //buffer setting and variables
    [Header("Input Settings")]
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float dashBufferTime = 0.15f;
    private float jumpBuffer=0;
    private float dashBuffer=0;

    //enabling input actions
    private void OnEnable()
    {
        moveInput.action.Enable();
        mouseInput.action.Enable();
        jumpInput.action.Enable();
        dashInput.action.Enable();
    }

    //disabling actions
    private void OnDisable()
    {
        moveInput.action.Disable();
        mouseInput.action.Disable();
        jumpInput.action.Disable();
        dashInput.action.Disable();
    }

    //input buffer that is passed out
    public struct InputBuffer 
    {
        public Vector2 Move;
        public Vector2 Mouse;
        public bool Jump;
        public bool JumpHold;
        public bool Dash;
        public bool DashHold;
    }
    public InputBuffer inputBuffer=new InputBuffer();

    //updating values
    private void UpdateInputBuffer()
    {
        inputBuffer.Move=moveInput.action.ReadValue<Vector2>();
        inputBuffer.Mouse=mouseInput.action.ReadValue<Vector2>();

        // Handle Jump Timing (Buffer)
        if (jumpInput.action.WasPressedThisFrame()) jumpBuffer = Time.time;
        // Is the jump still within the valid window?
        inputBuffer.Jump = (Time.time - jumpBuffer) <= jumpBufferTime;

        inputBuffer.JumpHold=jumpInput.action.IsPressed();

        //dash
        if (dashInput.action.WasPressedThisFrame()) dashBuffer = Time.time;
        inputBuffer.Dash = (Time.time - dashBuffer) <= dashBufferTime;

        inputBuffer.DashHold=dashInput.action.IsPressed();
    }

    void Update()
    {
        UpdateInputBuffer();
    }
}