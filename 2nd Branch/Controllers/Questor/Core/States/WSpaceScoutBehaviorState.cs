namespace EVESharpCore.Questor.States
{
    public enum WSpaceScoutBehaviorState
    {
        Default,
        Idle, // --> GotoHomeBookmark
        GotoHomeBookmark, // --> Start
        Start, // --> Switch
        Switch, // --> Unloadloot
        UnloadLoot, // --> Arm
        Arm, // --> LocalWatch
        IsItSafeToUndock,
        GotoScanningSpot,
        ScanSignatures,
        BookmarkSites,
        Error,

        Paused
        //Traveler // we cant use traveler in w-space. we will need to devise another way to go w-space system to w-space system via wormholes
    }
}