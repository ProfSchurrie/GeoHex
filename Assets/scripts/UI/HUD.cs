using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Net.Sockets;

public class EditorHUD2 : MonoBehaviour
{
    public VisualElement ui;
    public RadioButtonGroup pathsGroup;
    public Toggle pathsRemove;
    public Toggle pathsEnable;
    public VisualElement pathsPanel;
    public DropdownField propertyDropdown;
    public SliderInt propertyValueSlider;
    public Toggle propertyEnable;
    public VisualElement propertyPanel;
    public RadioButtonGroup structuresGroup;
    public Toggle structuresEnable;
    public VisualElement structuresPanel;
    public Button serverButton;
    public Button hostButton;
    public Button clientButton;
    public RadioButtonGroup colorGroup;
    public Toggle colorEnable;
    public VisualElement colorPanel;
    public Button startGameButton;
    public Label populationLable;
    public Label foodLable;
    public Label moneyLable;
    public Label energyLable;
    public Label timeLable;
    public Button researchButton;
    public Button tradeButton;
    public Button farmButton;
    public Button cityButton;
    public Button factoryButton;

    [SerializeField]
    public HexMapEditor editor;
    [SerializeField]
    public StartNetwork networkStarter;
    [SerializeField]
    public GameObject ipSelection;
    [SerializeField]
    public GameObject researchWindow;
    [SerializeField]
    public GameObject tradeWindow;
    [SerializeField]
    public Build buildWindow;
    [SerializeField]
    private List<Vector2Int> PropertyMaxMin;


    [SerializeField]
    public int Population;
    [SerializeField]
    public int Food;
    [SerializeField]
    public int Money;
    [SerializeField]
    public int Time;

    void OnEnable()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;

        hostButton = ui.Q<Button>("HostButton");
        clientButton = ui.Q<Button>("ClientButton");

        pathsGroup = ui.Q<RadioButtonGroup>("PathSelect");
        pathsRemove = ui.Q<Toggle>("PathRemove");
        pathsEnable = ui.Q<Toggle>("PathEnable");
        pathsPanel = ui.Q<VisualElement>("PathPanel");

        propertyDropdown = ui.Q<DropdownField>("PropertySelect");
        propertyValueSlider = ui.Q<SliderInt>("PropertyValue");
        propertyEnable = ui.Q<Toggle>("PropertyEnable");
        propertyPanel = ui.Q<VisualElement>("PropertyPanel");

        structuresGroup = ui.Q<RadioButtonGroup>("StructureSelect");
        structuresEnable = ui.Q<Toggle>("StructuresEnable");
        structuresPanel = ui.Q<VisualElement>("StructurePanel");

        colorGroup = ui.Q<RadioButtonGroup>("ColorGroup");
        colorEnable = ui.Q<Toggle>("ColorEnable");
        colorPanel = ui.Q<VisualElement>("ColorPanel");

        startGameButton = ui.Q<Button>("StartGame");

        populationLable = ui.Q<Label>("PopLabel");
        foodLable = ui.Q<Label>("FoodLabel");
        moneyLable = ui.Q<Label>("MoneyLabel");
        energyLable = ui.Q<Label>("EnergyLabel");
        timeLable = ui.Q<Label>("TimeLabel");

        researchButton = ui.Q<Button>("ResearchButton");
        tradeButton = ui.Q<Button>("TradeButton");
        farmButton = ui.Q<Button>("FarmButton");
        cityButton = ui.Q<Button>("CityButton");
        factoryButton = ui.Q<Button>("FactoryButton");
        // set callbacks
        hostButton.clicked += OnPressHost;
        clientButton.clicked += OnPressClient;

        pathsGroup.RegisterCallback<ChangeEvent<int>>(OnChangePaths);
        pathsRemove.RegisterCallback<ChangeEvent<bool>>(OnChangePathsRemove);
        pathsEnable.RegisterCallback<ChangeEvent<bool>>(OnChangePathsEnable);

        propertyDropdown.RegisterCallback<ChangeEvent<string>>(OnChangeProperty);
        propertyValueSlider.RegisterCallback<ChangeEvent<int>>(OnChangePropertyValue);
        propertyEnable.RegisterCallback<ChangeEvent<bool>>(OnChangePropertyEnable);


        structuresGroup.RegisterCallback<ChangeEvent<int>>(OnChangeStructures);
        structuresEnable.RegisterCallback<ChangeEvent<bool>>(OnChangeStructuresEnable);

