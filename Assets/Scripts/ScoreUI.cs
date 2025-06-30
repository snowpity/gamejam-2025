using UnityEngine;
using UnityEngine.UI;

public class ScoreDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public string fillyServedPrefix = "Fillies Served: ";
    public string scorePrefix = "Score: ";
   

    private Text uiText;

    void Start()
    {
        // Get the Text component on this GameObject
        uiText = GetComponent<Text>();

        UpdateDisplay();
    }

    void Update()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        string displayText;

        displayText = fillyServedPrefix + GameStateManager.totalCustomerServed.ToString() + "\n" + scorePrefix + GameStateManager.totalScore.ToString();

        uiText.text = displayText;
    }
}