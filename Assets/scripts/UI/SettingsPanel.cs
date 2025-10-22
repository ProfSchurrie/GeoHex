using System;
using Unity.Netcode;
using UnityEngine;

public class SettingsPanel : MonoBehaviour
{
    public void Toggle()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}