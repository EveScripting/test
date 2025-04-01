
namespace EVESharpCore.Questor.States
{
    public enum LoadItemsToHaulState
    {
        Idle,
        Begin,
        CalcValueOfLootHangar,
        MoveLootHangarItemsToCargo,
        StackItemHangar,
        StackAmmoHangar,
        StackLootHangar,
        Done
    }
}