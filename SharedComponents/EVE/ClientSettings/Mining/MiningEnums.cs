namespace SharedComponents.EVE.ClientSettings.Mining
{
    public enum MiningRole
    {
        Miner,  // Actively mines asteroids
        Hauler, // Picks up ore from miners/cans (future feature)
        Booster // Provides fleet boosts (future feature)
    }

    public enum MiningLocationType
    {
        Belt,     // Standard Asteroid Belts
        Anomaly,  // Ore Anomalies (Cosmic Signatures/Exploration Sites)
        Bookmark  // Specific location bookmarks
    }

    public enum BeltMiningOrder
    {
        TopDown, // Mine belts in the order they appear in overview/scan (e.g., I-1, I-2, II-1...)
        Random   // Pick a random belt from the available list
    }
}