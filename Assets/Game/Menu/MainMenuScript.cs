using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UIElements;

public class MainMenuScript : MonoBehaviour
{
    private enum Menus { MAIN, SETTINGS }
    private Menus currentMenu = Menus.MAIN;
    private bool isTransitioning = false;

    [Header("Attributes")]
    [SerializeField] private float transitionSpeed = 0.2f;

    private UIDocument document;
    private Button playButton;
    private Button settingsButton;
    private Button backButton;

    private VisualElement mainMenuElement;
    private VisualElement settingsMenuElement;

    private void Start()
    {
        document = GetComponent<UIDocument>();

        // Get UI elements
        playButton = document.rootVisualElement.Q("playButton") as Button;
        settingsButton = document.rootVisualElement.Q("settingsButton") as Button;
        backButton = document.rootVisualElement.Q("backButton") as Button;

        mainMenuElement = document.rootVisualElement.Q("mainMenu");
        settingsMenuElement = document.rootVisualElement.Q("settingsMenu");

        // Register button callbacks
        if (playButton != null)
            playButton.RegisterCallback<ClickEvent>(onPlayButtonClick);

        if (settingsButton != null)
            settingsButton.RegisterCallback<ClickEvent>(onSettingsButtonClick);

        if (backButton != null)
            backButton.RegisterCallback<ClickEvent>(onBackButtonClick);
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
    }

    private void onPlayButtonClick(ClickEvent evt)
    {
        toLevel(0);
    }

    private void onSettingsButtonClick(ClickEvent evt)
    {
        transitionMenuToSetting();
    }

    private void onBackButtonClick(ClickEvent evt)
    {
        transitionSettingToMenu();
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
        SceneManager.LoadScene(sceneID);
    }
}