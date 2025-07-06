using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class ScoreDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public string fillyServedPrefix = "Fillies Served: ";
    public string scorePrefix = "Score: ";
    public string timerPrefix = "Time: ";

    [Header("Game Over UI")]
    public GameObject gameOverUI; // Assign your game over UI element
    public GameObject hasNoNextObject;
    public GameObject hasNextObject;
    public Sprite[] trophySprites;
    public Image trophyImage;
    public Text gameOverText;

    [Header("Game Over Audio")]
    [SerializeField] private AudioController AudioController;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip failureSound;

    [Header("Visual Novel")]
    [SerializeField] private VisualNovel visualNovel; // Reference to VisualNovel component

    private Text uiText;
    private bool timerStarted = false;
    private Coroutine countdownCoroutine; // Store reference to the coroutine

    void Start()
    {
        // Get the Text component on this GameObject
        uiText = GetComponent<Text>();
        UpdateDisplay();
    }

    void Update()
    {
        // Start the timer when the game starts and timer hasn't started yet
        if (GameStateManager.IsGameStarted && !timerStarted)
        {
            // Stop any existing countdown coroutine before starting a new one
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
            }

            countdownCoroutine = StartCoroutine(CountdownCoroutine());
            timerStarted = true;
        }

        // Always update display to show current values
        UpdateDisplay();
    }

    // Add this method to reset the timer when replaying
    public void ResetTimer()
    {
        timerStarted = false;
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }

    private void UpdateDisplay()
    {
        string displayText = "";

        int min = GameStateManager.countdownTimer / 60;
        int sec = GameStateManager.countdownTimer % 60;

        if (GameStateManager.isInfiniteTime)
        {
            // displayText = timerPrefix + "Infinity" + "\n"; // No need to show this, the player knows it's infinite
            displayText = timerPrefix + min.ToString("00") + ":" + sec.ToString("00") + "\n";
            displayText = displayText + fillyServedPrefix + GameStateManager.totalCustomerServed.ToString() + "\n";
        }
        else
            displayText = timerPrefix + min.ToString("00") + ":" + sec.ToString("00") + "\n";

        displayText = displayText + scorePrefix + GameStateManager.totalScore.ToString();

        uiText.text = displayText;
    }

    IEnumerator CountdownCoroutine()
    {
        if (GameStateManager.isInfiniteTime)
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);

                if (!GameStateManager.IsPaused && GameStateManager.IsGameStarted)
                {
                    GameStateManager.countdownTimer++;
                }
            }

        }
        else
        {
            while (GameStateManager.countdownTimer > 0)
            {
                yield return new WaitForSeconds(1f);

                if (!GameStateManager.IsPaused && GameStateManager.IsGameStarted)
                {
                    GameStateManager.countdownTimer--;
                }
            }

            // Timer hit zero - trigger game over
            OnTimerReachedZero();
        }

    }

    private void OnTimerReachedZero()
    {
        Debug.Log("[ScoreUI] OnTimerReachedZero called. Scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + ", nextLevel: " + GameStateManager.nextLevel);
        // Pause the game
        Time.timeScale = 0;
        GameStateManager.SetPaused(true);

        gameOverText.text = fillyServedPrefix + GameStateManager.totalCustomerServed.ToString() + "\n" + scorePrefix + GameStateManager.totalScore.ToString();

        // Set the trophy sprite based on score
        SetTrophySprite();

        string currentScene = SceneManager.GetActiveScene().name;
        bool isLevel3 = currentScene == "Level 3";

        if (GameStateManager.nextLevel != -1 || isLevel3 && GetTrophyIndex() <= 2) // Trophy Index of 2 is bronze
        {
            hasNoNextObject.SetActive(false);
            hasNextObject.SetActive(true);

            Button nextButton = hasNextObject.transform.Find("NextButton")?.GetComponent<Button>();
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                if (isLevel3)
                {
                    Debug.Log("[ScoreUI] Setting NextButton to PlayLevel3WinDialogue");
                    nextButton.onClick.AddListener(PlayLevel3WinDialogue);
                }
                else
                {
                    Debug.Log("[ScoreUI] Setting NextButton to LoadNextLevel(" + GameStateManager.nextLevel + ")");
                    nextButton.onClick.AddListener(() => LoadNextLevel(GameStateManager.nextLevel));
                }
            }
        }
        else
        {
            hasNoNextObject.SetActive(true);
            hasNextObject.SetActive(false);
        }

        // Activate game over UI
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }
    }

    private void PlayLevel3WinDialogue()
    {
        Debug.Log("[ScoreUI] PlayLevel3WinDialogue called");
        if (visualNovel != null)
        {
            // Unpause the game so dialogue and coroutines work
            Debug.Log("[ScoreUI] Unpausing game and hiding game over UI");
            Time.timeScale = 1;
            GameStateManager.SetPaused(false);

            if (gameOverUI != null)
            {
                gameOverUI.SetActive(false);
            }

            Debug.Log("[ScoreUI] Loading level3_win_dialogue in VisualNovel");
            visualNovel.LoadDialogueFromJSON("level3_win_dialogue");

            Debug.Log("[ScoreUI] Starting WaitForDialogueComplete coroutine");
            StartCoroutine(WaitForDialogueComplete());
        }
        else
        {
            Debug.LogError("[ScoreUI] VisualNovel component not found! Cannot play win dialogue.");
            ReturnToMainMenu();
        }
    }

    private IEnumerator WaitForDialogueComplete()
    {
        Debug.Log("[ScoreUI] WaitForDialogueComplete coroutine started");
        int frameCount = 0;
        while (visualNovel != null && visualNovel.IsDialogueActive())
        {
            if (frameCount % 30 == 0) Debug.Log("[ScoreUI] Dialogue still active, waiting...");
            frameCount++;
            yield return null;
        }
        Debug.Log("[ScoreUI] Dialogue complete, returning to main menu");
        ReturnToMainMenu();
    }

    private void ReturnToMainMenu()
    {
        Unpause();
        GameStateManager.SetDefault();
        SceneManager.LoadScene(0); // Load main menu scene
    }

    private void SetTrophySprite()
    {
        if (trophyImage != null && trophySprites != null && trophySprites.Length > 0)
        {
            int trophyIndex = GetTrophyIndex();

            // Make sure the index is within bounds
            if (trophyIndex >= 0 && trophyIndex < trophySprites.Length)
            {
                trophyImage.sprite = trophySprites[trophyIndex];
            }
        }
    }

    public void LoadNextLevel(int sceneID)
    {
        Unpause();
        GameStateManager.SetDefault();
        SceneManager.LoadScene(sceneID);
    }

    public void Unpause()
    {
        Time.timeScale = 1;
        GameStateManager.SetPaused(false);
    }

    private int GetTrophyIndex()
    {
        int score = GameStateManager.totalScore;

        if (score >= GameStateManager.getGoldTrophyPoint())
        {
            AudioController.playFX(victorySound);
            return 0; // Gold trophy
        }
        else if (score >= GameStateManager.getSilverTrophyPoint())
        {
            AudioController.playFX(victorySound);
            return 1; // Silver trophy
        }
        else if (score >= GameStateManager.getCopperTrophyPoint())
        {
            AudioController.playFX(victorySound);
            return 2; // Bronze trophy
        }
        else
        {
            AudioController.playFX(failureSound);
            return 3; // Lol no star cuz u failed
        }
    }
}