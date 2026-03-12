using Unity.Mathematics;
using UnityEngine;

public class DashState : IPlayerState
{
    // The state requires the Player context upon creation
    private Player player;
    public DashState(Player playerContext){player = playerContext;}

    private float _dashDuration;

    public void EnterState()
    {
        _dashDuration=Time.time;
        Vector3 target=Vector3.ProjectOnPlane(player.inputHandler.inputBuffer.camAlignedMove,Vector3.up).normalized;
        player.kinematicPhysics.velocity+=target*player.stats.dashStr;
        CharacterRotation();
    }
    public void UpdateState()
    {

    }
    public void ExitState()
    {
        
    }
    public IPlayerState CheckSwitchStates()
    {
        if(_dashDuration+0.3f<=Time.time)
        {
            if (!player.kinematicPhysics.grounded) return player.FallState;
            else if (player.inputHandler.inputBuffer.Move.sqrMagnitude>0) return player.MoveState;
            else return player.IdleState;
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
