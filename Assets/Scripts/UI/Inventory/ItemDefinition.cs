using System;
using UnityEngine;

/// <summary> Data representation of an inventory item. </summary>
[CreateAssetMenu(fileName="New Inventory Item", menuName="Data/Inventory/Item")]
public class ItemDefinition : ScriptableObject
{
    [Tooltip("Unique identifier of this item.")]
    public string id = Guid.NewGuid().ToString();
    [Tooltip("Human readable name.")]
    public string readableName;
    [Tooltip("Human readable description.")]
    public string readableDescription;
    [Tooltip("Cost of the item.")]
    public int cost;
    [Tooltip("Prefab representing the scene object.")]
    public GameObject prefab;
}
