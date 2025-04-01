using System.Collections.Generic;
using System.Linq;

namespace SharedComponents.EVE.ClientSettings.Mining.Main
{
    public class MiningMainSetting
    {
        // --- General ---
        public MiningRole Role { get; set; }
        public string HomeStationBookmarkName { get; set; }

        // --- Location ---
        public MiningLocationType LocationType { get; set; }

        // --- Belt Mining Specific ---
        public BeltMiningOrder BeltOrder { get; set; }

        // --- Anomaly Mining Specific ---
        /// List of known ore anomalies and their user-set priority.
        /// Priority 0 means ignore this anomaly type.
        /// Lower numbers (1) have higher priority.
        public List<AnomalyPrioritySetting> AnomalyPriorities { get; set; }

        // --- Bookmark Mining Specific ---
        public string MiningBookmarkPrefix { get; set; }

        // --- Constructor with Defaults ---
        public MiningMainSetting()
        {
            // Set sensible defaults
            Role = MiningRole.Miner;
            HomeStationBookmarkName = "MiningHome"; // Example name
            LocationType = MiningLocationType.Belt;
            BeltOrder = BeltMiningOrder.TopDown;
            MiningBookmarkPrefix = "Mine:"; // Example prefix

            // Pre-populate with known Ore Anomaly types and a default priority of 0 (ignored)
            AnomalyPriorities = GetDefaultAnomalyPriorities();
        }

        public static List<AnomalyPrioritySetting> GetDefaultAnomalyPriorities(int defaultPriority = 0)
        {
            var anomalyNames = new List<string> {

                // Ore Prospecting Arrays
                "Small Asteroid Cluster",
                "Medium Asteroid Cluster",
                "Large Asteroid Cluster",
                "Enormous Asteroid Cluster",
                "Colossal Asteroid Cluster",
                // Isogen Prospecting Arrays
                "Small Griemeer Deposit",
                "Griemeer Deposit",
                // Mexallon Prospecting Arrays
                "Small Kylixium Deposit",
                "Kylixium Deposit",
                // Megacyte Prospecting Array
                "Small Ueganite Deposit",
                "Ueganite Deposit",
                //Mercoxit (Does not have an official Prospecting Array, just shows up alongside other arrays
                "Small Mercoxit Deposit",
                "Average Mercoxit Deposit",
                "Large Mercoxit Deposit",
                "Enormous Mercoxit Deposit",
                // Nocxium Prospecting Array
                "Small Nocxite Deposit",
                "Nocxite Deposit",
                // Pyerite Prospecting Array
                "Small Mordunium Deposit",
                "Mordunium Deposit",
                // Tritanium Prospecting Array
                "Small Veldspar Deposit",
                "Veldspar Deposit",
                // Zydrine Prospecting Array
                "Small Hezorime Deposit",
                "Hezorime Deposit",
                // Mining Escalation
                "Interstitial Ore Deposit",
                // Common Ore Anomalies
                "Medium Jaspet Deposits",
                "Small Gneiss Deposit",
                "Average Gneiss Deposit",
                "Large Dark Ochre and Gneiss Deposit",
                "Small Dark Ochre and Gneiss Deposit",
                "Average Dark Ochre and Gneiss Deposit",
                "Large Dark Ochre and Gneiss Deposit",
                "Small Crokite and Dark Ochre Deposit",
                "Average Crokite and Dark Ochre Deposit",
                "Large Crokite and Dark Ochre Deposit",
                "Small Crokite, Dark Ochre and Gneiss Deposit",
                "Average Crokite, Dark Ochre and Gneiss Deposit",
                "Large Crokite, Dark Ochre and Gneiss Deposit",
                "Small Hedbergite, Hemorphite and Jaspet Deposit",
                "Average Hedbergite, Hemorphite and Jaspet Deposit",
                "Large Hedbergite, Hemorphite and Jaspet Deposit",
                //Wormhole Space Anoms
                "Common Perimeter Deposit",
                "Ordinary Perimeter Deposit",
                "Average Frontier Deposit",
                "Unexceptional Frontier Deposit",
                "Exceptional Core Deposit",
                "Infrequent Core Deposit",
                "Isolated Core Deposit",
                "Rarified Core Deposit",
                "Uncommon Core Deposit",
                "Unusual Core Deposit",
                "Shattered Debris Field",
                //Pochvon Sites
                "Ubiquitous Mineral Fields"
            };

            return anomalyNames.Select(name => new AnomalyPrioritySetting(name, defaultPriority)).ToList();
        }
    }
}