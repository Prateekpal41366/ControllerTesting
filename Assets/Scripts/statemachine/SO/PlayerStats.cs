using UnityEngine;

[CreateAssetMenu(fileName = "NewStats", menuName = "Data/Stats")]
public class PlayerStats : ScriptableObject
{
    public float maxHealth = 100;
    public float currentHealth;
    public float moveSpeed = 20f;
    public float airSpeed=10f;
    public float airDrag=0.2f;
    public float airLift=10f;
    public float strafeSpeed=7f;
    public float jumpStr = 15f;
    public float flapStr=10f;

    //maybe

    // Helper to reset stats on game start
    public void Initialize() => currentHealth = maxHealth;
}