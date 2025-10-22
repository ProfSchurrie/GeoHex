using UnityEngine;
using UnityEngine.UI;

public class PlantSliderHandler : MonoBehaviour
{
    private Slider elevationSlider; // Automatically get the Slider component
    public HexMapEditor hexMapEditor; // Reference to HexMapEditor script

    void Start()
    {
        // Get the Slider component attached to this GameObject
        elevationSlider = GetComponent<Slider>();

        // Add a listener to the slider's value change event
        elevationSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    void OnSliderValueChanged(float value)
    {
        // Call SetElevation directly with the slider's current value
        if (hexMapEditor != null)
        {
            hexMapEditor.SetPlantLevel(value);
        }
    }
}
