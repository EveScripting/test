namespace EVESharpCore.Questor.States
{
    public enum MarketAdjustBehaviorState
    {
        Default,
        Idle,
        Cleanup,
        Start,
        GotoBase,
        LoadOrdersBeforeProcessingBuyOrders,
        LoadOrdersBeforeProcessingSellOrders,
        PullListOfMySellOrders,
        CheckSellOrders,
        PullListOfMyBuyOrders,
        CheckBuyOrders,
        WaitForNextMarketUpdate,
        Error,
        Paused,
        Panic,
        Traveler
    }
}