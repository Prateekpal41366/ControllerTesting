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
        player.transform.position+=
            new Vector3(
                player.inputHandler.inputBuffer.Move.x,
                0,
                player.inputHandler.inputBuffer.Move.y)
            *25f*Time.deltaTime;
    }
    public void ExitState()
    {
        
    }
    public IPlayerState CheckSwitchStates()
    {
        if (player.inputHandler.inputBuffer.Move.sqrMagnitude<math.EPSILON) return player.IdleState;
        return null;
    }
}
