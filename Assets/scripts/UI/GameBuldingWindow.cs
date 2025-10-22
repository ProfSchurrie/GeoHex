using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameBuldingWindow : MonoBehaviour
{
    public static GameBuldingWindow Instance { get; private set; }
    public VisualElement ui;
    public Button exitButton;
    public RadioButtonGroup buildingSelect;
    public Label costLabel;

    // Inspector variables
    [SerializeField]
    public HexMapEditor editor;
    [SerializeField]
    public List<Buildable> objects;

    private void Awake()
    {
        Instance = this;
    }

    // Unity Function
    private void OnEnable()
    {
        getElements();
    }

    // private functions
    private void getElements()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;
        exitButton = ui.Q<Button>("ExitButton");
        buildingSelect = ui.Q<RadioButtonGroup>("StructureSelect");
        costLabel = ui.Q<Label>("CostLabel");

        // register callbacks
        exitButton.clicked += OnPessExit;
        buildingSelect.RegisterCallback<ChangeEvent<int>>(OnChangeStructures);

        // fill selectable buildings
        List<string> choices = new List<string>();
        foreach (Buildable obj in objects)
        {
            /*if (!Research.Instance.ResearchedHydro() && obj.gameObject.name == "HydroPlant")
            {
                choices.Add("Unavailable");
            }

            if (!Research.Instance.ResearchedGas() && obj.gameObject.name == "GasPlant")
            {
                choices.Add("Unavailable");
            }

            if (!Research.Instance.ResearchedNuclear() && obj.gameObject.name == "Nuclear")
            {
                choices.Add("Unavailable");
            }

            if (!Research.Instance.ResearchedGreenCity() && obj.gameObject.name == "GreenCity")
            {
                choices.Add("Unavailable");
            }*/
            choices.Add(obj.buildingName);
        }
        buildingSelect.choices = choices;

        // reset editor
        ResetEditor();
    }

    // callbackEvents
    void OnPessExit()
    {
        gameObject.SetActive(false);
    }

    void ResetEditor()
    {
        editor.SetApplyElevation(false);
        editor.SetApplyPlantLevel(true);
        editor.SetApplyUrbanLevel(true);
        editor.SetApplyWaterLevel(false);
        editor.SetUrbanLevel(0);
        editor.SetPlantLevel(0);
        editor.SetCountry(-1);
        editor.SetRoadMode(0);
        editor.SetRiverMode(0);
        editor.SetSpecialIndex(0);
    }
    void OnChangeStructures(ChangeEvent<int> evt)
    {
        ResetEditor();
        if (evt.newValue == -1) return;
        if (objects[evt.newValue].gameObject.name == "Village")
        {
            editor.SetUrbanLevel(1);
            editor.SetApplyUrbanLevel(true);
        }

        else if (objects[evt.newValue].gameObject.name == "Town")
        {
            editor.SetUrbanLevel(3);
            editor.SetApplyUrbanLevel(true);
        }
        else if (objects[evt.newValue].gameObject.name == "GreenCity")
        {
            editor.SetUrbanLevel(3);
            editor.SetPlantLevel(3);
            editor.SetApplyPlantLevel(true);
            editor.SetApplyUrbanLevel(true);
        }
        else
        {
            costLabel.text = objects[evt.newValue].goldCost.ToString() + "g";
            editor.SetSpecialIndex(objects[evt.newValue].specialIndex);
        }
    }
}
