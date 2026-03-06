using Unity.Mathematics;
using UnityEngine;

public class IdleState : IPlayerState
{
    // The state requires the Player context upon creation
    private Player player;
    public IdleState(Player playerContext){player = playerContext;}

    public void EnterState()
    {
        
    }
    public void UpdateState()
    {
        
    }
    public void ExitState()
    {
        
    }
    public IPlayerState CheckSwitchStates()
    {
        if(player.inputHandler.inputBuffer.Move.sqrMagnitude>0) return player.MoveState;
        return null;
    }
}
