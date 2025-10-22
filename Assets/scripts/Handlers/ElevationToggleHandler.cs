using UnityEngine;
using UnityEngine.UI;

public class ElevationToggleHandler : MonoBehaviour
{
    private Toggle elevationToggle; // Automatically get the Toggle component
    public HexMapEditor hexMapEditor; // Reference to HexMapEditor script

    void Start()
    {
        // Get the Toggle component attached to this GameObject
        elevationToggle = GetComponent<Toggle>();

        // Add a listener to the toggle's value change event
        elevationToggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    void OnToggleValueChanged(bool isOn)
    {
        // If the toggle is on, set the elevation to a predefined value
        // If the toggle is off, set it to 0 or leave unchanged
        if (hexMapEditor != null)
        {
            hexMapEditor.SetApplyElevation(isOn);
        }
    }
}
