using System;

namespace SharedComponents.EVE.ClientSettings.Mining.Main
{
    public class AnomalyPrioritySetting
    {
        /// The name (or part of the name) used to identify the anomaly type.
        /// E.g., "Omber Deposit", "Arkonor Deposit"
        /// Using a pattern allows matching multiple variations if needed later.
        public string AnomalyNamePattern { get; set; }

        /// User-defined priority. 0 = Ignore, 1 = Highest, 5 = Lowest.
        public int Priority { get; set; }

        // Default constructor for serialization
        public AnomalyPrioritySetting() { }

        public AnomalyPrioritySetting(string pattern, int priority)
        {
            AnomalyNamePattern = pattern;
            Priority = priority;
        }

        public override string ToString()
        {
            return $"'{AnomalyNamePattern}': Priority {Priority}";
        }
    }
}