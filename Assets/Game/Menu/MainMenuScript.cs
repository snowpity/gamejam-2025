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
    [SerializeField] private AudioClip futaToggleSound; // Drag your sound clip here in the inspector

    // AudioSource will be created procedurally
    private AudioSource audioSource;

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

        // Create AudioSource procedurally
        SetupAudioSource();

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

    private void SetupAudioSource()
    {
        // Create a new AudioSource component
        audioSource = gameObject.AddComponent<AudioSource>();

        // Configure the AudioSource for UI sounds
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = AudioListener.volume; // Use current AudioListener volume
        audioSource.pitch = 1f;
        audioSource.spatialBlend = 0f; // 2D sound (not 3D positioned)
        audioSource.priority = 128; // Default priority
    }

    private void OnVolumeChanged(ChangeEvent<float> evt)
    {
        gameSettings.audio = evt.newValue;
        SaveSettings();

        // Apply volume change immediately
        AudioListener.volume = evt.newValue / 100f;

        // Update AudioSource volume to match
        if (audioSource != null)
        {
            audioSource.volume = AudioListener.volume;
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