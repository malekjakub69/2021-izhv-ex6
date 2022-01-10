using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Audio;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Audio;

/// <summary>
/// A simple sound manager which allows playing of sound effects.
/// Inspired by: https://www.youtube.com/watch?v=6OT43pvUyfY
/// </summary>
public class SoundManager : MonoBehaviour
{
#region Editor

    [Header("Sound Definition")] 
    [Tooltip("Primary mixer used for the sounds.")]
    public AudioMixer primaryMixer;
    [Tooltip("Mixer group used for the effects playback.")]
    public AudioMixerGroup effectMixerGroup;
    [Tooltip("Library of sound effects provided by the manager.")]
    public SoundEffect[] sounds = new SoundEffect[]
    {
        new SoundEffect()
    };
    [Tooltip("Enable pooling and placement of non-pooled sound effects?")]
    public bool poolSounds = false;
    [Tooltip("Size of the audio source pool used for pooling of the simple sound effects.")]
    public int poolSize = 10;
    [Space]
    [Tooltip("Library of pooled sound effects provided by the manager.")]
    public SoundEffectPooled[] pooledSounds = new SoundEffectPooled[]
    {
        new SoundEffectPooled()
    };
    
#endregion // Editor
    
#region Internal

    /// <summary>
    /// Singleton instance of the GameManager.
    /// </summary>
    private static SoundManager sInstance;
    
    /// <summary>
    /// Getter for the singleton GameManager object.
    /// </summary>
    public static SoundManager Instance
    { get { return sInstance; } }

    /// <summary> Dummy sound effect used for pooling of non-pooled sounds. </summary>
    private SoundEffectPooled mSoundEffectPool;

    /// <summary> Backup of the master volume used for un-muting. </summary>
    private float mMasterVolumeBackup;

    /// <summary> Did we mute the master? </summary>
    private bool mMasterMuted;
    
    /// <summary> Volume used for muted sounds. </summary>
    private static float VOLUME_MUTED = -80.0f;
    
#endregion // Internal

#region Interface

    /// <summary> Volume of the master mixer. </summary>
    public float masterVolume
    {
        get
        { primaryMixer.GetFloat("masterVol", out var val); return val; }
        set
        { primaryMixer.SetFloat("masterVol", value); }
    }
    
    /// <summary> Mute status of the master mixer. </summary>
    public bool masterMuted
    {
        get
        { primaryMixer.GetFloat("masterVol", out var val); return val <= VOLUME_MUTED; }
        set
        {
            if (value)
            { // Mute the mixer.
                primaryMixer.GetFloat("masterVol", out mMasterVolumeBackup); 
                mMasterMuted = true;
                primaryMixer.SetFloat("masterVol", VOLUME_MUTED);
            }
            else if (mMasterMuted)
            { // Un-mute the mixer.
                primaryMixer.SetFloat("masterVol", mMasterVolumeBackup);
                mMasterMuted = false;
            }
        }
    }

#endregion // Interface
    
    /// <summary> Initialize the sound manager. </summary>
    void Awake()
    {
        // Initialize the singleton instance, if no other exists.
        if (sInstance != null && sInstance != this)
        { Destroy(gameObject); }
        else
        { sInstance = this; }
        
        // Get the target mixer group for all of the effects.
        AudioMixerGroup mixerGroup = null;
        if (effectMixerGroup != null)
        { mixerGroup = effectMixerGroup; }
        else if (primaryMixer)
        { mixerGroup = primaryMixer.outputAudioMixerGroup; }
        
        // Initialize the sound library.
        foreach (var sound in sounds)
        { sound.mixerGroup = mixerGroup; sound.Initialize(gameObject); }
        foreach (var sound in pooledSounds)
        { sound.mixerGroup = mixerGroup; sound.Initialize(gameObject); }
        
        if (poolSounds)
        { // Initialize pooling if requested.
            mSoundEffectPool = new SoundEffectPooled();
            mSoundEffectPool.poolSize = poolSize;
            mSoundEffectPool.allowPositioning = true;
            mSoundEffectPool.mixerGroup = mixerGroup;
            mSoundEffectPool.Initialize(gameObject);
        }
    }

    /// <summary> Called before the first frame update. </summary>
    void Start()
    { }

    /// <summary> Update called once per frame. </summary>
    void Update()
    { }

    /// <summary>
    /// Play sound by identifier. The pooled properties are used only when poolSounds is enabled!
    /// </summary>
    /// <param name="identifier">Identifier of the sound</param>
    /// <param name="position">Optional position to place the audio source at. Must be enabled!</param>
    /// <param name="failOnFull">Fail when there are no free audio sources available?</param>
    /// <param name="fallbackOnFull">Fallback to the first audio source when all others are busy?</param>
    public void PlaySound(string identifier, Vector3? position = null, 
        bool failOnFull = false, bool fallbackOnFull = true)
    {
        //Debug.Assert(poolSounds || position == null);
        
        var sound = Array.Find(sounds, (s) => s.identifier == identifier);
        if (sound != null)
        {
            if (poolSounds)
            { // Perform pooling using the dummy pooled sound effect.
                var soundSource = mSoundEffectPool.GetNextAudioSource(position, failOnFull, fallbackOnFull);
                sound.Initialize(soundSource).Play();
            }
            else
            { // Simple sound effects, just play them.
                sound.audioSource.Play();
            }
        }
        else
        { throw new Exception($"Unknown sound identifier \"{identifier}\"!"); }
    }
    
    /// <summary>
    /// Play pooled sound by identifier.
    /// </summary>
    /// <param name="identifier">Identifier of the sound</param>
    /// <param name="position">Optional position to place the audio source at. Must be enabled!</param>
    /// <param name="failOnFull">Fail when there are no free audio sources available?</param>
    /// <param name="fallbackOnFull">Fallback to the first audio source when all others are busy?</param>
    public void PlayPooledSound(string identifier, Vector3? position = null, 
        bool failOnFull = false, bool fallbackOnFull = true)
    {
        var sound = Array.Find(pooledSounds, (s) => s.identifier == identifier);
        if (sound != null)
        { sound.GetNextAudioSource(position, failOnFull, fallbackOnFull)?.Play(); }
        else
        { throw new Exception($"Unknown sound identifier \"{identifier}\"!"); }
    }
}
