using System.Collections.Generic;
using Cysharp.Threading.Tasks.Triggers;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The main game manager GameObject.
/// </summary>
public class GameManager : MonoBehaviour
{
#region Editor

    // References to GameObjects within the scene.
    [ Header("Game Objects") ]

    [Tooltip("Player character")]
    public GameObject playerCharacter;
    
#endregion // Editor

#region Internal

    /// <summary> Did we start the game? </summary>
    private static bool sGameStarted = false;
    
    /// <summary> Is the game currently paused? </summary>
    private static bool sGamePaused = true;
    
    /// <summary> Singleton instance of the GameManager. </summary>
    private static GameManager sInstance;
    
    /// <summary> Getter for the singleton GameManager object. </summary>
    public static GameManager Instance
    { get { return sInstance; } }
    
#endregion // Internal

#region Interface

    /// <summary> Enable/disable the mouse interaction mode. </summary>
    public bool interactiveMode
    {
        get 
        { return playerCharacter.GetComponent<InputManager>().interact; }
        set 
        { playerCharacter.GetComponent<InputManager>().interact = value; }
    }

#endregion // Internal

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
        // Setup the game scene.
        SetupGame();
    }

    /// <summary>
    /// Update called once per frame.
    /// </summary>
    void Update()
    { }

    /// <summary> Setup the game scene. </summary>
    public void SetupGame()
    {
        // We have not started yet.
        sGameStarted = false;
        
        // Mute all sounds until we start the game.
        SoundManager.Instance.masterMuted = true;
        
        // Disable the player character.
        EnablePlayerCharacter(false);
    }

    /// <summary> Set the game to the "started" state. </summary>
    public void StartGame()
    {
        // If we already started the game, just assume we want to pause/unpause.
        if (sGameStarted)
        { PauseGame(); return; }

        // Set the game as running.
        sGameStarted = true; 
        sGamePaused = false;
        
        // Make the sounds audible.
        SoundManager.Instance.masterMuted = false;
        
        // Broadcast the "StartGame" message to all managers.
        transform.parent.BroadcastMessage("OnStartGame");
    }
    
    /// <summary> Is the player character enabled? </summary>
    public bool PlayerCharacterEnabled()
    { return playerCharacter.GetComponent<CharacterSelector>().characterEnabled; }

    /// <summary> Enable/disable the player character. </summary>
    public void EnablePlayerCharacter(bool enabled)
    {
        playerCharacter.GetComponent<Character3DController>().enabled = enabled;
        playerCharacter.GetComponent<Character3DMovement>().enabled = enabled;
        playerCharacter.GetComponent<CharacterSelector>().characterEnabled = enabled;
    }
    
    /// <summary> Enable/disable the player character. </summary>
    public void TogglePlayerCharacter()
    { EnablePlayerCharacter(!PlayerCharacterEnabled()); }
    
    /// <summary> Set the game to the "started" state. </summary>
    public void PauseGame()
    {
        if (!sGameStarted)
        { return; }

        if (sGamePaused)
        { // Game is paused -> Unpause it.
            // Broadcast the "UnPauseGame" message to all managers.
            transform.parent.BroadcastMessage("OnUnPauseGame");
            sGamePaused = false;
        }
        else
        { // Game is not paused -> Pause it.
            // Broadcast the "PauseGame" message to all managers.
            transform.parent.BroadcastMessage("OnPauseGame");
            sGamePaused = true;
        }
    }
    
    /// <summary> Reset the game to the default state. </summary>
    public void ResetGame()
    {
        // Reload the active scene, triggering reset...
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary> Quit the game. </summary>
    public void QuitGame()
    {
        /*
         * Task 1: Quit the game
         *
         * Unity has no unified way of quitting a game on all platforms.
         * For this reason, you have already prepared sections for the
         * three primary platforms we are working with.
         *
         * You should be able to easily solve this task with a little
         * bit of "Googling". Just for a little hint: you can use the 
         * static objects and methods within UnityEditor and Application.
		 * 
		 * Hint: For WebGL build, you may not be able to actually close 
		 *       the browser tab itself. That is OK. Try "unloading" the 
		 *       WebGL memory or just refreshing the page by reloading the 
		 *       current URL ("OpenURL", "absoluteURL").
         */
         
        Application.Quit();
        
#if UNITY_EDITOR
        // Quitting in Unity Editor: 
#elif UNITY_WEBPLAYER || UNITY_WEBGL
        // Quitting in the WebGL build: 
#else // !UNITY_WEBPLAYER
        // Quitting in all other builds: 
#endif
    }
}
