using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScoreDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public string fillyServedPrefix = "Fillies Served: ";
    public string scorePrefix = "Score: ";
    public string timerPrefix = "Time: ";

    [Header("Game Over UI")]
    public GameObject gameOverUI; // Assign your game over UI element
    public Sprite[] trophySprites;
    public Image trophyImage;
    public Text gameOverText;

    [Header("Game Over Audio")]
    [SerializeField] private AudioController AudioController;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip failureSound;

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
        string displayText;

        int min = GameStateManager.countdownTimer / 60;
        int sec = GameStateManager.countdownTimer % 60;

        displayText = timerPrefix + min.ToString("00") + ":" + sec.ToString("00") + "\n" + scorePrefix + GameStateManager.totalScore.ToString();

        uiText.text = displayText;
    }

    IEnumerator CountdownCoroutine()
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

    private void OnTimerReachedZero()
    {
        // Pause the game
        Time.timeScale = 0;
        GameStateManager.SetPaused(true);

        gameOverText.text = fillyServedPrefix + GameStateManager.totalCustomerServed.ToString() + "\n" + scorePrefix + GameStateManager.totalScore.ToString();

        // Set the trophy sprite based on score
        SetTrophySprite();

        // Activate game over UI
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }
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

        private int GetTrophyIndex()
    {
        // Example trophy logic based on score
        // Adjust these thresholds based on your game's scoring system
        int score = GameStateManager.totalScore;

        if (score >= 2000)
        {
            AudioController.playFX(victorySound);
            return 0; // Gold trophy
        }
        else if (score >= 1500)
        {
            AudioController.playFX(victorySound);
            return 1; // Silver trophy
        }
        else if (score >= 1000)
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