using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum MenuOptions { NewGame, Exit, Controls, Settings, Credits, ControlsWindow, SettingsWindow, CreditsWindow }
public class MenuControls : MonoBehaviour
{
    public MenuOptions menuOption = MenuOptions.NewGame;
    public GameObject controlsPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;

    public Text newGame;
    public Text quit;
    public Text controls;
    public Text settings;
    public Text credits;

    public AudioSource audioSource;
    public AudioClip selectClip;
    public AudioClip backClip;
    public AudioClip upClip;
    public AudioClip downClip;

    public float timeInMenu;
    public bool playedEgg;
    public AudioClip eggClip;
    public GameObject music;

    private void Start()
    {
        DisablePanels();
        RefreshUI();
        playedEgg = false;
        music.SetActive(true);
    }
    private void Update()
    {
        if(!playedEgg)
            timeInMenu += Time.deltaTime;

        if (timeInMenu > 180 && !playedEgg)
        {
            music.SetActive(false);
            audioSource.PlayOneShot(eggClip);
            playedEgg = true;
        }

        if(playedEgg)
        {
            if (!audioSource.isPlaying)
                music.SetActive(true);
        }
    }
    public void OnInteract(InputValue value)
    {
        Select();
    }
    public void OnDown(InputValue value)
    {
        ScrollDown();
    }
    public void OnUp(InputValue value)
    {
        ScrollUp();
    }
    public void OnBack(InputValue value)
    {
        Back();
    }    
    public void DisablePanels()
    {
        controlsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }
    public void ScrollUp()
    {
        if ((menuOption == MenuOptions.NewGame) || (menuOption == MenuOptions.ControlsWindow) || (menuOption == MenuOptions.SettingsWindow) || (menuOption == MenuOptions.CreditsWindow))
            return;
        else
        {
            audioSource.PlayOneShot(upClip);
            if (menuOption == MenuOptions.Exit)
                menuOption = MenuOptions.NewGame;
            else if (menuOption == MenuOptions.Controls)
                menuOption = MenuOptions.Exit;
            else if (menuOption == MenuOptions.Settings)
                menuOption = MenuOptions.Controls;
            else if (menuOption == MenuOptions.Credits)
                menuOption = MenuOptions.Settings;
        }
        RefreshUI();
    }
    public void ScrollDown()
    {
        if ((menuOption == MenuOptions.Credits) || (menuOption == MenuOptions.ControlsWindow) || (menuOption == MenuOptions.SettingsWindow) || (menuOption == MenuOptions.CreditsWindow))
            return;
        else
        {
            audioSource.PlayOneShot(downClip);
            if (menuOption == MenuOptions.NewGame)
                menuOption = MenuOptions.Exit;
            else if (menuOption == MenuOptions.Exit)
                menuOption = MenuOptions.Controls;
            else if (menuOption == MenuOptions.Controls)
                menuOption = MenuOptions.Settings;
            else if (menuOption == MenuOptions.Settings)
                menuOption = MenuOptions.Credits;
        }
        RefreshUI();
    }
    public void Select()
    {
        if ((menuOption == MenuOptions.ControlsWindow) || (menuOption == MenuOptions.SettingsWindow) || (menuOption == MenuOptions.CreditsWindow))
            return;
        else
        {
            audioSource.PlayOneShot(selectClip);

            if (menuOption == MenuOptions.NewGame)
                NewGame();
            else if (menuOption == MenuOptions.Exit)
                Exit();
            else if (menuOption == MenuOptions.Controls)
                Controls();
            else if (menuOption == MenuOptions.Settings)
                Settings();
            else if (menuOption == MenuOptions.Credits)
                Credits();
        }
    }
    public void Back()
    {
        DisablePanels();
        if (menuOption == MenuOptions.ControlsWindow)
        {
            audioSource.PlayOneShot(backClip);
            menuOption = MenuOptions.Controls;
        }
        else if (menuOption == MenuOptions.SettingsWindow)
        {
            audioSource.PlayOneShot(backClip);
            menuOption = MenuOptions.Settings;
        }
        else if (menuOption == MenuOptions.CreditsWindow)
        {
            audioSource.PlayOneShot(backClip);
            menuOption = MenuOptions.Credits;
        }
    }
    public void NewGame()
    {
        SceneManager.LoadScene("MainLevel");
    }
    public void Exit()
    {
        Application.Quit();
    }
    public void Controls()
    {
        menuOption = MenuOptions.ControlsWindow;
        controlsPanel.SetActive(true);
    }
    public void Settings()
    {
        menuOption = MenuOptions.SettingsWindow;
        settingsPanel.SetActive(true);
    }
    public void Credits()
    {
        menuOption = MenuOptions.CreditsWindow;
        creditsPanel.SetActive(true);
    }
    public void RefreshUI()
    {
        newGame.color = Color.white;
        quit.color = Color.white;
        controls.color = Color.white;
        settings.color = Color.white;
        credits.color = Color.white;
        if (menuOption == MenuOptions.NewGame)
            newGame.color = Color.green;
        else if (menuOption == MenuOptions.Exit)
            quit.color = Color.green;
        else if (menuOption == MenuOptions.Controls)
            controls.color = Color.green;
        else if (menuOption == MenuOptions.Settings)
            settings.color = Color.green;
        else if (menuOption == MenuOptions.Credits)
            credits.color = Color.green;
    }
}
