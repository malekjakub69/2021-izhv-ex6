using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Simple behavior which plays a sound on collision.
/// </summary>
public class CollisionSound : MonoBehaviour
{
    [Header("Sound Definition")]
    [Tooltip("List of collision sound alternatives.")]
    public SoundEffect[] effects = new SoundEffect[]
    {
        new SoundEffect()
    };

    /// <summary> Audio source shared by all effects. </summary>
    private AudioSource mAudioSource;
    
    /// <summary> Initialize the sound effects. </summary>
    void Awake()
    {
        // Create a shared AudioSource common to all effects.
        mAudioSource = gameObject.AddComponent<AudioSource>();
    }
    
    /// <summary> Called when collision occurs. </summary>
    private void OnCollisionEnter(Collision other)
    {
        // Choose a random collision sound and initialize it.
        var effect = effects[Random.Range(0, effects.Length)];
        effect.Initialize(mAudioSource);
        
        // Play the sound.
        effect.audioSource.Play();
    }
}
