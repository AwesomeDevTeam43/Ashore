using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    
    [Header("Mixer Groups")]
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;
    
    [Header("Background Music")]
    [Tooltip("Música que toca automaticamente ao iniciar o jogo")]
    public AudioClip backgroundMusic;
    [Tooltip("Loop da música de fundo")]
    public bool loopBackgroundMusic = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            RemoveDuplicateAudioManagers();

            InitializeAudioSources();
            
            if (audioMixer == null)
            {
                Debug.LogError("[AudioManager] AudioMixer reference is NOT set in the Inspector!");
            }
            else
            {
                Debug.Log("[AudioManager] AudioMixer reference is set: " + audioMixer.name);
            }
            
            // Apply saved volumes immediately on creation
            float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
            float music = PlayerPrefs.GetFloat("MusicVolume", 1f);
            float sfx = PlayerPrefs.GetFloat("SFXVolume", 1f);
            Debug.Log($"[AudioManager] Awake: Master={master}, Music={music}, SFX={sfx}");
            SetMasterVolume(master);
            SetMusicVolume(music);
            SetSFXVolume(sfx);

            StartCoroutine(ReapplySavedVolumes());
            
            // Iniciar música de fundo se configurada
            if (backgroundMusic != null)
            {
                PlayMusic(backgroundMusic);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RemoveDuplicateAudioManagers();
    }
    
    private void InitializeAudioSources()
    {
        // Criar musicSource se não existir
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.playOnAwake = false;
        musicSource.loop = loopBackgroundMusic;
        musicSource.spatialBlend = 0f; // 2D
        if (musicGroup != null)
            musicSource.outputAudioMixerGroup = musicGroup;
        
        // Criar sfxSource se não existir
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        if (sfxGroup != null)
            sfxSource.outputAudioMixerGroup = sfxGroup;
    }

    /// <summary>
    /// Toca uma música. Se já estiver a tocar a mesma música, não reinicia.
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        
        // Não reiniciar se já está a tocar a mesma música
        if (musicSource.clip == clip && musicSource.isPlaying)
            return;
        
        musicSource.clip = clip;
        musicSource.loop = loopBackgroundMusic;
        musicSource.Play();
        Debug.Log($"[AudioManager] A tocar música: {clip.name}");
    }
    
    /// <summary>
    /// Para a música atual
    /// </summary>
    public void StopMusic()
    {
        musicSource.Stop();
    }
    
    /// <summary>
    /// Pausa a música
    /// </summary>
    public void PauseMusic()
    {
        musicSource.Pause();
    }
    
    /// <summary>
    /// Retoma a música
    /// </summary>
    public void ResumeMusic()
    {
        musicSource.UnPause();
    }
    
    /// <summary>
    /// Verifica se a música está a tocar
    /// </summary>
    public bool IsMusicPlaying() => musicSource.isPlaying;

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
    
    public void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public AudioMixerGroup GetSFXGroup()
    {
        return sfxGroup;
    }
    
    public AudioMixerGroup GetMusicGroup()
    {
        return musicGroup;
    }

    // These methods are called by your UI sliders
    public void SetMusicVolume(float value)
    {
        currentMusicVolume = Mathf.Clamp01(value);
        
        // Apply combined master + music volume to AudioSource
        if (musicSource != null)
        {
            musicSource.volume = currentMasterVolume * currentMusicVolume;
        }
        
        // Also apply to AudioMixer if available
        if (audioMixer != null)
        {
            float volume = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
            audioMixer.SetFloat("MusicVolume", volume);
        }
        
        Debug.Log($"[AudioManager] SetMusicVolume({value})");
    }

    public void SetSFXVolume(float value)
    {
        currentSFXVolume = Mathf.Clamp01(value);
        
        // Apply combined master + sfx volume to AudioSource
        if (sfxSource != null)
        {
            sfxSource.volume = currentMasterVolume * currentSFXVolume;
        }
        
        // Also apply to AudioMixer if available
        if (audioMixer != null)
        {
            float volume = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
            audioMixer.SetFloat("SFXVolume", volume);
        }
        
        Debug.Log($"[AudioManager] SetSFXVolume({value})");
    }

    // Track master volume for applying to sources that might bypass mixer
    private float currentMasterVolume = 1f;
    private float currentMusicVolume = 1f;
    private float currentSFXVolume = 1f;

    public void SetMasterVolume(float value)
    {
        currentMasterVolume = Mathf.Clamp01(value);
        
        if (audioMixer != null)
        {
            float volume = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
            audioMixer.SetFloat("MasterVolume", volume);
        }
        
        // Also apply master volume to audio sources directly to ensure it affects all audio
        // This handles cases where the mixer routing might not properly chain Master -> Music/SFX
        ApplyVolumesToSources();
        
        Debug.Log($"[AudioManager] SetMasterVolume({value})");
    }
    
    /// <summary>
    /// Applies the combined master + channel volumes directly to AudioSources.
    /// This ensures master volume always affects all audio regardless of mixer routing.
    /// </summary>
    private void ApplyVolumesToSources()
    {
        if (musicSource != null)
        {
            musicSource.volume = currentMasterVolume * currentMusicVolume;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = currentMasterVolume * currentSFXVolume;
        }
    }

    private System.Collections.IEnumerator ReapplySavedVolumes()
    {
        // Try a few times with small delays to work around initialization timing issues
        var delays = new float[] { 0.05f, 0.2f, 0.5f };
        for (int i = 0; i < delays.Length; i++)
        {
            yield return new WaitForSecondsRealtime(delays[i]);
            float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
            float music = PlayerPrefs.GetFloat("MusicVolume", 1f);
            float sfx = PlayerPrefs.GetFloat("SFXVolume", 1f);
            SetMasterVolume(master);
            SetMusicVolume(music);
            SetSFXVolume(sfx);
            Debug.Log($"[AudioManager] Reapplied volumes attempt {i + 1}: Master={master}, Music={music}, SFX={sfx}");
        }
    }

    private void RemoveDuplicateAudioManagers()
    {
        var managers = Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None);
        foreach (var manager in managers)
        {
            if (manager == null || manager == this) continue;
            Destroy(manager.gameObject);
        }
    }
}