using UnityEngine;

public class Player : MonoBehaviour
{
    //refrences to scripts
    public InputHandler inputHandler;
    public KinematicPhysics kinematicPhysics;
    public PlayerStats stats;
    public Animator animator;

    //variables
    public Quaternion targetLookDirection;

    //state instances
    private IPlayerState currentState;
    // Exposed so states can easily return them in CheckSwitchStates()
    public IdleState IdleState { get; private set; } 
    public MoveState MoveState { get; private set; }
    public JumpState JumpState { get; private set; }
    public FallState FallState { get; private set; }

    private void Awake()
    {
        inputHandler=GetComponent<InputHandler>();
        kinematicPhysics=GetComponent<KinematicPhysics>();
        animator=GetComponentInChildren<Animator>();

        // instantiate state and context injection
        IdleState = new IdleState(this);
        MoveState = new MoveState(this);
        JumpState=new JumpState(this);
        FallState=new FallState(this);

        currentState = IdleState;
        currentState.EnterState();
    }

    private void Update()
    {        
        SwitchStates(currentState.CheckSwitchStates());
        currentState.UpdateState();
        RotateCharacter();
      //  Debug.Log(currentState);
    }

    private void SwitchStates(IPlayerState newState)
    {
        if (newState == null || newState == currentState) return; 
        
        currentState.ExitState();
        newState.EnterState();
        currentState = newState;
    }

    private void RotateCharacter()
    {
        if (targetLookDirection==null) return;
        // Smoothly interpolate to the target
        // We use a high multiplier if we want "snappy" transitions between modes
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetLookDirection, 
            10f * Time.deltaTime
        );
    }
}