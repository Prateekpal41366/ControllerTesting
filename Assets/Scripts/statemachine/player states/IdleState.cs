using UnityEngine;

public class IdleState : IPlayerState
{
    // The state requires the Player context upon creation
    private Player player;
    public IdleState(Player playerContext){player = playerContext;}

    public void EnterState()
    {
        //reset velocity
        player.kinematicPhysics.velocity=Vector3.up*player.kinematicPhysics.velocity.y;

        //character rotation
        Vector3 target=new Vector3(player.transform.rotation.x,0,player.transform.rotation.z);
        player.targetLookDirection=Quaternion.LookRotation(target,player.kinematicPhysics.groundSlopeNormal);
    }
    public void UpdateState()
    {
        
    }
    public void ExitState()
    {
        
    }
    public IPlayerState CheckSwitchStates()
    {
        if (player.inputHandler.inputBuffer.Move.sqrMagnitude>0) return player.MoveState;
        if (Time.time-player.inputHandler.inputBuffer.Jump<=0.2 && player.kinematicPhysics.grounded) 
        {
            return player.JumpState;
        }
        if (!player.kinematicPhysics.grounded) return player.FallState;
        return null;
    }
}
