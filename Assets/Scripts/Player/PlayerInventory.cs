using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

/// <summary> Item stored in the PlayerInventory. </summary>
[Serializable]
public class InventoryItem
{
    /// <summary> Definition of the item. </summary>
    public ItemDefinition definition;
    /// <summary> Visualization representing this item. </summary>
    public ItemVisual visual;
}

/// <summary> Behaviour allowing the player to have an inventory of items. </summary>
public class PlayerInventory : MonoBehaviour
{
    [ Header("Game Objects") ]
    [Tooltip("Current content of the inventory.")]
    public List<InventoryItem> inventoryItems = new List<InventoryItem>();
    [Tooltip("Usable size of the inventory. Use zero to make all slots available.")]
    public Vector2Int inventoryDimension;
    
    /// <summary> Called when the script instance is first loaded. </summary>
    void Awake()
    { }

    /// <summary> Called before the first frame update. </summary>
    void Start()
    { }

    /// <summary> Update called once per frame. </summary>
    void Update()
    { }
    
    /// <summary> Look through the inventory for a position to place a new item. </summary>
    private async Task<bool> FindPositionForItem(VisualElement newItem)
    {
        var inventoryManager = InventoryManager.Instance;
        var slotDimension = inventoryManager.slotDimension;
        
        for (var yyy = 0; yyy < inventoryDimension.y; ++yyy)
        { 
            for (var xxx = 0; xxx < inventoryDimension.x; ++xxx)
            { // Search through existing items for an empty slot.
                
                // Try placing the item at a position overlapping with the item slot.
                var testPosition = new Vector2(
                    slotDimension.x * xxx, 
                    slotDimension.y * yyy
                );
                SetItemFloatingPosition(newItem, testPosition);
                
                await UniTask.WaitForEndOfFrame();
                
                // Check if we are overlapping with any existing items.
                var overlappingItem = inventoryItems.FirstOrDefault(s => 
                    s.visual != null && 
                    s.visual.layout.Overlaps(newItem.layout)
                );
                
                if (overlappingItem == null)
                { // No items found, we can place it here.
                    return true;
                }
            }
        }

        return false;
    }
    
    /// <summary> Load inventory content and initialize. </summary>
    public async void LoadInventory()
    {
        var inventoryManager = InventoryManager.Instance;
        await UniTask.WaitUntil(() => inventoryManager.inventoryReady);

        if (inventoryDimension.magnitude == 0)
        { inventoryDimension = inventoryManager.gridDimension; }
        
        foreach (var item in inventoryItems)
        {
            // Add the visual representation to the inventory grid.
            var itemVisual = new ItemVisual(item.definition);
            inventoryManager.AddItemToGrid(itemVisual);
            
            // Try to find a place for the item.
            var inventoryHasSpace = await FindPositionForItem(itemVisual);
            if (!inventoryHasSpace)
            { // No place found...
                Debug.Log("Unable to place item into inventory, no space available!");
                inventoryManager.RemoveItemFromGrid(itemVisual);
                continue;
            }
            
            // Place the item within the grid and create a preview.
            itemVisual.SetGridPosition(inventoryManager.GetItemGridPosition(itemVisual));
            
            // Associate the item with its visual representation.
            ConfigureInventoryItem(item, itemVisual);
        }
    }

    /// <summary> Unload the current inventory from the scene. </summary>
    public void UnloadInventory()
    { InventoryManager.Instance.RemoveAllItemsFromGrid(); }
    
    /// <summary> Place an item at given position. </summary>
    private void SetItemFloatingPosition(VisualElement item, Vector2 position)
    {
        item.style.left = position.x;
        item.style.top = position.y;
    }
    
    /// <summary> Configure item with its visual component. </summary>
    private void ConfigureInventoryItem(InventoryItem item, ItemVisual visual)
    {
        item.visual = visual;
        //visual.style.visibility = Visibility.Visible;
    }
}
