namespace EVESharpCore.Questor.States
{
    public enum TransportItemTypesBehaviorState
    {
        Default,
        Idle,
        Prerequisites,
        ActivateTransportShip,
        CalculateItemsToMove,
        LoadItems,
        WaitForItemsToMove,
        LocalWatch,
        WaitingforBadGuytoGoAway,
        WarpOutStation,
        TravelToToLocation,
        DelayedGotoBase,
        GotoBase,
        UnloadLoot,
        Error,
        Paused,
        Panic,
        Traveler,
        Done,
    }
}