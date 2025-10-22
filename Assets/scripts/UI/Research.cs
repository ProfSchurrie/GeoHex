using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Research : MonoBehaviour
{
    public static Research Instance { get; private set; }
    public VisualElement ui;
    public VisualElement buttons;
    public Label nameLabel;
    public Label costLabel;
    public Label effectsLabel;
    public Button exitButton;
    public Button waterButton;
    public Button gasButton;
    public Button nuclearButton;
    public Button betterFarmButton;
    public Button offshoreButton;
    public Button betterSolarButton;
    public Button betterWindButton;
    public Button greenCityButton;
    public Button solarButton;

    // researched stuff ONLY ACCESS THEM VIA GETTER/SETTER
    private bool rWater;
    private bool rGas;
    private bool rNuclear;
    private bool rBetterFarm;
    private bool rBetterSolar;
    private bool rBetterWind;
    private bool rOffshore;
    private bool rGreenCity;
    private bool rSolar;

    [SerializeField]
    List<string> names;
    [SerializeField]
    List<string> costs;
    [SerializeField]
    List<string> effects;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;

        buttons = ui.Q<VisualElement>("Buttons");

        nameLabel = ui.Q<Label>("NameLabel");
        costLabel = ui.Q<Label>("CostLabel");
        effectsLabel = ui.Q<Label>("EffectsLabel");

        exitButton = ui.Q<Button>("ExitButton");
        waterButton = ui.Q<Button>("WaterButton");
        gasButton = ui.Q<Button>("GasButton");
        nuclearButton = ui.Q<Button>("NuclearButton");
        betterFarmButton = ui.Q<Button>("BetterFarmButton");
        offshoreButton = ui.Q<Button>("OffshoreButton");
        betterSolarButton = ui.Q<Button>("BetterSolarButton");
        betterWindButton = ui.Q<Button>("BetterWindButton");
        greenCityButton = ui.Q<Button>("GreenCityButton");
        solarButton = ui.Q<Button>("SolarButton");

        // register callbacks
        exitButton.clicked += OnPressExit;
        waterButton.clicked += OnPressWater;
        gasButton.clicked += OnPressGas;
        nuclearButton.clicked += OnPressNuclear;
        betterFarmButton.clicked += OnPressBetterFarm;
        betterSolarButton.clicked += OnPressBetterSolar;
        betterWindButton.clicked += OnPressBetterWind;
        offshoreButton.clicked += OnPressOffshoreWind;
        greenCityButton.clicked += OnPressGreenCity;
        solarButton.clicked += OnPressSolar;
        waterButton.RegisterCallback<MouseOverEvent>(OnHoverWater);
        gasButton.RegisterCallback<MouseOverEvent>(OnHoverGas);
        nuclearButton.RegisterCallback<MouseOverEvent>(OnHoverNuclear);
        betterFarmButton.RegisterCallback<MouseOverEvent>(OnHoverBetterFarm);
        betterSolarButton.RegisterCallback<MouseOverEvent>(OnHoverBetterSolar);
        betterWindButton.RegisterCallback<MouseOverEvent>(OnHoverBetterWind);
        offshoreButton.RegisterCallback<MouseOverEvent>(OnHoverOffshoreWind);
        greenCityButton.RegisterCallback<MouseOverEvent>(OnHoverGreenCity);
        solarButton.RegisterCallback<MouseOverEvent>(OnHoverSolar);

        // hide all advanced researchs
        betterSolarButton.SetEnabled(false);
    }

    // Update is called once per frame
    void Update()
    {
        // close if esc is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameObject.SetActive(false);
        }
    }

    // press events
    void OnPressExit()
    {
        gameObject.SetActive(false);
    }
    void OnPressWater()
    {
        RWater = true;
    }
    void OnPressGas()
    {
        RGas = true;
    }
    void OnPressNuclear()
    {
        RNuclear = true;
    }
    void OnPressBetterFarm()
    {
        RBetterFarm = true;
    }
    void OnPressBetterSolar()
    {
        RBetterSolar = true;
    }
    void OnPressBetterWind()
    {
        RBetterWind = true;
    }
    void OnPressOffshoreWind()
    {
        ROffshore = true;
    }
    void OnPressGreenCity()
    {
        RGreenCity = true;
    }
    void OnPressSolar()
    {
        RSolar = true;
    }

    // hover events
    void OnHoverWater(MouseOverEvent evt)
    {
        nameLabel.text = names[0].ToString();
        costLabel.text = costs[0].ToString();
        effectsLabel.text = effects[0].ToString();
    }
    void OnHoverGas(MouseOverEvent evt)
    {
        nameLabel.text = names[1].ToString();
        costLabel.text = costs[1].ToString();
        effectsLabel.text = effects[1].ToString();
    }
    void OnHoverNuclear(MouseOverEvent evt)
    {
        nameLabel.text = names[2].ToString();
        costLabel.text = costs[2].ToString();
        effectsLabel.text = effects[2].ToString();
    }
    void OnHoverBetterFarm(MouseOverEvent evt)
    {
        nameLabel.text = names[3].ToString();
        costLabel.text = costs[3].ToString();
        effectsLabel.text = effects[3].ToString();
    }
    void OnHoverBetterSolar(MouseOverEvent evt)
    {
        nameLabel.text = names[4].ToString();
        costLabel.text = costs[4].ToString();
        effectsLabel.text = effects[4].ToString();
    }
    void OnHoverBetterWind(MouseOverEvent evt)
    {
        nameLabel.text = names[5].ToString();
        costLabel.text = costs[5].ToString();
        effectsLabel.text = effects[5].ToString();
    }
    void OnHoverOffshoreWind(MouseOverEvent evt)
    {
        nameLabel.text = names[6].ToString();
        costLabel.text = costs[6].ToString();
        effectsLabel.text = effects[6].ToString();
    }
    void OnHoverGreenCity(MouseOverEvent evt)
    {
        nameLabel.text = names[7].ToString();
        costLabel.text = costs[7].ToString();
        effectsLabel.text = effects[7].ToString();
    }
    void OnHoverSolar(MouseOverEvent evt)
    {
        nameLabel.text = names[8].ToString();
        costLabel.text = costs[8].ToString();
        effectsLabel.text = effects[8].ToString();
    }

    // getter setter
    public bool RWater
    {
        get
        {
            return rWater;
        }
        set
        {
            if (rWater == true) return;
            if (value == false) return;

            float cost = float.Parse(costs[0]);
            float currentGold = Nation.CurrentNation.GetStats().Gold;
            if ( currentGold < cost) return;
            Nation.CurrentNation.changeGold(-cost);

            rWater = true;
            buttons.Remove(waterButton);
        }
    }
    public bool RGas
    {
        get
        {
            return rGas;
        }
        set
        {
            if(rGas == true) return;
            if(value == false) return;

            float cost = float.Parse(costs[1]);
            float currentGold = Nation.CurrentNation.GetStats().Gold;
            if (currentGold < cost) return;
            Nation.CurrentNation.changeGold(-cost);

            rGas = true;
            buttons.Remove(gasButton);
        }
    }
    public bool RNuclear
    {
        get
        {
            return rNuclear;
        }
        set
        {
            if (rNuclear == true) return;
            if (value == false) return;

            float cost = float.Parse(costs[2]);
            float currentGold = Nation.CurrentNation.GetStats().Gold;
            if (currentGold < cost) return;
            Nation.CurrentNation.changeGold(-cost);

            rNuclear = true;
            buttons.Remove(nuclearButton);
        }
    }
    public bool RBetterFarm
    {
        get
        {
            return rBetterFarm;
        }
        set
        {
            if (rBetterFarm == true) return;
            if (value == false) return;

            float cost = float.Parse(costs[3]);
            float currentGold = Nation.CurrentNation.GetStats().Gold;
            if (currentGold < cost) return;
            Nation.CurrentNation.changeGold(-cost);

            rBetterFarm = true;
            buttons.Remove(betterFarmButton);
        }
    }
    public bool RBetterSolar
    {
        get
        {
            return rBetterSolar;
        }
        set
        {
            if (rBetterSolar == true) return;
            if (value == false) return;

            float cost = float.Parse(costs[4]);
            float currentGold = Nation.CurrentNation.GetStats().Gold;
            if (currentGold < cost) return;
            Nation.CurrentNation.changeGold(-cost);

            rBetterSolar = true;
            buttons.Remove(betterSolarButton);
        }
    }
    public bool RBetterWind
    {
        get
        {
            return rBetterWind;
        }
        set
        {
            if (rBetterWind == true) return;
            if (value == false) return;

            float cost = float.Parse(costs[5]);
            float currentGold = Nation.CurrentNation.GetStats().Gold;
            if (currentGold < cost) return;
            Nation.CurrentNation.changeGold(-cost);

            rBetterWind = true;
            buttons.Remove(betterWindButton);
        }
    }
    public bool ROffshore
    {
        get
        {
            return rOffshore;
        }
        set
        {
            if (rOffshore == true) return;
            if (value == false) return;

            float cost = float.Parse(costs[6]);
            float currentGold = Nation.CurrentNation.GetStats().Gold;
            if (currentGold < cost) return;
            Nation.CurrentNation.changeGold(-cost);

            rOffshore = true;
            buttons.Remove(offshoreButton);
        }
    }
    public bool RGreenCity
    {
        get
        {
            return rGreenCity;
        }
        set
        {
            if (rGreenCity == true) return;
            if (value == false) return;

            float cost = float.Parse(costs[7]);
            float currentGold = Nation.CurrentNation.GetStats().Gold;
            if (currentGold < cost) return;
            Nation.CurrentNation.changeGold(-cost);

            rGreenCity = true;
            buttons.Remove(greenCityButton);
        }
    }
    public bool RSolar
    {
        get 
        { 
            return rSolar;
        }
        set
        {
            if (rSolar == true) return;
            if (value == false) return;

            float cost = float.Parse(costs[8]);
            float currentGold = Nation.CurrentNation.GetStats().Gold;
            if (currentGold < cost) return;
            Nation.CurrentNation.changeGold(-cost);

            rSolar = true;
            buttons.Remove(solarButton);
            // show improved solar
            betterSolarButton.SetEnabled(true);
        }
    }
}
