using System;
using JetBrains.Annotations;
using Unity.Audio;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Simple sound clip wrapper representing a single sound effect.
/// </summary>
[Serializable]
public class SoundEffect
{
    [Header("Data")] 
    [Tooltip("Unique identifier of this sound.")]
    public string identifier;
    [Tooltip("Audio clip associated with this sound effect.")]
    public AudioClip clip;

    [Header("Properties")]
    [Tooltip("Volume to play the effect at.")]
    [Range(0.0f, 1.0f)]
    public float volume = 1.0f;
    [Tooltip("Pitch of the sound effect.")]
    [Range(0.01f, 5.0f)]
    public float pitch = 1.0f;
    [Tooltip("Loop the sound effect?")]
    public bool loop = false;
    
    [Header("Mixing")]
    [Tooltip("Mixer group used for audio playback.")]
    public AudioMixerGroup mixerGroup;

    public AudioSource audioSource
    { get => mSource; }

    /// <summary> Internal audio source used for playing this sound effect. </summary>
    private AudioSource mSource;
    
    /// <summary> Internal GameObject used when no other is provided to the Initialize. </summary>
    private GameObject mGO;

    /// <summary> Cleanup and destroy. </summary>
    ~SoundEffect()
    { Deinitialize(); }

    /// <summary>
    /// Initialize this sound effect, creating AudioSource on the target object.
    /// </summary>
    /// <param name="target">Target GameObject to create audio source in.</param>
    public void Initialize([CanBeNull] GameObject target)
    {
        if (!target)
        { mGO = new GameObject(); target = mGO; }
        
        Initialize(target.AddComponent<AudioSource>());
    }
    
    /// <summary>
    /// Initialize this sound effect, using pre-existing AudioSource.
    /// </summary>
    /// <param name="target">Target AudioSource to use.</param>
    public AudioSource Initialize(AudioSource target)
    {
        mSource = target;
        mSource.playOnAwake = false;
        
        mSource.clip = clip;
        mSource.volume = volume;
        mSource.pitch = pitch;
        mSource.loop = loop;
        
        target.outputAudioMixerGroup = mixerGroup;

        return mSource;
    }

    /// <summary> Deinitialize and cleanup. </summary>
    public void Deinitialize()
    {
        if (mGO) 
        { GameObject.Destroy(mGO); }
    }
}
