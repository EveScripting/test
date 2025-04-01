namespace EVESharpCore.Questor.States
{
    public enum GatherItemsBehaviorState
    {
        Default,
        Idle,
        Start,
        Prerequisites,
        ActivateTransportShip,
        FindStationToGatherItemsFrom,
        LoadItems,
        WaitForItemsToMove,
        LocalWatch,
        WaitingforBadGuytoGoAway,
        WarpOutStation,
        TravelToToLocation,
        TravelToMarketSystem,
        DelayedGotoBase,
        GotoBase,
        UnloadCargoHold,
        Error,
        Paused,
        Panic,
        Traveler,
        Done,
    }
}