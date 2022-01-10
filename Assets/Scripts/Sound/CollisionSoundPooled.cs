using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Behavior using the primary sound manager to play collisions sounds from a pool of audio sources.
/// </summary>
public class CollisionSoundPooled : MonoBehaviour
{
    [Header("Sound Definition")] 
    [Tooltip("List of collision sound identifiers for all available alternatives.")]
    public string[] effects;
    [Tooltip("Use the PlayPooledSound instead of simple PlaySound?")]
    public bool usePlayPooled = true;

    /// <summary> Initialize the sound effects. </summary>
    void Awake()
    { }
    
    /// <summary> Called when collision occurs. </summary>
    private void OnCollisionEnter(Collision other)
    {
        // Choose a random collision sound identifier.
        var effect = effects[Random.Range(0, effects.Length)];
        
        // Play the sound from the current position.
        if (usePlayPooled)
        { GameSettings.Instance.soundManager.PlayPooledSound(effect, gameObject.transform.position); }
        else
        { GameSettings.Instance.soundManager.PlaySound(effect, gameObject.transform.position); }
    }
}
