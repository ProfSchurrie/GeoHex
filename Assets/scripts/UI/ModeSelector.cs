using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class ModeSelector : MonoBehaviour
{
    public VisualElement ui;
    public Button hostButton;
    public Button clientButton;

    // Inspector variables
    [SerializeField]
    public StartNetwork networkStarter;
    [SerializeField]
    public GameObject ipSelection;
    
    public NationSelector nationSelector;

    // Unity Function
    private void Awake()
    {
        getElements();
    }

    // private functions
    private void getElements()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;
        hostButton = ui.Q<Button>("HostButton");
        clientButton = ui.Q<Button>("ClientButton");

        // register callbacks
        hostButton.clicked += OnPressHost;
        clientButton.clicked += OnPressClient;
    }

    // callbackEvents
    void OnPressHost()
    {
        // set IP to 0.0.0.0 to listen for all
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = "0.0.0.0";

        // start hosting
        networkStarter.StartHost();
        //HexMapEditor.Instance.Load();
        //HexMapEditor.Instance.RequestNation(0);
        gameObject.SetActive(false);
        nationSelector.gameObject.SetActive(true);
    }
    void OnPressClient()
    {
        // select IP
        ipSelection.SetActive(true);
        //HexMapEditor.Instance.Load();
        gameObject.SetActive(false);
        nationSelector.gameObject.SetActive(true);
    }
}
