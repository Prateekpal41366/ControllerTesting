using Unity.Mathematics;
using UnityEngine;

public class FallState : IPlayerState
{
    // The state requires the Player context upon creation
    private Player player;
    public FallState(Player playerContext){player = playerContext;}

    public void EnterState()
    {
        
    }
    public void UpdateState()
    {
        // combine input with speed and projection
        Vector3 target =Vector3.ProjectOnPlane(
            player.inputHandler.inputBuffer.camAlignedMove,
            Vector3.up
        )*player.stats.airSpeed;
        target.y=player.kinematicPhysics.velocity.y;

        //lerping
        Vector3 vel=player.kinematicPhysics.velocity;
        vel=Vector3.MoveTowards(vel,target,100*Time.fixedDeltaTime);
        //applying
        player.kinematicPhysics.velocity=new Vector3(vel.x,player.kinematicPhysics.velocity.y,vel.z);

        CharacterRotation();
    }
    public void ExitState()
    {
        
    }
    public IPlayerState CheckSwitchStates()
    {   
        if (Time.time - player.inputHandler.inputBuffer.Dash <= 0.2f)
        {
            return player.DashState;
        }
        if (player.kinematicPhysics.grounded)
        {
            if (player.inputHandler.inputBuffer.Move.sqrMagnitude<math.EPSILON) return player.IdleState;
            if(player.inputHandler.inputBuffer.Move.sqrMagnitude>0) return player.MoveState;
        }
        return null;
    }

    private void CharacterRotation()
    {
        Vector3 target=Vector3.ProjectOnPlane
        (
            player.inputHandler.inputBuffer.camAlignedMove,
            Vector3.up
        );
        player.targetLookDirection = Quaternion.LookRotation
        (
            target,
            Vector3.up
        );
    }
}
