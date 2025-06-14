using UnityEngine;
using UnityEngine.InputSystem;

public class pauseScript : MonoBehaviour
{
    private bool isPaused = false;
    private bool wasButtonPressed = false; // Track previous button state

    [Header("Dependencies")]
    [SerializeField] private InputActionReference pauseButton;
    [SerializeField] private SpriteRenderer menuSprite;

    private void Pause()
    {
        menuSprite.enabled = true;
        isPaused = true;
        Time.timeScale = 0;
    }

    private void Unpause()
    {
        menuSprite.enabled = false;
        isPaused = false;
        Time.timeScale = 1;
    }

    private float getInput()
    {
        return pauseButton.action.ReadValue<float>();
    }

    void Update()
    {
        bool isButtonPressed = getInput() > 0;

        // Only trigger when button goes from not pressed to pressed
        if (isButtonPressed && !wasButtonPressed)
        {
            if (!isPaused)
            {
                Pause();
            }
            else
            {
                Unpause();
            }
        }

        wasButtonPressed = isButtonPressed;
    }
}