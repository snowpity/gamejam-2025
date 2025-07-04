using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UIElements;
using System.IO;

[System.Serializable]
public class GameSettings
{
    public float music = 50f;
    public float audio = 50f;
    public bool futa = false;
}

public class MainMenuScript : MonoBehaviour
{
    private enum Menus { MAIN, SETTINGS, LEVEL }
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
    private Slider backgroundMusicSlider;
    private Slider volumeSlider;
    private Toggle futaToggle;

    private VisualElement mainMenuElement;
    private VisualElement settingsMenuElement;
    private VisualElement levelMenuElement;

    private GameSettings gameSettings;
    private string settingsPath;

    private void Start()
    {
        document = GetComponent<UIDocument>();

        // Set up settings path
        settingsPath = Path.Combine(Application.dataPath, "Resources/Settings/settings.json");

        // Load settings
        LoadSettings();

        SetupAudioSources();

        // Get MAIN UI elements
        playButton = document.rootVisualElement.Q("playButton") as Button;
        settingsButton = document.rootVisualElement.Q("settingsButton") as Button;
        exitButton = document.rootVisualElement.Q("exitButton") as Button;

        // Get settings UI elements
        backgroundMusicSlider = document.rootVisualElement.Q("BGMusic") as Slider;
        volumeSlider = document.rootVisualElement.Q("Volume") as Slider;
        futaToggle = document.rootVisualElement.Q("Toggle") as Toggle;

        // GET MENU WRAPPERS
        mainMenuElement = document.rootVisualElement.Q("mainMenu");
        settingsMenuElement = document.rootVisualElement.Q("settingsMenu");
        levelMenuElement = document.rootVisualElement.Q("levelMenu");

        RegisterAllBackButtons();
        RegisterAllLevelButtons();

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
        if (backgroundMusicSlider != null)
            backgroundMusicSlider.RegisterValueChangedCallback(OnBGMusicChanged);

        if (volumeSlider != null)
            volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);

