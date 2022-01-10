using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Util;

/// <summary>
/// The main Player script.
/// </summary>
public class Player : MonoBehaviour
{
#region Editor
    
#endregion // Editor

#region Internal
    
#endregion // Internal
    
    /// <summary> Called when the script instance is first loaded. </summary>
    private void Awake()
    { }

    /// <summary> Called before the first frame update. </summary>
    void Start()
    { }
    
    // Input System Callbacks: 
    public void OnPause(InputValue value)
    { if (value.isPressed) { GameManager.Instance.PauseGame(); } }
    public void OnToggleDevUI(InputValue value)
    { if (value.isPressed) { UIManager.Instance.ToggleDevUI(); } }
    public void OnToggleDebugUI(InputValue value)
    { if (value.isPressed) { UIManager.Instance.ToggleDebugUI(); } }

    /// <summary> Update called once per frame. </summary>
    void Update()
    { }

    /// <summary> Update called at fixed time delta. </summary>
    void FixedUpdate()
    { }
}
