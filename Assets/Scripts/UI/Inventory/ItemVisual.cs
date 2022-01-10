using UnityEngine;
using UnityEngine.UIElements;

/// <summary> Visual representation of an item within the inventory. </summary>
public class ItemVisual : VisualElement
{
    /// <summary> Target item being visualized. </summary>
    private ItemDefinition mTargetItem;

    /// <summary> Has this item already been placed in the item grid? </summary>
    private bool mPlacedInGrid = false;
    /// <summary> Original grid position of the visual before moving it. </summary>
    private Vector2Int mOriginalGridPosition;
    
    /// <summary> Original position of the visual before moving it. </summary>
    private Vector2 mOriginalPosition;
    /// <summary> Are we currently dragging this item? </summary>
    private bool mDragging;
    /// <summary> Storage for the result of attempted placement of this item. </summary>
    private (bool canPlace, Vector2 position) mPlacementResult;

    /// <summary> Reference to the element displaying the preview image. </summary>
    private VisualElement mPreview;

    /// <summary> Definition of the represented item. </summary>
    public ItemDefinition definition => mTargetItem;
    
    /// <summary> Create visualization for given item. </summary>
    public ItemVisual(ItemDefinition item)
    { SetStyle(item); }
    
    /// <summary> Clean up and destroy. </summary>
    ~ItemVisual()
    {
        UnregisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
        UnregisterCallback<MouseUpEvent>(OnMouseUpEvent);
    }

    /// <summary> Setup style for given item. </summary>
    public void SetStyle(ItemDefinition item)
    {
        mTargetItem = item;
        
        name = $"{item.readableName}";

        var slotDimension = GameSettings.Instance.uiManager.inventoryManager.slotDimension;
        style.width = slotDimension.x;
        style.height = slotDimension.y;
        /*
        var shouldBeVisible = InventoryManager.Instance.InventoryVisible();
        style.visibility = shouldBeVisible ? Visibility.Visible : Visibility.Hidden;
        mPreview.style.visibility =  shouldBeVisible ? Visibility.Visible : Visibility.Hidden;
        */
        
        mPreview = new VisualElement();
        Add(mPreview);
        mPreview.AddToClassList("preview_visual");
        
        AddToClassList("preview_visual_container");
        
        
        RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
        RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
        RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
    }
    
    /// <summary> Setup position within the inventory grid. </summary>
    public void SetGridPosition(Vector2Int position)
    {
        // Cleanup the old preview if we used it.
        if (mPlacedInGrid)
        { InventoryManager.Instance.previewGrid.DisplayItem(position, null); }
        
        // Create a new preview.
        mPreview.style.backgroundImage = Background.FromRenderTexture(
            InventoryManager.Instance.previewGrid.DisplayItem(position, mTargetItem.prefab)
        );
        mOriginalGridPosition = position;
        mPlacedInGrid = true;
    }
    
    /// <summary> Setup position within the inventory grid. </summary>
    public void SetFloatingPosition(Vector2 position)
    {
        style.left = position.x;
        style.top = position.y;
    }
    
    /// <summary> Get the current floating position. </summary>
    public Vector2 GetFloatingPosition()
    { return new Vector2(style.left.value.value, style.top.value.value); }
    
    /// <summary> Callback triggered when mouse button is pressed.</summary>
    private void OnMouseDownEvent(MouseDownEvent mouseEvent)
    {
        if (!mDragging)
        { StartDrag(); }
    }
    
    /// <summary> Callback triggered when mouse button is released.</summary>
    private void OnMouseUpEvent(MouseUpEvent mouseEvent)
    {
        if (!mDragging)
        { return; }
        
        mDragging = false;
        if (mPlacementResult.canPlace)
        { // We released the mouse button and can place -> Place it into the selected slot.
            SetFloatingPosition(new Vector2(
                mPlacementResult.position.x - parent.worldBound.position.x,
                mPlacementResult.position.y - parent.worldBound.position.y)
            );
        }
        else
        { // Released, but cannot place -> Return it to the original position.
            SetFloatingPosition(new Vector2(mOriginalPosition.x, mOriginalPosition.y));
        }
        
        // Update the item details.
        InventoryManager.Instance.UpdateSelectedItem(this);
         
        // Make the selection visible to the user.
        InventoryManager.Instance.MoveHighlightToTop();
        
        // Finally, make sure we update the grid position.
        SetGridPosition(InventoryManager.Instance.GetItemGridPosition(GetFloatingPosition()));
    }
    
    /// <summary> Called when a new drag & drop operation is started. </summary>
    public void StartDrag()
    {
        mDragging = true;
        mOriginalPosition = worldBound.position - parent.worldBound.position;
        InventoryManager.Instance.ShowPlacementTarget(this);
        BringToFront();
    }
    
    /// <summary> Callback triggered when the mouse cursor is moved. </summary>
    /// <param name="mouseEvent"></param>
    private void OnMouseMoveEvent(MouseMoveEvent mouseEvent)
    {
        if (!mDragging) 
        { return; }
        
        SetFloatingPosition(GetMousePosition(mouseEvent.mousePosition));
        mPlacementResult = InventoryManager.Instance.ShowPlacementTarget(this);
    }

    /// <summary> Get local mouse position based on the mouse cursor position. </summary>
    private Vector2 GetMousePosition(Vector2 mousePosition)
    {
        return new Vector2(
            mousePosition.x - (layout.width / 2) - parent.worldBound.position.x, 
            mousePosition.y - (layout.height / 2) - parent.worldBound.position.y
        );
    }
}
