using System;
using System.Collections.Generic;
using System.Linq;

namespace ReforgeHelper
{
    public static class ItemSubtypes
    {
        public static readonly Dictionary<string, List<string>> SubtypeCategories = new()
        {
            // Rings
            ["Ring"] = new List<string>
            {
                "Iron Ring", "Lazuli Ring", "Ruby Ring", "Sapphire Ring", "Topaz Ring",
                "Amethyst Ring", "Emerald Ring", "Pearl Ring", "Prismatic Ring",
                "Gold Ring", "Unset Ring", "Breach Ring"
            },

            // Amulets
            ["Amulet"] = new List<string>
            {
                "Crimson Amulet", "Azure Amulet", "Amber Amulet", "Jade Amulet",
                "Lapis Amulet", "Lunar Amulet", "Stellar Amulet", "Bloodstone Amulet",
                "Solar Amulet", "Gold Amulet"
            },

            // Jewels
            ["Jewel"] = new List<string>
            {
                "Ruby", "Emerald", "Sapphire", "Diamond",
                "Time-Lost Ruby", "Time-Lost Emerald", "Time-Lost Sapphire", "Time-Lost Diamond",
                "Timeless Jewel"
            },

            // Soul Cores
            ["Soul Core"] = new List<string>
            {
                "Soul Core of Tacati", "Soul Core of Opiloti", "Soul Core of Jiquani",
                "Soul Core of Zalatl", "Soul Core of Citaqualotl", "Soul Core of Puhuarte",
                "Soul Core of Tzamoto", "Soul Core of Xopec", "Soul Core of Azcapa",
                "Soul Core of Topotante", "Soul Core of Quipolatl", "Soul Core of Ticaba",
                "Soul Core of Atmohua", "Soul Core of Cholotl", "Soul Core of Zantipi"
            },

            // Waystones
            ["Waystone"] = new List<string>
            {
                "Waystone (Tier 1)", "Waystone (Tier 2)", "Waystone (Tier 3)", "Waystone (Tier 4)",
                "Waystone (Tier 5)", "Waystone (Tier 6)", "Waystone (Tier 7)", "Waystone (Tier 8)",
                "Waystone (Tier 9)", "Waystone (Tier 10)", "Waystone (Tier 11)", "Waystone (Tier 12)",
                "Waystone (Tier 13)", "Waystone (Tier 14)"
            },

            // Relics
            ["Relic"] = new List<string>
            {
                "Urn Relic", "Amphora Relic", "Vase Relic", "Seal Relic",
                "Coffer Relic", "Tapestry Relic", "Incense Relic"
            }
        };

        public static string GetExactSubtype(string itemName)
        {
            foreach (var category in SubtypeCategories)
            {
                var exactMatch = category.Value.FirstOrDefault(subtype => 
                    itemName.Equals(subtype, StringComparison.OrdinalIgnoreCase));
                
                if (exactMatch != null)
                    return exactMatch;
            }

            return string.Empty;
        }

        public static string GetBaseCategory(string itemName)
        {
            foreach (var category in SubtypeCategories)
            {
                if (category.Value.Any(subtype => 
                    itemName.Contains(subtype, StringComparison.OrdinalIgnoreCase)))
                {
                    return category.Key;
                }
            }

            return string.Empty;
        }
    }
}