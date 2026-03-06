using Unity.Mathematics;
using UnityEngine;

public class JumpState : IPlayerState
{
    // The state requires the Player context upon creation
    private Player player;
    public JumpState(Player playerContext){player = playerContext;}

    public void EnterState()
    {
        //add upwards force
    }
    public void UpdateState()
    {
        
    }
    public void ExitState()
    {
        
    }
    public IPlayerState CheckSwitchStates()
    {
        if(player.positionState==0) return player.IdleState;
        return null;
    }
}
