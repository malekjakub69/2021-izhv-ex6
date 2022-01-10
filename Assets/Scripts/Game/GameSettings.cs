using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ReSharper disable InvalidXmlDocComment

/// <summary>
/// Container for settings used in the game.
/// </summary>
public class GameSettings : MonoBehaviour
{
#region Editor

    // References to GameObjects within the scene.
    [ Header("Game Objects") ]

    [Tooltip("Main camera used for viewing the scene.")]
    public Camera mainCamera;
    
    [Header("Control Settings")]
    
    // Game Settings
    [Header("Game Settings")] 
    
    // World Settings
    [ Header("World Settings") ]
    
    /// <summary>
    /// Mask for the objects in the ground layer.
    /// </summary>
    public LayerMask groundLayer;

#endregion // Editor

#region Internal

    /// <summary> Currently used GameManager component. </summary>
    [CanBeNull] 
    private GameManager mGameManager;
    
    /// <summary> Currently used SoundManager component. </summary>
    [CanBeNull] 
    private SoundManager mSoundManager;
    
    /// <summary> Currently used UIManager component. </summary>
    [CanBeNull] 
    private UIManager mUIManager;
    
    /// <summary> Singleton instance of the Settings. </summary>
    private static GameSettings sInstance;
    
    /// <summary> Getter for the singleton Settings object. </summary>
    public static GameSettings Instance
    { get { return sInstance; } }
    
#endregion // Internal

#region Interface

    public GameManager gameManager => mGameManager;
    public SoundManager soundManager => mSoundManager;
    public UIManager uiManager => mUIManager;

#endregion // Interface
    
    /// <summary> Called when the script instance is first loaded. </summary>
    private void Awake()
    {
        // Initialize the singleton instance, if no other exists.
        if (sInstance != null && sInstance != this)
        { Destroy(gameObject); }
        else
        { sInstance = this; }
    }

    /// <summary> Called before the first frame update. </summary>
    void Start()
    {
        mGameManager = GameManager.Instance;
        mSoundManager = SoundManager.Instance;
        mUIManager = UIManager.Instance;
    }
}
