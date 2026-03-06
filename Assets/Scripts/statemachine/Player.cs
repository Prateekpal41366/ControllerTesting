using KinematicCharacterController;
using UnityEngine;

public class Player : MonoBehaviour
{
    public InputHandler inputHandler;
    private KinematicCharacterMotor characterMotor;
    public enum PositionState { 
        Grounded, 
        Air,
        water,
    }
    public PositionState positionState=new PositionState();

    private IPlayerState currentState;
    
    // Exposed so states can easily return them in CheckSwitchStates()
    public IdleState IdleState { get; private set; } 
    public MoveState MoveState { get; private set; }
    public JumpState JumpState { get; private set; }

    private void Awake()
    {
        inputHandler=GetComponent<InputHandler>();
        characterMotor=GetComponent<KinematicCharacterMotor>();

        // CONTEXT INJECTION: We pass 'this' to the states so they know who they belong to
        IdleState = new IdleState(this);
        MoveState = new MoveState(this);
        JumpState = new JumpState(this);

        currentState = IdleState;
        currentState.EnterState();
    }

    private void Update()
    {
        GroundCheck();
        
        SwitchStates(currentState.CheckSwitchStates());
        currentState.UpdateState();
        Debug.Log(currentState);
    }

    private void SwitchStates(IPlayerState newState)
    {
        if (newState == null || newState == currentState) return; 
        
        currentState.ExitState();
        newState.EnterState();
        currentState = newState;
    }

    private void GroundCheck()
    {
        // Implementation...
        if (characterMotor.GroundingStatus.IsStableOnGround)
        {
            positionState=0;
        }
    }
}