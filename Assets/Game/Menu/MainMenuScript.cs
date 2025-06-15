using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuScript : MonoBehaviour
{
    private enum Menus { MAIN, SETTINGS }
    private Menus currentMenu = Menus.MAIN;
    private bool isTransitioning = false;

    [Header("Attributes")]
    [SerializeField] private float transitionSpeed = 0.2f;

    [Header("Dependencies")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject settingMenu;

    private Vector3 menuMainPos, settingMainPos, menuCenteredPos, settingCenteredPos;

    private void Start()
    {
        menuCenteredPos = mainMenu.transform.position;
        settingCenteredPos = settingMenu.transform.position;

        print(menuMainPos);
        print(settingMainPos);

        // Initializing position
        menuMainPos = mainMenu.transform.position;

        settingMainPos = new Vector3(settingCenteredPos.y + 3000, settingCenteredPos.y, settingCenteredPos.z);
        settingMenu.transform.position = settingMainPos;
    }

    private IEnumerator menuTransition(Menus from, Menus to)
    {
        Vector3 mainStartPos = Vector3.zero, mainTargetPos = Vector3.zero, settingStartPos = Vector3.zero, settingTargetPos = Vector3.zero;
        bool shouldTransition = false;

        if (from != to)
        {
            shouldTransition = true;

            // Current position
            mainStartPos = mainMenu.transform.position;
            settingStartPos = settingMenu.transform.position;

            if (from == Menus.MAIN && to == Menus.SETTINGS) // Menu to Setting
            {
                mainTargetPos = new Vector3(mainStartPos.x - 3000, mainStartPos.y, mainStartPos.z);
                settingTargetPos = settingCenteredPos;
            }
            else if (from == Menus.SETTINGS && to == Menus.MAIN) // Setting to Menu
            {
                mainTargetPos = menuCenteredPos;
                settingTargetPos = new Vector3(settingStartPos.x + 3000, settingStartPos.y, settingStartPos.z); ;
            }
        }
        
        if(!shouldTransition) // If we should not transition (i.e. same from and to input for some reason)
        {
            isTransitioning = false;
            yield break; // exit early if no tranition needed
        }


        float elapsedTime = 0f;

        while (elapsedTime < transitionSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionSpeed; // The percentage fraction of current transition animation
            
            mainMenu.transform.position = Vector3.Lerp(mainStartPos, mainTargetPos, t);
            settingMenu.transform.position = Vector3.Lerp(settingStartPos, settingTargetPos, t);

            yield return null;
        }

        // Ensure final is correct
        mainMenu.transform.position = mainTargetPos;
        settingMenu.transform.position = settingTargetPos;

        isTransitioning = false; // DONE! Set flag back
    }

    // Public
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