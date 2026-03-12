using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewInventory", menuName = "Data/Inventory")]
public class PlayerInventory : ScriptableObject
{
    public List<string> itemIDs = new List<string>();

    public void AddItem(string id) => itemIDs.Add(id);
}