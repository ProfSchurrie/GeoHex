/*
 * Project: GeoHex
 * File: BalancingStats.cs
 * Author: Sören Coremans
 * Description:
 * Central balancing data for buildable structures. Defines a Stats struct
 * capturing build/demolish costs, per-round outputs, CO₂ impact, and
 * terrain effectiveness multipliers. Static presets (Farm, City, etc.)
 * are consumed by gameplay systems when placing or simulating buildings.
 *
 * Conventions:
 *  • Positive values = production/output per simulation round (e.g., +EnergyOutput).
 *  • Negative values = costs/consumption (e.g., -GoldBuildCost, -EnergyOutput during upkeep).
 *  • Effectiveness is a 6-length array mapping to terrain indices:
 *      [0]=Desert, [1]=Grasslands, [2]=Ocean, [3]=Hills, [4]=Forest, [5]=Underwater
 *    Use with HexCell.EffectivenessIndex (which returns 5 when underwater).
 *
 * Notes:
 *  • TimeToBuild, GoldDemolishCost, EnergyDemolishCost, EnergyCostDuringBuild are
 *    currently unused but retained for future features (build time, paid demolition).
 *  • EnergyBuildCost is from a legacy system and is deprecated (to be removed).
 *  • “Round” length elsewhere is 10 seconds; treat outputs as per-round rates.
 */

namespace DefaultNamespace
{
    /// <summary>
    /// Immutable set of balancing parameters for a building.
    /// Sign conventions: positive = output/production, negative = cost/consumption.
    /// </summary>
    public struct Stats
    {
        /// <summary>Gold required to place/build (negative = pay cost).</summary>
        public float GoldBuildCost;

        /// <summary>
        /// (Deprecated) Legacy field from an older energy-build-cost system.
        /// Kept for backward compatibility; not used by current gameplay.
        /// </summary>
        [System.Obsolete("Deprecated legacy field; not used by current gameplay. Remove in future.")]
        public float EnergyBuildCost;

        /// <summary>Food produced per round (negative = food consumed).</summary>
        public float FoodOutput;

        /// <summary>Energy produced per round (negative = energy upkeep).</summary>
        public float EnergyOutput;

        /// <summary>Gold produced per round (negative = gold upkeep).</summary>
        public float GoldOutput;

        /// <summary>CO₂ emitted per round (negative would imply sequestration).</summary>
        public float CO2Output;

        /// <summary>Gold refunded/paid on demolition (currently unused).</summary>
        public float GoldDemolishCost;

        /// <summary>Energy cost/refund on demolition (currently unused).</summary>
        public float EnergyDemolishCost;

        /// <summary>Time to construct, in rounds (currently unused; builds are instant).</summary>
        public float TimeToBuild;

        /// <summary>Energy consumed during construction (currently unused).</summary>
        public float EnergyCostDuringBuild;

        /// <summary>
        /// Terrain effectiveness multipliers:
        /// [0]=Desert, [1]=Grasslands, [2]=Ocean, [3]=Hills, [4]=Forest, [5]=Underwater.
        /// </summary>
        public float[] Effectiveness;
        
        public Stats(int GoldBuildCost, int EnergyBuildCost, int FoodOutput, int EnergyOutput, int GoldOutput, 
            int CO2Output, int GoldDemolishCost, int EnergyDemolishCost, int TimeToBuild, int EnergyCostDuringBuild, 
            float[] Effectiveness)
        {
            this.GoldBuildCost = GoldBuildCost;
            this.EnergyBuildCost = EnergyBuildCost;
            this.FoodOutput = FoodOutput;
            this.EnergyOutput = EnergyOutput;
            this.GoldOutput = GoldOutput;
            this.CO2Output = CO2Output;
            this.GoldDemolishCost = GoldDemolishCost;
            this.EnergyDemolishCost = EnergyDemolishCost;
            this.TimeToBuild = TimeToBuild;
            this.EnergyCostDuringBuild = EnergyCostDuringBuild;
            this.Effectiveness = Effectiveness;
        }
    }
    
    /// <summary>
    /// Static catalog of balancing presets for all building types.
    /// Negative costs indicate payment/consumption; positives are outputs.
    /// </summary>
    public static class BalancingStats
    {
        public static Stats None = new Stats(0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, new float[6]{1f,1f,1f,1f,1f,1f});

        public static Stats Farm = new Stats(-50, -10, 10, -10, 0,
            10, -50, -10, 0, 0, new float[6]{0.3f,1f,0f,0.6f,1f,0f});

        public static Stats ImprovedFarm = new Stats(-100, -10, 40, -25, 0, 
            20, -50, -10, 0, 0, new float[6]{0.3f,1f,0f,0.6f,1f,0f});

        public static Stats Village = new Stats(-250, -10, -20, -20, 5,
            15, -100, -10, 0, 0, new float[6]{1f,1f,0f,1f,1f,0f});

        public static Stats Port = new Stats(-300, -20, -20, -30, 10,
            15, -75, -10, 0, 0, new float[6]{1f,1f,1f,1f,1f,0f});

        public static Stats City = new Stats(-350, -30, -50, -50, 50,
            35, -150, -20, 0, 0, new float[6]{0f,1f,0f,1f,1f,0f});

        public static Stats GreenCity = new Stats(-500, -30, -50, -20, 50,
            5, -150, -10, 0, 0, new float[6]{0f,1f,0f,0f,1f,0f});

        public static Stats GasPowerPlant = new Stats(-50, -10, 0, 50, 0,
            25, -50, -10, 0, 0, new float[6]{1f,1f,0f,1f,1f,0f});

        public static Stats CoalPowerPlant = new Stats(-75, -10, 0, 40, 0,
            30, -50, -10, 0, 0, new float[6]{1f,1f,0f,1f,1f,0f});

        public static Stats WindPark = new Stats(-100, -10, 0, 20, 0, 
            0, -75, -10, 0, 0, new float[6]{0.2f,0.6f,0f,0.7f,0.6f,1f});

        public static Stats ImprovedWindPark = new Stats(-150, -10, 0, 40, 0, 0, -75, -10, 0, 0, new float[6]{0.2f,0.6f,0f,0.7f,0.6f,1f});

        public static Stats SolarPark = new Stats(-250, -10, 0, 30, 0, 
            0, -100, -10, 0, 0, new float[6]{1f,0.8f,0f,0.7f,0.8f,0f});

        public static Stats ImprovedSolarPark = new Stats(-300, -20, 0, 50, 0, 
            0, -100, -10, 0, 0, new float[6]{1f,0.8f,0f,0.7f,0.8f,0f});

        public static Stats HydroPlant = new Stats(-200, -20, 0, 30, 0, 
            0, -50, -10, 0, 0, new float[6]{1f,1f,1f,1f,1f,0f});

        public static Stats NuclearPlant = new Stats(-450, -20, 0, 60, 0, 
            0, -300, -20, 0, 0, new float[6]{1f,1f,0f,1f,1f,0f});

        public static Stats Forrest = new Stats(0, 0, 0, 0, 0, 
            -5, -100, -20, 0, 0, new float[6]{1f,1f,1f,1f,1f,1f});
    }
}