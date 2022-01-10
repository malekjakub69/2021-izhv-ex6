using System;
using UnityEngine;

/// <summary>
/// Helper script used for managing camera letterbox/pillarbox.
/// </summary>
[ExecuteInEditMode]
public class CameraHelper : MonoBehaviour
{
#region Editor

    /// <summary>
    /// Target camera size in units.
    /// </summary>
    [Header("Global")]
    public Vector2 targetResolution = new Vector2(4.0f, 4.0f);

    /// <summary>
    /// How fast should the zooming work?
    /// </summary>
    public float zoomSpeed = 0.1f;

    /// <summary>
    /// Set a GameObject to follow.
    /// </summary>
    public GameObject followTarget;
    
    /// <summary>
    /// Follow the target GameObject?
    /// </summary>
    public bool doFollowTarget = false;
    
#endregion // Editor

#region Internal
    
    /// <summary>
    /// The managed camera component.
    /// </summary>
    private Camera mCamera;

    /// <summary>
    /// Current resolution we are working with.
    /// </summary>
    private Vector2 mResolution;
    
    /// <summary>
    /// Current target we are working with.
    /// </summary>
    private Vector2 mTarget;
    
#endregion // Internal
    
    /// <summary>
    /// Called before the first frame update.
    /// </summary>
    void Start()
    { mCamera = GetComponent<Camera>(); }

    /// <summary>
    /// Update called once per frame.
    /// </summary>
    void Update()
    {
        // Fit the camera to the target resolution, if necessary.
        FitTargetResolution(targetResolution);
        // Follow the target, if enabled.
        if (doFollowTarget)
        { FollowTarget(followTarget); }
    }

    public void FitTargetResolution(Vector2 target)
    {
        // Update is only needed when the resolution is changed.
        Vector2 currentResolution = new Vector2((float)Screen.width, (float)Screen.height);
        if (mTarget.Equals(target) && mResolution.Equals(currentResolution))
        { return; }

        // Set the extent of size we want to use.
        var cameraSize = Math.Max(target.x, target.y);
        mCamera.orthographicSize = cameraSize;
        
        // Calculate the current aspect ratio of the screen and the requested target.
        var currentAspectRatio = (float)Screen.width / Screen.height;
        var targetAspectRatio = target.x / target.y;
        // How much of a letterbox do we need?
        var letterboxRatio = currentAspectRatio / targetAspectRatio;

        // Prepare letterbox-ed rectangle for the camera.
        var cameraRect = new Rect();
        if (letterboxRatio >= 1.0f)
        { // The screen is too wide -> Vertical letterbox.
            var letterboxWidth = 1.0f / letterboxRatio;
            cameraRect.x = (1.0f - letterboxWidth) / 2.0f;
            cameraRect.y = 0.0f;
            cameraRect.width = letterboxWidth;
            cameraRect.height = 1.0f;
        }
        else
        { // The screen is too high -> Horizontal letterbox.
            var letterboxHeight = letterboxRatio;
            cameraRect.x = 0.0f;
            cameraRect.y = (1.0f - letterboxHeight) / 2.0f;
            cameraRect.width = 1.0f;
            cameraRect.height = letterboxHeight;
        }

        // Update the camera to include our new letterbox.
        mCamera.rect = cameraRect;
        mResolution = currentResolution;
        mTarget = target;
    }

    public void FollowTarget(GameObject target)
    {
        // Safety check...
        if (target == null)
        { return; }
        
        // Move the camera to be above the target's location.
        var currentPosition = transform.position;
        var targetPosition = target.transform.position;
        transform.position = new Vector3{
            x = targetPosition.x, 
            y = currentPosition.y, 
            z = targetPosition.z
        };
    }

    /// <summary>
    /// Zoom the current view by given magnitude.
    /// </summary>
    /// <param name="magnitude">By how much should the view change.</param>
    public void ZoomView(float magnitude)
    {
        // Clamp the values to be at least 1.0f.
        targetResolution.x = Math.Max(targetResolution.x + magnitude * zoomSpeed, 1.0f);
        targetResolution.y = Math.Max(targetResolution.y + magnitude * zoomSpeed, 1.0f);
    }
}
