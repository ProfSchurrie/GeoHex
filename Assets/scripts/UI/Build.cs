using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Build : MonoBehaviour
{
    public VisualElement ui;
    public VisualElement buttons;
    
    public Button exitButton;
    public Button farmButton;
    public Button improvedFarmButton;
    public Button villageButton;
    public Button cityButton;
    public Button greenCityButton;
    public Button gasPowerPlantButton;
    public Button coalPowerPlantButton;
    public Button windParkButton;
    public Button improvedWindParkButton;
    public Button solarParkButton;
    public Button improvedSolarParkButton;
    public Button hydroPlantButton;
    public Button nuclearPlantButton;

    private bool aFarm = true;
    private bool aTown = true;
    private bool aFactory = true;
    
    public bool researchImprovedFarm;
    public bool researchImprovedWindPark;
    public bool researchImprovedSolarPark;
    public bool researchGreenCity;

    private void OnEnable()
    {
        HexMapEditor.Instance.SetApplyUrbanLevel(false);
        HexMapEditor.Instance.SetApplyPlantLevel(false);
        HexMapEditor.Instance.SetSpecialIndex(-1);
        
        ui = GetComponent<UIDocument>().rootVisualElement;
        
        buttons = ui.Q<VisualElement>("Buttons");
        
        exitButton = ui.Q<Button>("ExitButton");
        farmButton = ui.Q<Button>("FarmButton");
        improvedFarmButton = ui.Q<Button>("ImprovedFarmButton");
        villageButton = ui.Q<Button>("VillageButton");
        cityButton = ui.Q<Button>("CityButton");
        greenCityButton = ui.Q<Button>("GreenCityButton");
        gasPowerPlantButton = ui.Q<Button>("GasPowerPlantButton");
        coalPowerPlantButton = ui.Q<Button>("CoalPowerPlantButton");
        windParkButton = ui.Q<Button>("WindParkButton");
        improvedWindParkButton = ui.Q<Button>("ImprovedWindParkButton");
        solarParkButton = ui.Q<Button>("SolarParkButton");
        improvedSolarParkButton = ui.Q<Button>("ImprovedSolarParkButton");
        hydroPlantButton = ui.Q<Button>("HydroPlantButton");
        nuclearPlantButton = ui.Q<Button>("NuclearPlantButton");
        
        Debug.Log(ui.ToString());
        Debug.Log(buttons.ToString());
        Debug.Log(exitButton.ToString());
        Debug.Log(farmButton.ToString());
        Debug.Log(improvedFarmButton.ToString());
        Debug.Log(villageButton.ToString());
        Debug.Log(cityButton.ToString());
        Debug.Log(greenCityButton.ToString());
        Debug.Log(gasPowerPlantButton.ToString());
        Debug.Log(coalPowerPlantButton.ToString());
        Debug.Log(windParkButton.ToString());
        Debug.Log(improvedWindParkButton.ToString());
        Debug.Log(solarParkButton.ToString());
        Debug.Log(improvedSolarParkButton.ToString());
        Debug.Log(hydroPlantButton.ToString());
        Debug.Log(nuclearPlantButton.ToString());
        
        exitButton.clicked += OnPressExit;
        farmButton.clicked += OnPressFarm;
        improvedFarmButton.clicked += OnPressImprovedFarm;
        villageButton.clicked += OnPressVillage;
        cityButton.clicked += OnPressCity;
        greenCityButton.clicked += OnPressGreenCity;
        gasPowerPlantButton.clicked += OnPressGasPowerPlant;
        coalPowerPlantButton.clicked += OnPressCoalPowerPlant;
        windParkButton.clicked += OnPressWindPark;
        improvedWindParkButton.clicked += OnPressImprovedWindPark;
        solarParkButton.clicked += OnPressSolarPark;
        improvedSolarParkButton.clicked += OnPressImprovedSolarPark;
        hydroPlantButton.clicked += OnPressHydroPlant;
        nuclearPlantButton.clicked += OnPressNuclearPlant;
        
        farmButton.visible = false;
        improvedFarmButton.visible = false;
        villageButton.visible = false;
        cityButton.visible = false;
        greenCityButton.visible = false;
        gasPowerPlantButton.visible = false;
        coalPowerPlantButton.visible = false;
        windParkButton.visible = false;
        improvedWindParkButton.visible = false;
        solarParkButton.visible = false;
        improvedSolarParkButton.visible = false;
        hydroPlantButton.visible = false;
        nuclearPlantButton.visible = false;
    }

    public void EnableFarm()
    {
        gameObject.SetActive(true);
        aFarm = true;
        farmButton.visible = true;
        improvedFarmButton.visible = true;
        if (!researchImprovedFarm)
        {
            improvedFarmButton.SetEnabled(false);
        }
    }

    public void EnableTown()
    {
        gameObject.SetActive(true);
        aTown = true;
        
        villageButton.visible = true;
        cityButton.visible = true;
        greenCityButton.visible = true;
        if (!researchGreenCity)
        {
            greenCityButton.SetEnabled(false);
        }
    }

    public void EnableFactory()
    {
        gameObject.SetActive(true);
        aFactory = true;
        
        gasPowerPlantButton.visible = true;
        coalPowerPlantButton.visible = true;
        windParkButton.visible = true;
        improvedWindParkButton.visible = true;
        if (!researchImprovedWindPark)
        {
            improvedWindParkButton.SetEnabled(false);
        }
        solarParkButton.visible = true;
        improvedSolarParkButton.visible = true;
        if (!researchImprovedSolarPark)
        {
            improvedSolarParkButton.SetEnabled(false);
        }
        hydroPlantButton.visible = true;
        nuclearPlantButton.visible = true;
    }
    
    void OnPressExit()
    {
        gameObject.SetActive(false);
    }
    
    void Update()
    {
        // close if esc is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnPressExit();
        }
    }

    private void OnPressFarm()
    {
        OnPressExit();
    }
    
    private void OnPressImprovedFarm()
    {
        OnPressExit();
    }
    
    private void OnPressVillage()
    {
        HexMapEditor.Instance.SetApplyUrbanLevel(true);
        HexMapEditor.Instance.SetUrbanLevel(1);
        OnPressExit();
    }
    
    private void OnPressCity()
    {
        HexMapEditor.Instance.SetApplyUrbanLevel(true);
        HexMapEditor.Instance.SetUrbanLevel(3);
        OnPressExit();
    }
    
    private void OnPressGreenCity()
    {
        HexMapEditor.Instance.SetApplyUrbanLevel(true);
        HexMapEditor.Instance.SetUrbanLevel(3);
        HexMapEditor.Instance.SetPlantLevel(5);
        OnPressExit();
    }
    
    private void OnPressGasPowerPlant()
    {
        OnPressExit();
    }
    
    private void OnPressCoalPowerPlant()
    {
        OnPressExit();
    }
    
    private void OnPressWindPark()
    {
        HexMapEditor.Instance.SetSpecialIndex(3);
        OnPressExit();
    }
    
    private void OnPressImprovedWindPark()
    {
        OnPressExit();
    }
    
    private void OnPressSolarPark()
    {
        HexMapEditor.Instance.SetSpecialIndex(1);
        OnPressExit();
    }
    
    private void OnPressImprovedSolarPark()
    {
        OnPressExit();
    }
    
    private void OnPressHydroPlant()
    {
        OnPressExit();
    }
    
    private void OnPressNuclearPlant()
    {
        OnPressExit();
    }
}
