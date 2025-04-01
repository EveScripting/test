
namespace EVESharpCore.Questor.States
{
    public enum UnloadLootState
    {
        Idle,
        Begin,
        MoveAmmoItems,
        MoveAmmoItemsFromFleetHangar,
        //MoveMobileTractor,
        //MoveMobileTractorFromFleetHangar,
        MoveMissionCompletionItems,
        //MoveMissionCompletionItemsFromFleetHangar,
        MoveHighTierLoot,
        //MoveHighTierLootFromFleetHangar,
        MoveOre,
        MoveLoot,
        MoveRestOfCargo,
        //MoveLootFromFleetHangar,
        OrganizeItemHangar,
        OrganizeAmmoHangar,
        OrganizeLootHangar,
        Done
    }
}