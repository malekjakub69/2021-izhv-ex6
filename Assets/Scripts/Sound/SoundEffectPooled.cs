using System;
using JetBrains.Annotations;
using Unity.Audio;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Pooled sound clip wrapper representing a single sound effect.
/// </summary>
[Serializable]
public class SoundEffectPooled
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

    [Header("Pooling")] 
    [Tooltip("Number of available sources in the pool?")]
    [Range(0, 256)]
    public int poolSize = 1;
    [Tooltip("Allow positioning of the audio sources?")]
    public bool allowPositioning = true;
    
    public AudioSource audioSource
    { get => GetNextAudioSource(); }

    /// <summary> List of internal audio sources used for playback. </summary>
    private AudioSource[] mSources;
    /// <summary> Internal GameObject used when no other is provided to the Initialize. </summary>
    private GameObject mGO;
    /// <summary> Internal list of GameObjects used when positioning is requested. </summary>
    private GameObject[] mGOs;
    
    /// <summary> Cleanup and destroy. </summary>
    ~SoundEffectPooled()
    { Deinitialize(); }

    /// <summary>
    /// Initialize this sound effect, creating AudioSource on the target object.
    /// </summary>
    /// <param name="target">Target GameObject to create audio sources in.</param>
    public void Initialize([CanBeNull] GameObject target)
    {
        if (!target)
        { mGO = new GameObject(); target = mGO; }
        
        mSources = new AudioSource[poolSize];
        if (allowPositioning)
        { mGOs = new GameObject[poolSize]; }
        for (int iii = 0; iii < poolSize; ++iii)
        {
            if (allowPositioning)
            { // Create a GameObject for each pooled sound to allow changing of its position.
                var go = new GameObject($"SoundPoolSource_{identifier}_{iii}");
                go.transform.parent = target.transform; mGOs[iii] = go;
                mSources[iii] = Initialize(go.AddComponent<AudioSource>());
            }
            else
            { // Use the root GameObject, since position will not be changed.
                mSources[iii] = Initialize(target.AddComponent<AudioSource>());
            }
        }
    }
    
    /// <summary>
    /// Initialize this sound effect, using pre-existing AudioSource.
    /// </summary>
    /// <param name="target">Target AudioSource to use.</param>
    private AudioSource Initialize(AudioSource target)
    {
        target.playOnAwake = false;
        
        target.clip = clip;
        target.volume = volume;
        target.pitch = pitch;
        target.loop = loop;

        target.outputAudioMixerGroup = mixerGroup;

        return target;
    }

    /// <summary>
    /// Get the next available audio source which is not currently playing a sound.
    /// </summary>
    /// <param name="position">Optional position to place the audio source at. Must be enabled!</param>
    /// <param name="failOnFull">Fail when there are no free audio sources available?</param>
    /// <param name="fallbackOnFull">Fallback to the first audio source when all others are busy?</param>
    [CanBeNull]
    public AudioSource GetNextAudioSource(Vector3? position = null, 
        bool failOnFull = false, bool fallbackOnFull = true)
    {
        //Debug.Assert(allowPositioning || position == null);

        var freeAudioSource = Array.Find(mSources, (s) => !s.isPlaying);

        if (!freeAudioSource && fallbackOnFull)
        { freeAudioSource = mSources[0]; }
        
        if (freeAudioSource)
        { // We found a currently available audio source.
            if (position != null)
            { freeAudioSource.gameObject.transform.position = position.Value; }
        }
        else if (failOnFull)
        { throw new Exception("Soud effect pool has no free audio sources currently available!"); }

        return freeAudioSource;
    }
    
    /// <summary> Deinitialize and cleanup. </summary>
    public void Deinitialize()
    {
        foreach (var go in mGOs)
        { GameObject.Destroy(go); }
        if (mGO) 
        { GameObject.Destroy(mGO); }
    }
}
