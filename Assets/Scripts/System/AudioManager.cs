using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioMixer audioMixer; // Assign your Master mixer in inspector
    public AudioSource musicSource;
    public AudioSource sfxSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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

            // Start a short coroutine to re-apply saved volumes a few times.
            // Some platforms have timing issues where the mixer isn't fully ready at Awake,
            // reapplying a couple times shortly after startup ensures the values stick.
            StartCoroutine(ReapplySavedVolumes());
            Debug.Log($"[AudioManager] Awake applied initial volumes (Master={PlayerPrefs.GetFloat("MasterVolume", 1f)}, Music={PlayerPrefs.GetFloat("MusicVolume", 1f)}, SFX={PlayerPrefs.GetFloat("SFXVolume", 1f)})");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    // These methods are called by your UI sliders
    public void SetMusicVolume(float value)
    {
        float volume = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        audioMixer.SetFloat("MusicVolume", volume);
        Debug.Log($"[AudioManager] SetMusicVolume({value}) -> {volume} dB");
    }

    public void SetSFXVolume(float value)
    {
        float volume = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        audioMixer.SetFloat("SFXVolume", volume);
        Debug.Log($"[AudioManager] SetSFXVolume({value}) -> {volume} dB");
    }

    public void SetMasterVolume(float value)
    {
        float volume = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        audioMixer.SetFloat("MasterVolume", volume);
        Debug.Log($"[AudioManager] SetMasterVolume({value}) -> {volume} dB");
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
}