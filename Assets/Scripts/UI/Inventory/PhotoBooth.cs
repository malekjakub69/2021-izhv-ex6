using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

/// <summary> Simple photo booth used for the item previews. </summary>
public class PhotoBooth : MonoBehaviour
{
    [Header("PhotoBooth Settings")] 
    [Tooltip("Prefab used for the booth.")]
    public GameObject boothPrefab;
    [Tooltip("Base size of the prefab.")]
    public float boothPrefabBaseSize = 10.0f;
    [Tooltip("Color of the booth backdrop.")]
    public Color boothBackdropColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
    [Tooltip("Color of the booth box.")]
    public Color boothBoxColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
    [Tooltip("Size of the booth.")]
    public float boothSize = 10.0f;

    [Header("Rendering Settings")] 
    [Tooltip("Resolution of the output texture.")]
    public Vector2Int renderResolution = new Vector2Int(256, 256);
    [Tooltip("Color depth of the output texture.")]
    public int renderColorDepth = 16;
    [Tooltip("Color format used in the texture.")]
    public RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32;

    /// <summary> Currently used photo booth instance. May be empty. </summary>
    [CanBeNull] private GameObject mPhotoBooth;
    /// <summary> Game Object currently being displayed in the photo booth. </summary>
    [CanBeNull] private GameObject mDisplayedGO;
    /// <summary> Render texture used to capture the visual output of the booth's camera. </summary>
    [CanBeNull] private RenderTexture mRenderTexture;
    /// <summary> Did we create the target render texture (true) or is it from an external source (false)? </summary>
    private bool mRenderTextureCreated;
    
    /// <summary> Reference to the currently displayed Game Object. </summary>
    public GameObject displayedGO => mDisplayedGO;

    /// <summary> Called when the script instance is first loaded. </summary>
    void Awake()
    { }

    /// <summary> Called before the first frame update. </summary>
    void Start()
    { }

    /// <summary> Update called once per frame. </summary>
    void Update()
    { }

    /// <summary> Cleanup resources and destroy. </summary>
    void OnDestroy()
    {
        if (mRenderTexture != null && mRenderTextureCreated)
        { mRenderTexture.Release(); }
    }

    /// <summary> Make sure we have a valid photo booth instantiated. </summary>
    private void EnsurePhotoBooth()
    { if (mPhotoBooth == null) { mPhotoBooth = Instantiate(boothPrefab, transform); } }
    
    /// <summary> Make sure we have a valid rendering texture. </summary>
    private void EnsureRenderTexture([CanBeNull] RenderTexture texture = null)
    {
        if (texture != null)
        {
            if (mRenderTexture != null && mRenderTextureCreated)
            { mRenderTexture.Release(); }
            
            mRenderTexture = texture;
            mRenderTextureCreated = false;
        }
        else if (mRenderTexture == null)
        {
            mRenderTexture = new RenderTexture(
                renderResolution.x, renderResolution.y, 
                renderColorDepth, renderTextureFormat
            );
            mRenderTextureCreated = true;
        }
    }

    /// <summary> Setup photo booth at the current position. Reuses current instance if possible. </summary>
    public void SetupPhotoBooth()
    {
        EnsurePhotoBooth();
        
        ConfigureBoothBackdrop();
        ConfigureBoothBox();
    }
    
    /// <summary> Tear down the photo booth, cleaning up any resources. </summary>
    public void TeardownPhotoBooth()
    { Destroy(mDisplayedGO); Destroy(mPhotoBooth); }

    /// <summary> Set parameters of the photo booth backdrop. Automatically creates the photo booth.</summary>
    private void ConfigureBoothBackdrop()
    {
        EnsurePhotoBooth();

        var backdrop = Util.Common.GetChildByName(mPhotoBooth, "BoothBackdrop");

        var finalBackdropScale = boothSize / boothPrefabBaseSize;
        backdrop.transform.localScale = new Vector3(
            finalBackdropScale, finalBackdropScale, finalBackdropScale
        );
        
        var backdropRenderer = backdrop.GetComponent<Renderer>();
        backdropRenderer.material.SetColor("_Color", boothBackdropColor);
    }
    
    /// <summary> Set parameters of the photo booth box. Automatically creates the photo booth.</summary>
    private void ConfigureBoothBox()
    {
        EnsurePhotoBooth();

        var box = Util.Common.GetChildByName(mPhotoBooth, "BoothBox");
        
        var finalBoxScale = boothSize / boothPrefabBaseSize;
        box.transform.localScale = new Vector3(
            finalBoxScale, finalBoxScale, finalBoxScale
        );
        
        var boxRenderer = box.GetComponent<Renderer>();
        boxRenderer.material.SetColor("_Color", boothBoxColor);
    }

    /// <summary> Display an instance of given prefab within the photo booth. </summary>
    /// <param name="prefab">Game Object to instantiate and display. Use null to cleanup the instance.</param>
    /// <param name="output">Optional output texture. Keep null to use the internal RenderTexture.</param>
    /// <returns>Returns reference to the target render texture.</returns>
    public RenderTexture DisplayItem([CanBeNull] GameObject prefab = null, [CanBeNull] RenderTexture output = null)
    {
        ConfigureCamera(output: output);

        if (mDisplayedGO != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(mDisplayedGO);
#else // !UNITY_EDITOR
            Destroy(mDisplayedGO);
#endif // !UNITY_EDITOR
        }

        if (prefab != null)
        {
            var pedestal = Util.Common.GetChildByName(mPhotoBooth, "BoothPedestal");
            mDisplayedGO = Instantiate(prefab, pedestal.transform);
            
            // Remove all unnecessary components.
            // TODO - This is a little bit of a hack, but it allows us to use a single prefab for everything.
            foreach (var component in mDisplayedGO.GetComponents<Component>())
            {
                if (component.GetType() != typeof(UnityEngine.Transform) &&
                    component.GetType() != typeof(UnityEngine.MeshFilter) &&
                    component.GetType() != typeof(UnityEngine.MeshRenderer))
                { DestroyImmediate(component); }
            }
        }
        
        return mRenderTexture;
    }

    /// <summary> Configure the camera and output texture. </summary>
    /// <param name="output">Optional output texture. Keep null to use the internal RenderTexture. </param>
    private void ConfigureCamera([CanBeNull] RenderTexture output = null)
    {
        EnsurePhotoBooth();
        EnsureRenderTexture(output);
        
        var camera = Util.Common.GetChildByName(mPhotoBooth, "BoothCamera");

        var cameraPosition = camera.transform.position;
        cameraPosition.z = boothSize / 2.0f - 0.01f;
        
        var cameraComponent = camera.GetComponent<Camera>();
        cameraComponent.orthographicSize = boothSize / 2.0f;
        cameraComponent.farClipPlane = boothSize;
        cameraComponent.targetTexture = mRenderTexture;
    }
}
