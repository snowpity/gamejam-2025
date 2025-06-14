using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class pauseScript : MonoBehaviour
{
    private bool isPaused = false;
    private bool wasButtonPressed = false; // Track previous button state

    [Header("Dependencies")]
    [SerializeField] private InputActionReference pauseButton;
    [SerializeField] private GameObject pauseMenu;

    public void Pause()
    {
        pauseMenu.SetActive(true);
        isPaused = true;
        Time.timeScale = 0;
        GameStateManager.SetPaused(true);
    }

    public void Unpause()
    {
        pauseMenu.SetActive(false);
        isPaused = false;
        Time.timeScale = 1;
        GameStateManager.SetPaused(false);
    }

    // Eventually we'll have a button to go home
    public void Home(int sceneID)
    {
        Unpause();
        SceneManager.LoadScene(sceneID);
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