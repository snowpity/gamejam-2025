using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UIElements;
using System.IO;

[System.Serializable]
public class GameSettings
{
    public float audio = 100f;
    public bool futa = false;
}

public class MainMenuScript : MonoBehaviour
{
    private enum Menus { MAIN, SETTINGS }
    private Menus currentMenu = Menus.MAIN;
    private bool isTransitioning = false;

    [Header("Attributes")]
    [SerializeField] private float transitionSpeed = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip futaToggleSound;
    [SerializeField] private AudioClip backgroundMusic;

    private AudioSource audioSource;
    private AudioSource musicSource;

    private UIDocument document;
    private Button playButton, settingsButton, backButton, exitButton;
    private Slider volumeSlider;
    private Toggle futaToggle;

    private VisualElement mainMenuElement;
    private VisualElement settingsMenuElement;

    private GameSettings gameSettings;
    private string settingsPath;

    private void Start()
    {
        document = GetComponent<UIDocument>();

        SetupAudioSources();

        // Set up settings path
        settingsPath = Path.Combine(Application.dataPath, "Resources/Settings/settings.json");

        // Load settings
        LoadSettings();

        // Get UI elements
        playButton = document.rootVisualElement.Q("playButton") as Button;
        settingsButton = document.rootVisualElement.Q("settingsButton") as Button;
        backButton = document.rootVisualElement.Q("backButton") as Button;
        exitButton = document.rootVisualElement.Q("exitButton") as Button;

        // Get settings UI elements
        volumeSlider = document.rootVisualElement.Q("Volume") as Slider;
        futaToggle = document.rootVisualElement.Q("Toggle") as Toggle;

        mainMenuElement = document.rootVisualElement.Q("mainMenu");
        settingsMenuElement = document.rootVisualElement.Q("settingsMenu");

        // Apply loaded settings to UI
        ApplySettingsToUI();

        // Start background music
        PlayBackgroundMusic();

        // Register button callbacks
        if (playButton != null)
            playButton.RegisterCallback<ClickEvent>(onPlayButtonClick);

        if (settingsButton != null)
            settingsButton.RegisterCallback<ClickEvent>(onSettingsButtonClick);

        if (backButton != null)
            backButton.RegisterCallback<ClickEvent>(onBackButtonClick);

        if (exitButton != null)
            exitButton.RegisterCallback<ClickEvent>(onExitButtonClick);

        // Register settings callbacks
        if (volumeSlider != null)
            volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);

