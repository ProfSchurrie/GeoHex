using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour
{
    public VisualElement ui;
    public Button playButton;
    public Button exitButton;
    public string GameSceneName;
    
    void Awake()
    {
        // get the root UI element
        ui = GetComponent<UIDocument>().rootVisualElement;
    }

    void OnEnable()
    {
        // grab all the buttons
        playButton = ui.Q<Button>("StartButton");
        exitButton = ui.Q<Button>("ExitButton");

        // TODO: Check if we got the buttons and throw an Error otherwise

        // register the events
        playButton.clicked += OnPlayClick;
        exitButton.clicked += OnExitClick;
    }


    
    // the functions which get called, if the buttons are pressed
    void OnPlayClick()
    {
        // switch to the game scene
        SceneManager.LoadScene(GameSceneName);
    }
    void OnExitClick()
    {
        // exit the application
        Application.Quit();

        // if still in Editor exit the play mode
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
}
