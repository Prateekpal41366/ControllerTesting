using System;
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
        public Vector3 camAlignedMove;
        public Vector3 camForward;
        public Vector3 camUp;
        public float Jump;
        public bool JumpHold;
        public float Dash;
        public bool DashHold;
    }
    public InputBuffer inputBuffer=new InputBuffer();

    //updating values
    private void UpdateInputBuffer()
    {
        //wasd and mouse
        inputBuffer.Move=moveInput.action.ReadValue<Vector2>();
        inputBuffer.Mouse=mouseInput.action.ReadValue<Vector2>();

        //buffer times
        //jump
        if (jumpInput.action.WasPressedThisFrame()) inputBuffer.Jump = Time.time;
        inputBuffer.JumpHold=jumpInput.action.IsPressed();

        //dash
        if (dashInput.action.WasPressedThisFrame()) inputBuffer.Dash = Time.time;
        inputBuffer.DashHold=dashInput.action.IsPressed();

        AlignMoveToCam();
    }

    private void AlignMoveToCam()
    {
        Vector3 camRight=Vector3.Cross(inputBuffer.camForward,inputBuffer.camUp);
        inputBuffer.camAlignedMove=(inputBuffer.camForward*inputBuffer.Move.y+
            camRight*-inputBuffer.Move.x).normalized;
    }

    void Start()
    {
        //prevents actions on start
        inputBuffer.Jump=-10f;
        inputBuffer.Dash=-10f;
    }

    void Update()
    {
        UpdateInputBuffer();
    }
}