using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

/// <summary> Grid of photo booths for inventory previews. </summary>
public class PreviewGrid : MonoBehaviour
{
    [Header("PhotoBooth Settings")] 
    [Tooltip("Prefab used for the booth seeds.")]
    public GameObject boothSeedPrefab;

    [Header("Grid Settings")] 
    [Tooltip("Number of photo booths within the grid.")]
    public Vector2Int maximumPreviewCount = new Vector2Int(10, 2);
    
    [Header("Preview Settings")] 
    [Tooltip("Scale of the displayed item")]
    public float previewScale = 1.0f;
    [Tooltip("Attempt to automatically scale the displayed item?")]
    public bool previewAutoScale = true;
    [Tooltip("Multiplier for the highest possible scale found by auto scale.")]
    public float previewAutoScaleMultiplier = 0.9f;
    [Tooltip("Rotate the previewed item?")]
    public bool previewRotate = true;
    [Tooltip("Speed of preview rotation")]
    public float previewRotateSpeed = 1.0f;
    [Tooltip("Euler angles for the preview rotation.")]
    public Vector3 previewRotateDirection = Vector3.right;

    /// <summary> Currently used list of preview photo booths. </summary>
    private GameObject[] mPhotoBoothGrid;

    /// <summary> Size of the photo booth. </summary>
    public float boothSize => boothSeedPrefab.GetComponent<PhotoBooth>().boothSize;
    
    /// <summary> Called when the script instance is first loaded. </summary>
    void Awake()
    { }

    /// <summary> Called before the first frame update. </summary>
    void Start()
    { }

    /// <summary> Update called once per frame. </summary>
    void Update()
    { }

    /// <summary> Setup the preview grid. </summary>
    private void EnsureGrid()
    {
        var totalPreviewCount = maximumPreviewCount.x * maximumPreviewCount.y;
        if (mPhotoBoothGrid != null && mPhotoBoothGrid.Length == totalPreviewCount)
        { return; }
            
        var seedPhotoBooth = boothSeedPrefab.GetComponent<PhotoBooth>();
        var singleBoothSize = seedPhotoBooth.boothSize;

        if (mPhotoBoothGrid != null && mPhotoBoothGrid.Length > 0)
        { foreach (var photoBooth in mPhotoBoothGrid) { DestroyImmediate(photoBooth); } }

        mPhotoBoothGrid = new GameObject[totalPreviewCount];
        for (var iii = 0; iii < totalPreviewCount; ++iii)
        {
            mPhotoBoothGrid[iii] = Instantiate(boothSeedPrefab,
                transform.position + new Vector3(
                    singleBoothSize * iii + singleBoothSize / 2.0f, 0.0f, 0.0f
                ), 
                Quaternion.identity, transform
            );
        }
    }

    /// <summary> Display an instance of given prefab within the target preview. </summary>
    /// <returns>Returns reference to the target render texture.</returns>
    public RenderTexture DisplayItem(Vector2Int previewIdx, [CanBeNull] GameObject prefab = null)
    {
        EnsureGrid();

        var flatPreviewIdx = maximumPreviewCount.x * previewIdx.y + previewIdx.x;
        var photoBooth = mPhotoBoothGrid[flatPreviewIdx].GetComponent<PhotoBooth>();
        
        var renderTexture = photoBooth.DisplayItem(prefab);

        var displayedItem = photoBooth.displayedGO;
        if (displayedItem)
        { // Only modify the item if we are actually displaying something.
            if (previewAutoScale)
            { // Auto-detect the largest possible scale.
                var meshBounds = displayedItem.GetComponent<MeshFilter>().mesh.bounds;
                var maxScale = 1.0f / meshBounds.extents.magnitude;
                displayedItem.transform.localScale = new Vector3(
                    maxScale, maxScale, maxScale
                ) * boothSize / 2.0f * previewAutoScaleMultiplier;
            }
            displayedItem.transform.localScale *= previewScale;

            if (previewRotate)
            { // Add auto-rotator component.
                var rotator = displayedItem.AddComponent<Rotator>();
                rotator.speed = previewRotateSpeed;
                rotator.direction = previewRotateDirection;
            }
        }

        return renderTexture;
    }
}