        colorGroup.RegisterCallback<ChangeEvent<int>>(OnChangeColor);
        colorEnable.RegisterCallback<ChangeEvent<bool>>(OnChangeColorEnable);

        startGameButton.clicked += OnPressStartGame;
        researchButton.clicked += OnPressResearch;
        tradeButton.clicked += OnPressTrade;
        farmButton.clicked += OnPressFarm;
        cityButton.clicked += OnPressCity;
        factoryButton.clicked += OnPressFactory;
        // initial setup...
        OnChangePathsEnable(ChangeEvent<bool>.GetPooled(true, false));
        OnChangePropertyEnable(ChangeEvent<bool>.GetPooled(true, false));
        OnChangeStructuresEnable(ChangeEvent<bool>.GetPooled(true, false));
        OnChangeColorEnable(ChangeEvent<bool>.GetPooled(true, false));
    }

    private void LateUpdate()
    {
        populationLable.text = Population.ToString();
        if (Nation.CurrentNation != null)
        {
            foodLable.text = Nation.CurrentNation.GetStats().FoodEffectiveness.ToString();
            moneyLable.text = Nation.CurrentNation.GetStats().Gold.ToString();
            energyLable.text = Nation.CurrentNation.GetStats().EnergyEffectiveness.ToString();
            timeLable.text = Nation.CurrentNation.GetTime().ToString();
        }
    }

    void OnPressHost()
    {
        // set IP to 0.0.0.0 to listen for all
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = "0.0.0.0";

        // start hosting
        networkStarter.StartHost();
    }
    void OnPressClient()
    {
        // select IP
        ipSelection.SetActive(true);
    }
    void OnChangePaths(ChangeEvent<int> evt)
    {
        // deactivate previously selected one
        if (evt.previousValue != evt.newValue) switchPath(evt.previousValue, 0);

        // activate the new one
        switchPath(pathsGroup.value, (pathsRemove.value == true) ? 2 : 1);
    }
    void OnChangePathsRemove(ChangeEvent<bool> evt)
    {
        OnChangePaths(ChangeEvent<int>.GetPooled(pathsGroup.value, pathsGroup.value));
    }
    void switchPath(int pathIndex, int value)
    {
        switch(pathIndex)
        {
            default:
                Debug.LogError("Invalid Path Index! Check with the UI!");
                break;
            case 0: // River
                editor.SetRiverMode(value);
                break;
            case 1: // Road
                editor.SetRoadMode(value);
                break;
        }
    }
    void OnChangePathsEnable(ChangeEvent<bool> evt)
    {
        if(pathsEnable.value == true) // activate it
        {
            pathsPanel.style.opacity = 1.0f;
            pathsGroup.SetEnabled(true);
            OnChangePaths(ChangeEvent<int>.GetPooled(0, pathsGroup.value));
        } else // deactivate
        {
            pathsPanel.style.opacity = 0.5f;
            pathsGroup.SetEnabled(false);
            // deactivate the editor
            editor.SetRiverMode(0);
            editor.SetRoadMode(0);
        }
    }

    void OnChangeProperty(ChangeEvent<string> evt)
    {
        // deactivate the old one
        if(evt.previousValue != "") switchProperty(evt.previousValue, false);

        // activate the new one
        if(evt.newValue != "") switchProperty(evt.newValue, true);

        // change the slider 
        if (propertyDropdown.index != -1)
        {
            propertyValueSlider.value = PropertyMaxMin[propertyDropdown.index].x;
            propertyValueSlider.lowValue = PropertyMaxMin[propertyDropdown.index].x;
            propertyValueSlider.highValue = PropertyMaxMin[propertyDropdown.index].y;
        }
    }
    void switchProperty(string propertyname, bool active)
    {
        if (propertyname == "Elevation")
            editor.SetApplyElevation(active);
        else if (propertyname == "Water Level")
            editor.SetApplyWaterLevel(active);
        else if (propertyname == "Urban")
            editor.SetApplyUrbanLevel(active);
        else if (propertyname == "Plant")
            editor.SetApplyPlantLevel(active);
        else
            Debug.LogError("Invalid Property Name! Check with the UI!");
    }
    void OnChangePropertyValue(ChangeEvent<int> evt)
    {
        // set the new value
        switchPropertyValue(propertyDropdown.value, evt.newValue);
    }
    void switchPropertyValue(string propertyname, int value)
    {
        if(propertyname == "Elevation")
            editor.SetElevation(value);
        else if(propertyname == "Water Level")
            editor.SetWaterLevel(value);
        else if(propertyname == "Urban") 
            editor.SetUrbanLevel(value);
        else if(propertyname == "Plant")
            editor.SetPlantLevel(value);
        else
            Debug.LogError("Invalid Property Name! Check with the UI! (Value)");
    }
    void OnChangePropertyEnable(ChangeEvent<bool> evt)
    {
        if (evt.newValue == true) // activate it
        {
            propertyPanel.style.opacity = 1.0f;
            propertyDropdown.SetEnabled(true);
            propertyValueSlider.SetEnabled(true);
            propertyDropdown.index = 0;
        }
        else // deactivate
        {
            propertyPanel.style.opacity = 0.5f;
            propertyDropdown.value = "";
            propertyDropdown.SetEnabled(false);
            propertyValueSlider.SetEnabled(false);            
        }
    }
    
    void OnChangeStructures(ChangeEvent<int> evt)
    {
        switch (evt.newValue)
        {
            default:
                Debug.LogError("Invalid Path Index! Check with the UI!");
                break;
            case 0: // REMOVE
                editor.SetSpecialIndex(0);
                break;
            case 1: // Forrest
                editor.SetSpecialIndex(2);
                break;
            case 2: // Windpark
                editor.SetSpecialIndex(3);
                break;
            case 3: // SolarField
                editor.SetSpecialIndex(1);
                break;
            case 4: // Mountains
                editor.SetSpecialIndex(4);
                break;
            case 5: // Farm
                editor.SetSpecialIndex(5);
                break;
            case 6: // Town
                editor.SetSpecialIndex(6);
                break;
            case 7: // Village
                editor.SetSpecialIndex(7);
                break;
            case 8: // Coal
                editor.SetSpecialIndex(8);
                break;
            case 9: // Gas
                editor.SetSpecialIndex(9);
                break;
            case 10: // Nuclear
                editor.SetSpecialIndex(10);
                break;
        }
    }
    void OnChangeStructuresEnable(ChangeEvent<bool> evt)
    {
        if (structuresEnable.value == true) // activate it
        {
            structuresPanel.style.opacity = 1.0f;
            structuresGroup.SetEnabled(true);
            structuresGroup.value = 0;
        }
        else // deactivate
        {
            structuresPanel.style.opacity = 0.5f;
            structuresGroup.SetEnabled(false);
            // deactivate the editor
            editor.SetSpecialIndex(-1);
        }
    }

    void OnChangeColor(ChangeEvent<int> evt)
    {
        switch(evt.newValue)
        {
            default:
                Debug.LogError("Invalid Path Index! Check with the UI!");
                break;
            case 0: // Green
                editor.SetTerrainTypeIndex(1);
                break;
            case 1: // Blue
                editor.SetTerrainTypeIndex(2);
                break;
            case 2: // White
                editor.SetTerrainTypeIndex(3);
                break;
            case 3: // Yellow
                editor.SetTerrainTypeIndex(0);
                break;
            case 4: // DarkGreen
                editor.SetTerrainTypeIndex(4);
                break;
        }
    }
    void OnChangeColorEnable(ChangeEvent<bool> evt)
    {
        if (colorEnable.value == true) // activate it
        {
            colorPanel.style.opacity = 1.0f;
            colorGroup.SetEnabled(true);
            colorGroup.value = 0;
            //UpdatePaths();
        }
        else // deactivate
        {
            colorPanel.style.opacity = 0.5f;
            colorGroup.SetEnabled(false);
            // deactivate the editor
            editor.SetTerrainTypeIndex(-1);
        }
    }

    void OnPressStartGame()
    {
        startGameButton.SetEnabled(false);
        startGameButton.style.opacity = 0.5f;
        editor.StartGame();
    }

    void OnPressResearch()
    {
        researchWindow.SetActive(true);
    }
    void OnPressTrade()
    {
        tradeWindow.SetActive(true);
    }

    void OnPressFarm()
    {
        buildWindow.EnableFarm();
    }
    
    void OnPressCity()
    {
        buildWindow.EnableTown();
    }

    void OnPressFactory()
    {
        buildWindow.EnableFactory();
    }
}
