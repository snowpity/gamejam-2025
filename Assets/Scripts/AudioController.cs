using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UIElements;
using System.IO;

public class AudioController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip backgroundMusic;

    private AudioSource audioSource;
    private AudioSource musicSource;
    private GameSettings gameSettings;

    private string settingsPath;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set up settings path
        settingsPath = Path.Combine(Application.dataPath, "Resources/Settings/settings.json");

        // Load settings
        LoadSettings();
        SetupAudioSources();
    }

    private void SetupAudioSources()
    {
        // Create AudioSource for UI sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = gameSettings.audio / 100f;
        audioSource.pitch = 1f;
        audioSource.spatialBlend = 0f;
        audioSource.priority = 128;

        // Create AudioSource for background music
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = gameSettings.music / 100f;
        musicSource.pitch = 1f;
        musicSource.spatialBlend = 0f;
        musicSource.priority = 64;
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(settingsPath))
            {
                string jsonContent = File.ReadAllText(settingsPath);
                gameSettings = JsonUtility.FromJson<GameSettings>(jsonContent);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load settings: {e.Message}");
            gameSettings = new GameSettings(); // Use defaults
        }
    }

    public void playFX(AudioClip audio)
    {
        audioSource.PlayOneShot(audio);
    }

    public void PlayBackgroundMusic(AudioClip backgroundMusic)
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
            Debug.Log("Background music started");
        }
        else
        {
            Debug.LogWarning("Cannot play background music - missing AudioSource or AudioClip");
        }
    }

    public void StopBackgroundMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            Debug.Log("Background music stopped");
        }
    }

    public void PauseBackgroundMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
            Debug.Log("Background music paused");
        }
    }

    public void ResumeBackgroundMusic()
    {
        if (musicSource != null && !musicSource.isPlaying && musicSource.clip != null)
        {
            musicSource.UnPause();
            Debug.Log("Background music resumed");
        }
    }
}
