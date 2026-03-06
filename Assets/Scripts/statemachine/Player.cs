using System;
using KinematicCharacterController;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] public Vector3 Velocity=Vector3.zero;
    [SerializeField] private Vector3 Gravity=Vector3.down*10f;

    public InputHandler inputHandler;
    public enum PositionState { 
        Grounded, 
        Air,
        water,
    }
    public PositionState positionState=new PositionState();

    public enum OrientationMethod
    {
        None,
        TowardsGravity,
        TowardsGroundSlopeAndGravity,
    }
    public OrientationMethod orientationMethod=new OrientationMethod();

    private IPlayerState currentState;
    
    // Exposed so states can easily return them in CheckSwitchStates()
    public IdleState IdleState { get; private set; } 
    public MoveState MoveState { get; private set; }
    public JumpState JumpState { get; private set; }

    private void Awake()
    {
        inputHandler=GetComponent<InputHandler>();

        // creating state instances
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
        Debug.Log(positionState);
    }

    private void SwitchStates(IPlayerState newState)
    {
        if (newState == null || newState == currentState) return; 
        
        currentState.ExitState();
        newState.EnterState();
        currentState = newState;
    }

    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask waterLayer;

    private void GroundCheck()
    {

        // Implement coyote jumps
        RaycastHit hit;
        bool hasHit = Physics.SphereCast(
            transform.position,
            0.3f,
            Vector3.down,
            out hit,
            0.5f
        );

        if (!hasHit)
        {
            positionState=PositionState.Air;
            return;
        }
        int hitLayer=hit.collider.gameObject.layer;
        if((groundLayer&(1<<hitLayer))!=0) 
        {
            positionState=PositionState.Grounded;
            return;
        }
        if((waterLayer&(1<<hitLayer))!=0) 
        {
            positionState=PositionState.water;
            return;
        }
    }

    void FixedUpdate()
    {
        UpdateVelocity();
    }

    private void UpdateVelocity()
    {
       // Velocity+=Gravity;
        transform.position+=Velocity*Time.fixedDeltaTime;
    }
}