        if (futaToggle != null)
            futaToggle.RegisterValueChangedCallback(OnFutaToggleChanged);
    }

    private void OnDisable()
    {
        // Stop background music when disabling
        StopBackgroundMusic();

        // Unregister all callbacks
        if (playButton != null)
            playButton.UnregisterCallback<ClickEvent>(onPlayButtonClick);

        if (settingsButton != null)
            settingsButton.UnregisterCallback<ClickEvent>(onSettingsButtonClick);

        if (backButton != null)
            backButton.UnregisterCallback<ClickEvent>(onBackButtonClick);

        if (exitButton != null)
            exitButton.UnregisterCallback<ClickEvent>(onExitButtonClick);

        if (volumeSlider != null)
            volumeSlider.UnregisterValueChangedCallback(OnVolumeChanged);

        if (futaToggle != null)
            futaToggle.UnregisterValueChangedCallback(OnFutaToggleChanged);
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
            else
            {
                // Create default settings if file doesn't exist
                gameSettings = new GameSettings();
                SaveSettings();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load settings: {e.Message}");
            gameSettings = new GameSettings(); // Use defaults
        }
    }

    private void SaveSettings()
    {
        try
        {
            string directory = Path.GetDirectoryName(settingsPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string jsonContent = JsonUtility.ToJson(gameSettings, true);
            File.WriteAllText(settingsPath, jsonContent);

            Debug.Log("Settings saved successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save settings: {e.Message}");
        }
    }

    private void ApplySettingsToUI()
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = gameSettings.audio;
        }

        if (futaToggle != null)
        {
            futaToggle.value = gameSettings.futa;
        }
    }

    private void SetupAudioSources()
    {
        // Create AudioSource for UI sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = AudioListener.volume;
        audioSource.pitch = 1f;
        audioSource.spatialBlend = 0f; // 2D sound
        audioSource.priority = 128;

        // Create AudioSource for background music
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true; // Loop the background music
        musicSource.volume = AudioListener.volume;
        musicSource.pitch = 1f;
        musicSource.spatialBlend = 0f; // 2D sound
        musicSource.priority = 64; // Higher priority than UI sounds
    }

    private void PlayBackgroundMusic()
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

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(volume);
        }
    }

    private void OnVolumeChanged(ChangeEvent<float> evt)
    {
        gameSettings.audio = evt.newValue;
        SaveSettings();

        // Apply volume change immediately
        float normalizedVolume = evt.newValue / 100f;
        AudioListener.volume = normalizedVolume;

        // Update both AudioSource volumes to match
        if (audioSource != null)
        {
            audioSource.volume = normalizedVolume;
        }

        if (musicSource != null)
        {
            musicSource.volume = normalizedVolume;
        }
    }

    private void OnFutaToggleChanged(ChangeEvent<bool> evt)
    {
        gameSettings.futa = evt.newValue;
        SaveSettings();
        Debug.Log($"Futa mode: {evt.newValue}");

        if(gameSettings.futa)
            audioSource.PlayOneShot(futaToggleSound);
    }

    private void onPlayButtonClick(ClickEvent evt)
    {
        // Stop background music when transitioning to game
        StopBackgroundMusic();
        toLevel(1);
    }

    private void onSettingsButtonClick(ClickEvent evt)
    {
        transitionMenuToSetting();
    }

    private void onBackButtonClick(ClickEvent evt)
    {
        transitionSettingToMenu();
    }

    private void onExitButtonClick(ClickEvent evt)
    {
        // Stop background music before exiting
        StopBackgroundMusic();

#if UNITY_EDITOR
        // If running in the Unity Editor, stop play mode
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // If running as a built application, quit the application
        Application.Quit();
#endif
    }

    private IEnumerator menuTransition(Menus from, Menus to)
    {
        if (from == to)
        {
            isTransitioning = false;
            yield break;
        }

        float elapsedTime = 0f;
        float startMainTranslateX = 0f;
        float targetMainTranslateX = 0f;
        float startSettingTranslateX = 200f; // Starting position from CSS
        float targetSettingTranslateX = 200f;

        if (from == Menus.MAIN && to == Menus.SETTINGS)
        {
            // Main menu slides out to the left, settings menu slides in from the right
            startMainTranslateX = 0f;
            targetMainTranslateX = -200f;
            startSettingTranslateX = 200f;
            targetSettingTranslateX = 0f;
            currentMenu = Menus.SETTINGS;
        }
        else if (from == Menus.SETTINGS && to == Menus.MAIN)
        {
            // Settings menu slides out to the right, main menu slides in from the left
            startMainTranslateX = -200f;
            targetMainTranslateX = 0f;
            startSettingTranslateX = 0f;
            targetSettingTranslateX = 200f;
            currentMenu = Menus.MAIN;
        }

        while (elapsedTime < transitionSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionSpeed;

            // Smooth interpolation
            float currentMainTranslateX = Mathf.Lerp(startMainTranslateX, targetMainTranslateX, t);
            float currentSettingTranslateX = Mathf.Lerp(startSettingTranslateX, targetSettingTranslateX, t);

            // Apply transforms
            if (mainMenuElement != null)
                mainMenuElement.style.translate = new Translate(Length.Percent(currentMainTranslateX), 0);

            if (settingsMenuElement != null)
                settingsMenuElement.style.translate = new Translate(Length.Percent(currentSettingTranslateX), 0);

            yield return null;
        }

        // Ensure final positions are exact
        if (mainMenuElement != null)
            mainMenuElement.style.translate = new Translate(Length.Percent(targetMainTranslateX), 0);

        if (settingsMenuElement != null)
            settingsMenuElement.style.translate = new Translate(Length.Percent(targetSettingTranslateX), 0);

        isTransitioning = false;
    }

    // Public methods
    public void transitionMenuToSetting()
    {
        if (!isTransitioning)
        {
            isTransitioning = true;
            StartCoroutine(menuTransition(Menus.MAIN, Menus.SETTINGS));
        }
    }

    public void transitionSettingToMenu()
    {
        if (!isTransitioning)
        {
            isTransitioning = true;
            StartCoroutine(menuTransition(Menus.SETTINGS, Menus.MAIN));
        }
    }

    public void toLevel(int sceneID)
    {
        GameStateManager.SetDefault(); // Precaution, we'll set all public flag to it's default state.
        SceneManager.LoadScene(sceneID);
    }

    // Public getter for other scripts to access settings
    public GameSettings GetSettings()
    {
        return gameSettings;
    }
}