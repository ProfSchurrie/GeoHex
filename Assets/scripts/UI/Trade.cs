/*
 * Project: GeoHex
 * File: Trade.cs
 * Author: Alexander Kautz
 * Description:
 * Handles the in-game user interface for creating and submitting trade offers.
 * Connects UI input fields (money, resources, rounds, target nation) with the player's
 * networked TradeOffer instance and manages basic menu interactions.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

/// <summary>
/// UI controller responsible for managing the player's trade interface.
/// Gathers input from UI elements, populates the player's <see cref="TradeOffer"/>,
/// and sends the trade request to the server.
/// </summary>
public class Trade : MonoBehaviour
{
    /// <summary>
    /// Root UI element of the trade interface document.
    /// </summary>
    public VisualElement ui;

    /// <summary>
    /// Field for specifying a one-time money transfer amount.
    /// </summary>
    public FloatField offerMoney;

    /// <summary>
    /// Field for specifying the amount of food transferred per round.
    /// </summary>
    public FloatField offerFoodRound;

    /// <summary>
    /// Field for specifying the amount of money transferred per round.
    /// </summary>
    public FloatField offerMoneyRound;

    /// <summary>
    /// Field for specifying the amount of energy transferred per round.
    /// </summary>
    public FloatField offerEnergyRound;

    /// <summary>
    /// Field for specifying how many rounds the trade should last.
    /// </summary>
    public IntegerField rounds;

    /// <summary>
    /// Label displaying the current player's nation name.
    /// </summary>
    public Label myCountryName;

    /// <summary>
    /// Dropdown menu for selecting which nation to trade with.
    /// </summary>
    public DropdownField otherCountry;

    /// <summary>
    /// Button to close the trade window.
    /// </summary>
    public Button exitButton;

    /// <summary>
    /// Button to confirm and submit the trade offer.
    /// </summary>
    public Button confirmButton;

    /// <summary>
    /// Prefab reference for the player's <see cref="TradeOffer"/> object.
    /// Used to store and send the details of the current offer to the server.
    /// </summary>
    [FormerlySerializedAs("tradeInfoPrefab")]
    public TradeOffer tradeOfferPrefab;

    /// <summary>
    /// The numeric ID of the player's nation.
    /// Not synchronized over the network — maintained locally per client.
    /// </summary>
    [SerializeField]
    public int myCountryID;

    /// <summary>
    /// Called when the trade interface becomes active.
    /// Initializes all UI element references and assigns button callbacks.
    /// </summary>
    void OnEnable()
    {
        myCountryID = Player.CurrentPlayer.nation.id.Value;
        ui = GetComponent<UIDocument>().rootVisualElement;

        offerMoney = ui.Q<FloatField>("OfferMoney");
        offerFoodRound = ui.Q<FloatField>("OfferFoodRounds");
        offerMoneyRound = ui.Q<FloatField>("OfferMoneyRounds");
        offerEnergyRound = ui.Q<FloatField>("OfferEnergyRounds");
        rounds = ui.Q<IntegerField>("Rounds");
        myCountryName = ui.Q<Label>("MyCountryName");
        otherCountry = ui.Q<DropdownField>("OtherCountry");

        exitButton = ui.Q<Button>("ExitButton");
        confirmButton = ui.Q<Button>("ConfirmButton");

        // Register UI event handlers
        exitButton.clicked += OnPressExit;
        confirmButton.clicked += OnPressConfirm;
    }

    /// <summary>
    /// Closes the trade interface.
    /// </summary>
    void OnPressExit()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Reads player input from UI fields, updates the player's <see cref="TradeOffer"/>,
    /// and triggers the networked trade request.
    /// </summary>
    void OnPressConfirm()
    {
        TradeOffer tradeOffer = Player.CurrentPlayer.tradeOffer;

        tradeOffer.offeringCountry.Value = myCountryID;
        tradeOffer.offeredMoney.Value = float.Parse(offerMoney.text);
        tradeOffer.offeredFoodRounds.Value = float.Parse(offerFoodRound.text);
        tradeOffer.offeredMoneyRounds.Value = float.Parse(offerMoneyRound.text);
        tradeOffer.offeredEnergyRounds.Value = float.Parse(offerEnergyRound.text);
        tradeOffer.rounds.Value = int.Parse(rounds.text);
        tradeOffer.otherCountry.Value = otherCountry.index + 1;

        tradeOffer.trade();

        // Close the trade window after submitting
        gameObject.SetActive(false);
    }
}