        if (futaToggle != null)
            futaToggle.RegisterValueChangedCallback(OnFutaToggleChanged);
    }
    private void RegisterAllBackButtons()
    {
        // Get all buttons with the "backButton" class
        var allBackButtons = document.rootVisualElement.Query<Button>(className: "backButton").ToList();

        // Register the same callback for all of them
        foreach (var button in allBackButtons)
        {
            button.RegisterCallback<ClickEvent>(onBackButtonClick);
        }

        Debug.Log($"Registered {allBackButtons.Count} back buttons");
    }

    // LOAD AND SAVE
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
        if (backgroundMusicSlider != null)
        {
            backgroundMusicSlider.value = gameSettings.music;
        }

        if (volumeSlider != null)
        {
            volumeSlider.value = gameSettings.audio;
        }

        if (futaToggle != null)
        {
            futaToggle.value = gameSettings.futa;
        }
    }

    // AUDIO SETUP
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

    private void OnBGMusicChanged(ChangeEvent<float> evt)
    {
        gameSettings.music = evt.newValue;
        SaveSettings();

        // Apply volume change immediately
        float normalizedVolume = evt.newValue / 100f;

        // Update both AudioSource volumes to match
        if (musicSource != null)
        {
            musicSource.volume = normalizedVolume;
        }
    }

    // SETTINGS
    private void OnVolumeChanged(ChangeEvent<float> evt)
    {
        gameSettings.audio = evt.newValue;
        SaveSettings();

        // Apply volume change immediately
        float normalizedVolume = evt.newValue / 100f;

        // Update both AudioSource volumes to match
        if (audioSource != null)
        {
            audioSource.volume = normalizedVolume;
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

    // BUTTON REGISTERING
    private void onPlayButtonClick(ClickEvent evt)
    {
        transitionMenuToLevel();
    }

    private void onSettingsButtonClick(ClickEvent evt)
    {
        transitionMenuToSetting();
    }

    private void onBackButtonClick(ClickEvent evt)
    {
        transitionToMenu();
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

    private void RegisterAllLevelButtons()
    {
        // Get all buttons whose name starts with "lvl" and ends with "Button"
        var allButtons = document.rootVisualElement.Query<Button>().ToList();

        int registeredCount = 0;

        foreach (var button in allButtons)
        {
            string buttonName = button.name;

            // Check if button name matches the pattern "lvl[number]-Button"
            if (buttonName.StartsWith("lvl") && buttonName.EndsWith("-Button"))
            {
                // Extract the number between "lvl" and "-Button"
                string numberPart = buttonName.Substring(3, buttonName.Length - 3 - 7); // Remove "lvl" and "-Button"

                if (int.TryParse(numberPart, out int levelNumber))
                {
                    // Register callback with the extracted level number
                    button.RegisterCallback<ClickEvent>(evt => OnLevelButtonClick(levelNumber));
                    registeredCount++;
                    Debug.Log($"Registered level button: {buttonName} -> Level {levelNumber}");
                }
                else
                {
                    Debug.LogWarning($"Could not parse level number from button name: {buttonName}");
                }
            }
        }

        Debug.Log($"Registered {registeredCount} level buttons");
    }

    private void OnLevelButtonClick(int levelNumber)
    {
        Debug.Log($"Level {levelNumber} button clicked");

        StopBackgroundMusic();
        toLevel(levelNumber);
    }

private IEnumerator menuTransition(Menus from, Menus to)
    {
        if (from == to)
        {
            isTransitioning = false;
            yield break;
        }

        if (from == Menus.MAIN && to == Menus.SETTINGS)
        {
            // Main menu slides out to the left, settings menu slides in from the right
            if (mainMenuElement != null)
            {
                mainMenuElement.RemoveFromClassList("menuCentered");
                mainMenuElement.AddToClassList("menuLeft");
            }

            if (settingsMenuElement != null)
            {
                settingsMenuElement.RemoveFromClassList("menuRight");
                settingsMenuElement.AddToClassList("menuCentered");
            }

            currentMenu = Menus.SETTINGS;
        }
        else if (from == Menus.SETTINGS && to == Menus.MAIN)
        {
            // Settings menu slides out to the right, main menu slides in from the left
            if (mainMenuElement != null)
            {
                mainMenuElement.RemoveFromClassList("menuLeft");
                mainMenuElement.AddToClassList("menuCentered");
            }

            if (settingsMenuElement != null)
            {
                settingsMenuElement.RemoveFromClassList("menuCentered");
                settingsMenuElement.AddToClassList("menuRight");
            }

            currentMenu = Menus.MAIN;
        }
        else if (from == Menus.MAIN && to == Menus.LEVEL)
        {
            if (mainMenuElement != null)
            {
                mainMenuElement.RemoveFromClassList("menuCentered");
                mainMenuElement.AddToClassList("menuRight");
            }

            if (levelMenuElement != null)
            {
                levelMenuElement.RemoveFromClassList("menuLeft");
                levelMenuElement.AddToClassList("menuCentered");
            }

            currentMenu = Menus.LEVEL;
        }
        else if (from == Menus.LEVEL && to == Menus.MAIN)
        {
            if (mainMenuElement != null)
            {
                mainMenuElement.RemoveFromClassList("menuRight");
                mainMenuElement.AddToClassList("menuCentered");
            }

            if (levelMenuElement != null)
            {
                levelMenuElement.RemoveFromClassList("menuCentered");
                levelMenuElement.AddToClassList("menuLeft");
            }

            currentMenu = Menus.MAIN;
        }


        // Wait for the transition duration to complete
        yield return new WaitForSeconds(transitionSpeed);

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

    public void transitionMenuToLevel()
    {
        if (!isTransitioning)
        {
            isTransitioning = true;
            StartCoroutine(menuTransition(Menus.MAIN, Menus.LEVEL));
        }
    }

    public void transitionToMenu()
    {
        if (!isTransitioning)
        {
            isTransitioning = true;
            if(currentMenu == Menus.SETTINGS)
                StartCoroutine(menuTransition(Menus.SETTINGS, Menus.MAIN));
            if(currentMenu == Menus.LEVEL)
                StartCoroutine(menuTransition(Menus.LEVEL, Menus.MAIN));
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