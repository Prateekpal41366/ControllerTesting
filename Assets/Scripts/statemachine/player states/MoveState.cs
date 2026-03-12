using Unity.Mathematics;
using UnityEngine;

public class MoveState : IPlayerState
{
    // The state requires the Player context upon creation
    private Player player;
    public MoveState(Player playerContext){player = playerContext;}
    
    public void EnterState()
    {
        
    }
    public void UpdateState()
    {
        // combine input with speed and projection
        Vector3 target =Vector3.ProjectOnPlane(
            player.inputHandler.inputBuffer.camAlignedMove,
            player.kinematicPhysics.groundSlopeNormal
        )*player.stats.moveSpeed;
        target.y=player.kinematicPhysics.velocity.y;

        //lerping
        Vector3 vel=player.kinematicPhysics.velocity;
        vel=Vector3.MoveTowards(vel,target,250*Time.fixedDeltaTime);
        //applying
        player.kinematicPhysics.velocity=new Vector3(vel.x,player.kinematicPhysics.velocity.y,vel.z);

        CharacterRotation();
    }
    public void ExitState()
    {
        
    }
    public IPlayerState CheckSwitchStates()
    {
        if (player.inputHandler.inputBuffer.Move.sqrMagnitude<math.EPSILON) return player.IdleState;
        if (Time.time-player.inputHandler.inputBuffer.Jump<=0.2 && player.kinematicPhysics.grounded) {return player.JumpState;}
        if (Time.time - player.inputHandler.inputBuffer.Dash <= 0.2f) return player.DashState;
        if (!player.kinematicPhysics.grounded) return player.FallState;
        return null;
    }

    private void CharacterRotation()
    {
        Vector3 target=Vector3.ProjectOnPlane
        (
            player.inputHandler.inputBuffer.camAlignedMove,
            player.kinematicPhysics.groundSlopeNormal
        );
        player.targetLookDirection = Quaternion.LookRotation
        (
            target,
            player.kinematicPhysics.groundSlopeNormal
        );
    }
}
