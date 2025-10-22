using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class Pause : MonoBehaviour
{
    public UIDocument document;
    public VisualElement ui;
    public Button saveButton;
    public Button loadButton;
    public Button exitButton;
    public string MainMenuSceneName;
    private bool shown = false;

    [SerializeField]
    public HexMapEditor editor;

    // Update is called once per frame
    void OnEnable()
    {
        // hide the menue on startup
        shown = document.enabled;
        if (shown) hide();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if(shown == false) show();
            else hide();
        }
    }

    void show()
    {
        if (shown == true) return; // nothing to do

        shown = true;
        document.enabled = true;

        // get the root UI element
        ui = GetComponent<UIDocument>().rootVisualElement;

        // grab all the buttons
        saveButton = ui.Q<Button>("SaveButton");
        loadButton = ui.Q<Button>("LoadButton");
        exitButton = ui.Q<Button>("ExitButton");

        // TODO: Check if we got the buttons and throw an Error otherwise

        // register the events
        saveButton.clicked += OnSaveClick;
        loadButton.clicked += OnLoadClick;
        exitButton.clicked += OnExitClick;
    }
    void hide()
    {
        if(shown == false) return; // nothing to do

        shown=false;
        document.enabled = false;
    }

    void OnSaveClick()
    {
        Debug.Log("SAVE");
        editor.Save();
    }
    void OnLoadClick()
    {
        Debug.Log("LOAD");
        editor.Load();
    }
    void OnExitClick()
    {
        Debug.Log("EXIT");
        // close the program because then we don't need to properly shut down the server
        // exit the application
        Application.Quit();

        // if still in Editor exit the play mode
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif

        // load the main menu
        //SceneManager.LoadScene(MainMenuSceneName);
    }
}
