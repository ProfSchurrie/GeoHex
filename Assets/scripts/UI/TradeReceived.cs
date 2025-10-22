/*
 * Project: GeoHex
 * File: TradeReceived.cs
 * Author: Alexander Kautz
 * Description:
 * UI component for displaying an incoming trade offer to the player.
 * Shows all details of the associated TradeDeal and provides buttons to
 * accept or decline the proposal. The actual transaction logic is handled
 * by the TradeDeal class.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

/// <summary>
/// UI controller for reviewing and responding to incoming trade offers.
/// Displays the contents of a <see cref="TradeDeal"/> and allows the player
/// to accept or reject it. The acceptance or rejection triggers logic
/// within <see cref="TradeDeal.Accept(bool)"/>.
/// </summary>
public class TradeReceived : MonoBehaviour
{
    /// <summary>
    /// Root UI element of this trade offer window.
    /// </summary>
    public VisualElement ui;

    /// <summary>
    /// Label displaying how many rounds the trade will last.
    /// </summary>
    public Label rounds;

    /// <summary>
    /// Label showing the name or ID of the offering country.
    /// </summary>
    public Label countryName;

    /// <summary>
    /// Label showing the single transfer amount of money in the offer.
    /// </summary>
    public Label offeredMoney;

    /// <summary>
    /// Label showing the amount of food transferred per round.
    /// </summary>
    public Label offeredFoodRounds;

    /// <summary>
    /// Label showing the amount of money transferred per round.
    /// </summary>
    public Label offeredMoneyRounds;

    /// <summary>
    /// Label showing the amount of energy transferred per round.
    /// </summary>
    public Label offeredEnergyRounds;

    /// <summary>
    /// Button for accepting the trade offer.
    /// </summary>
    public Button acceptButton;

    /// <summary>
    /// Button for declining the trade offer.
    /// </summary>
    public Button declineButton;

    /// <summary>
    /// The underlying <see cref="TradeDeal"/> that this window represents.
    /// Contains all trade details and executes the acceptance logic.
    /// </summary>
    [FormerlySerializedAs("tradeInfo")]
    [SerializeField]
    public TradeDeal TradeDeal;

    /// <summary>
    /// Called when the trade window is activated.
    /// Binds all UI elements and populates them with data from the associated <see cref="TradeDeal"/>.
    /// </summary>
    void OnEnable()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;

        offeredMoney = ui.Q<Label>("OfferedMoney");
        offeredFoodRounds = ui.Q<Label>("OfferedFoodRounds");
        offeredMoneyRounds = ui.Q<Label>("OfferedMoneyRounds");
        offeredEnergyRounds = ui.Q<Label>("OfferedEnergyRounds");
        rounds = ui.Q<Label>("Rounds");
        countryName = ui.Q<Label>("CountryName");
        acceptButton = ui.Q<Button>("AcceptButton");
        declineButton = ui.Q<Button>("DeclineButton");

        loadOffer();

        acceptButton.clicked += OnPressAccept;
        declineButton.clicked += OnPressDecline;
    }

    /// <summary>
    /// Loads and displays all offer details from the linked <see cref="TradeDeal"/>.
    /// </summary>
    public void loadOffer()
    {
        Debug.Log("load");
        offeredMoney.text = TradeDeal.offeredMoney.Value.ToString();
        offeredFoodRounds.text = TradeDeal.offeredFoodRounds.Value.ToString();
        offeredMoneyRounds.text = TradeDeal.offeredMoneyRounds.Value.ToString();
        offeredEnergyRounds.text = TradeDeal.offeredEnergyRounds.Value.ToString();
        rounds.text = TradeDeal.rounds.Value.ToString();
        countryName.text = "From Country: " + TradeDeal.offeringCountry.Value.ToString();
    }

    /// <summary>
    /// Called when the player accepts the offer.
    /// Notifies the <see cref="TradeDeal"/> and closes the window.
    /// </summary>
    void OnPressAccept()
    {
        TradeDeal.Accept(true);
        Destroy(gameObject);
    }

    /// <summary>
    /// Called when the player declines the offer.
    /// Notifies the <see cref="TradeDeal"/> and closes the window.
    /// </summary>
    void OnPressDecline()
    {
        TradeDeal.Accept(false);
        Destroy(gameObject);
    }
}
