using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary> Primary user interface manager. </summary>
public class UIManager : MonoBehaviour
{
#region Editor

    // References to GameObjects within the scene.
    [ Header("Game Objects") ]

    [Tooltip("Root of the developer UI.")]
    public GameObject devUIGO;
    
    [Tooltip("Root of the debug menu UI.")]
    public GameObject debugMenuGO;
    
    [Tooltip("Root of the Main Menu UI.")]
    public GameObject mainMenuGO;
    
    [Tooltip("Reference to the current player.")]
    public GameObject playerGO;
    
#endregion // Editor

#region Internal

    /// <summary> Reference to the main debug UI component. </summary>
    [CanBeNull] private DebugMenuUI mDebugMenuUI;

    /// <summary> Currently used InventoryManager component. </summary>
    [CanBeNull] private InventoryManager mInventoryManager;
    
    /// <summary> Currently used main menu canvas group. </summary>
    [CanBeNull] private CanvasGroup mMainMenu;
    
    /// <summary> Current main player inventory. </summary>
    [CanBeNull] private PlayerInventory mPlayerInventory;
    
    /// <summary> Singleton instance of the UIManager. </summary>
    private static UIManager sInstance;
    
    /// <summary> Getter for the singleton UIManager object. </summary>
    public static UIManager Instance
    { get { return sInstance; } }
    
#endregion // Internal

#region Interface

    public InventoryManager inventoryManager => mInventoryManager;

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
        mDebugMenuUI = debugMenuGO.GetComponent<DebugMenuUI>();
        mInventoryManager = InventoryManager.Instance;
        mMainMenu = mainMenuGO.GetComponent<CanvasGroup>();
        mPlayerInventory = playerGO.GetComponent<PlayerInventory>();
    }

    /// <summary> Update called once per frame. </summary>
    void Update()
    {
    }

    /// <summary> Toggle the development User Interface. </summary>
    public void ToggleDevUI()
    { devUIGO.SetActive(!devUIGO.activeSelf); }

    /// <summary> Toggle the debug User Interface. </summary>
    public void ToggleDebugUI()
    { mDebugMenuUI.displayUI = !mDebugMenuUI.displayUI; }

    /// <summary> Toggle visibility of the main menu. </summary>
    public void ToggleMainMenu()
    { mainMenuGO.SetActive(!mainMenuGO.activeSelf); }

    /// <summary> Setup UI when we start the game. </summary>
    public void OnStartGame()
    {
        ToggleMainMenu();
        inventoryManager.DisplayInventory(); 
        mPlayerInventory.LoadInventory();
    }
    
    /// <summary> Setup UI when we pause the game. </summary>
    public void OnPauseGame()
    {
        inventoryManager.HideInventory(); 
        ToggleMainMenu();
    }
    
    /// <summary> Setup UI when we pause the game. </summary>
    public void OnUnPauseGame()
    {
        inventoryManager.DisplayInventory(); 
        ToggleMainMenu();
    }
}
