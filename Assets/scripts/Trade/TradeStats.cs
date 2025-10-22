/*
 * Project: GeoHex
 * File: TradeStats.cs
 * Author: Sören Coremans
 * Description:
 * Lightweight value object for representing a trade’s per-round terms.
 * Used to bundle recurring transfers of gold, food, and energy over a
 * fixed number of rounds.
 *
 * Conventions:
 *  • Positive values = receiver gains; negative values = receiver pays.
 *  • Rounds are simulation rounds (elsewhere: 1 round = 5 seconds).
 */

public class TradeStats
{
    public float Gold;
    public float Food;
    public float Energy;
    public int Rounds;

    public TradeStats(float gold, float food, float energy, int rounds)
    {
        Gold = gold;
        Food = food;
        Energy = energy;
        Rounds = rounds;
    }
}