using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class IPAddressSelector : MonoBehaviour
{
    [SerializeField]
    StartNetwork networkStarter;
    VisualElement ui;
    Button abortButton;
    Button connectButton;
    TextField ipField;
    private string defaultText; // will be taken from the ui

    private void OnEnable()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;

        abortButton = ui.Q<Button>("AbortButton");
        connectButton = ui.Q<Button>("ConnectButton");
        ipField = ui.Q<TextField>("IpField");

        // initialize
        if(defaultText != null)ipField.SetValueWithoutNotify(defaultText);

        // events
        abortButton.clicked += OnClickAbort;
        connectButton.clicked += OnClickConnect;
    }

    void OnClickAbort()
    {
        // exit again
        defaultText = ipField.text;

        // close the window
        gameObject.SetActive(false);
    }
    void OnClickConnect()
    {
        // set IP
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = ipField.text;

        // start client
        networkStarter.StartClient();

        // close the window
        gameObject.SetActive(false);
    }
}
