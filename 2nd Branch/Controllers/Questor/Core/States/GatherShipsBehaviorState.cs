namespace EVESharpCore.Questor.States
{
    public enum GatherShipsBehaviorState
    {
        Default,
        Idle,
        Start,
        Prerequisites,
        TravelToToLocation,
        TravelToMarketSystem,
        DelayedGotoBase,
        LeaveShip,
        PickNextShipToGrab,
        ActivateNextShipToMove,
        Error,
        Paused,
        Panic,
        Traveler,
        Done,
    }
}