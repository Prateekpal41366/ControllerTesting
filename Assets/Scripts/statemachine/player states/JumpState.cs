using Unity.Mathematics;
using UnityEngine;

public class JumpState : IPlayerState
{
    // The state requires the Player context upon creation
    private Player player;
    public JumpState(Player playerContext){player = playerContext;}

    private float _jumpDuration;

    public void EnterState()
    {
        _jumpDuration=Time.time;
        player.kinematicPhysics.velocity+=Vector3.up*player.stats.jumpStr;
    }
    public void UpdateState()
    {
        // combine input with speed and projection
        Vector3 target =Vector3.ProjectOnPlane(
            player.inputHandler.inputBuffer.camAlignedMove,
            Vector3.up
        )*player.stats.strafeSpeed;
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
        if(_jumpDuration+0.5<=Time.time)
        {
            return player.FallState;
        }
        // 2. Early exit: If we hit a ceiling
        // (Assuming you added a ceiling check to your motor)
        if (player.kinematicPhysics.velocity.y <= 0) 
        {
            return player.FallState;
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
