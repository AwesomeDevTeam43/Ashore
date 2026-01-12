using UnityEngine;

/// <summary>
/// Attach this to a GameObject with an AudioSource to play scene-specific music.
/// This component respects AudioManager - it won't play if AudioManager is already playing music,
/// and it will stop if AudioManager starts playing something else.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SceneMusic : MonoBehaviour
{
    [Tooltip("If true, this will tell AudioManager to play this clip instead of managing it locally.")]
    public bool useAudioManager = true;
    
    private AudioSource audioSource;
    private static SceneMusic currentlyPlaying;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Disable Play On Awake - we'll handle it ourselves
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        // If there's already a SceneMusic playing, stop it
        if (currentlyPlaying != null && currentlyPlaying != this)
        {
            currentlyPlaying.Stop();
        }
        
        if (useAudioManager && AudioManager.Instance != null)
        {
            // Let AudioManager handle the music
            if (audioSource.clip != null)
            {
                AudioManager.Instance.PlayMusic(audioSource.clip);
            }
            // Disable this source since AudioManager is handling it
            audioSource.enabled = false;
        }
        else
        {
            // No AudioManager, play locally
            if (audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.Play();
                currentlyPlaying = this;
            }
        }
    }

    public void Stop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        if (currentlyPlaying == this)
        {
            currentlyPlaying = null;
        }
    }

    private void OnDestroy()
    {
        if (currentlyPlaying == this)
        {
            currentlyPlaying = null;
        }
    }
}
