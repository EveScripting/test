namespace EVESharpCore.Questor.States
{
    public enum IndustryBehaviorState
    {
        Default,
        Idle, // --> GotoHomeBookmark
        GotoHomeBookmark, // --> Start
        Start, // --> Switch
        Switch, // --> Unloadloot
        UnloadLoot, // --> Prepare
        LocalWatch,
        WaitingforBadGuytoGoAway,
        WarpOutStation, // --> ???
        Prepare, // --> GatherInputs
        Error,
        Paused,
        Traveler,
        GoToMarketSystemToGatherInputs,
        BuyMissingInputs, // --> CreateNewJobs
        CreateNewJobs, // --> WaitForJobs
        WaitForJobs, // --> JobsDeliverAll
        JobsDeliverAll, // --> MoveItemsToMarket
        Done
    }
}