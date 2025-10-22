using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NationSelector : MonoBehaviour
{
    public static NationSelector Instance { get; private set; }
    public VisualElement ui;
    public Button silvaniumButton;
    public Button fluviaterraButton;
    public Button aquiloniaButton;
    public Button oceanumButton;
    public Button aridusButton;
    public Button observerButton;
    
    // Unity Function
    private void OnEnable()
    {
        Instance = this;
        getElements();
    }

    // private functions
    private void getElements()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;
        silvaniumButton = ui.Q<Button>("SilvaniumButton");
        fluviaterraButton = ui.Q<Button>("FluviaterraButton");
        aquiloniaButton = ui.Q<Button>("AquiloniaButton");
        oceanumButton = ui.Q<Button>("OceanumButton");
        aridusButton = ui.Q<Button>("AridusButton");
        observerButton = ui.Q<Button>("ObserverButton");

        // register callbacks
        silvaniumButton.clicked += OnPressSilvanium;
        fluviaterraButton.clicked += OnPressFluviaterra;
        aquiloniaButton.clicked += OnPressAquilonia;
        oceanumButton.clicked += OnPressOceanum;
        aridusButton.clicked += OnPressAridus;
        observerButton.clicked += OnPressObserver;
    }


    // callbackEvents
    void OnPressSilvanium()
    {
        HexMapEditor.Instance.Load();
        HexMapEditor.Instance.RequestNation(1);
    }
    void OnPressFluviaterra()
    {
        HexMapEditor.Instance.Load();
        HexMapEditor.Instance.RequestNation(3);
    }
    void OnPressAquilonia()
    {
        HexMapEditor.Instance.Load();
        HexMapEditor.Instance.RequestNation(5);
    }
    void OnPressOceanum()
    {
        HexMapEditor.Instance.Load();
        HexMapEditor.Instance.RequestNation(4);
    }
    void OnPressAridus()
    {
        HexMapEditor.Instance.Load();
        HexMapEditor.Instance.RequestNation(2);
    }
    void OnPressObserver()
    {
        HexMapEditor.Instance.Load();
        HexMapEditor.Instance.RequestNation(0);
    }
}