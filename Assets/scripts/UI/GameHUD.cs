using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }
    public VisualElement ui;
    public Label populationLable;
    public Label foodLable;
    public Label moneyLable;
    public Label energyLable;
    public Label CO2Lable;
    public Label timeLable;
    public Button researchButton;
    public Button tradeButton;
    public Button buildButton;

    // Inspector variables
    [SerializeField]
    public GameObject researchWindow;
    [SerializeField]
    public GameObject tradeWindow;
    [SerializeField]
    public GameObject buildWindow;
    [SerializeField]
    public int Population;


    // Unity Function
    private void Awake()
    {
        getElements();
        Instance = this;
    }
    private void OnEnable()
    {
        getElements();
    }
    private void LateUpdate()
    {
        populationLable.text = Population.ToString();
        if (Nation.CurrentNation != null)
        {
            foodLable.text = (Nation.CurrentNation.GetStats().FoodEffectiveness * 100).ToString() + "%";
            moneyLable.text = Nation.CurrentNation.GetStats().Gold.ToString();
            energyLable.text = (Nation.CurrentNation.GetStats().EnergyEffectiveness * 100).ToString() + "%";
            CO2Lable.text = HexServer.Instance.TotalCO2().ToString();
            timeLable.text = Nation.CurrentNation.GetTime().ToString();
        }
    }

    // private functions
    private void getElements()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;
        populationLable = ui.Q<Label>("PopLabel");
        foodLable = ui.Q<Label>("FoodLabel");
        moneyLable = ui.Q<Label>("MoneyLabel");
        energyLable = ui.Q<Label>("EnergyLabel");
        CO2Lable = ui.Q<Label>("CO2Label");
        timeLable = ui.Q<Label>("TimeLabel");
        researchButton = ui.Q<Button>("ResearchButton");
        tradeButton = ui.Q<Button>("TradeButton");
        buildButton = ui.Q<Button>("BuildButton");

        // register callbacks
        researchButton.clicked += OnPressResearch;
        tradeButton.clicked += OnPressTrade;
        buildButton.clicked += OnPressBuild;
    }

    // callbackEvents
    void OnPressResearch()
    {
        researchWindow.SetActive(true);
    }
    void OnPressTrade()
    {
        tradeWindow.SetActive(true);
    }
    void OnPressBuild()
    {
        buildWindow.SetActive(true);
    }
}
