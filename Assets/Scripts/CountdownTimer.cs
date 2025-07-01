using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CountdownTimer : MonoBehaviour
{
    public Text timerText;

    void Start()
    {
        StartCoroutine(CountdownCoroutine());
    }

    IEnumerator CountdownCoroutine()
    {
        while (GameStateManager.countdownTimer > 0)
        {
            if (!GameStateManager.IsPaused)
            {
                UpdateDisplay();
                yield return new WaitForSeconds(1f);
                GameStateManager.countdownTimer--;
            }
            else
            {
                yield return null;
            }
        }

        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        int minutes = GameStateManager.countdownTimer / 60;
        int seconds = GameStateManager.countdownTimer % 60;
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}