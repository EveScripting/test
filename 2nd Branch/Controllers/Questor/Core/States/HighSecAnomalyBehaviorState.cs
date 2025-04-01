namespace EVESharpCore.Questor.States
{
    public enum HighSecAnomalyBehaviorState
    {
        Default,
        Idle, // --> GotoHomeBookmark
        GotoHomeBookmark, // --> Start
        Start, // --> Switch
        Switch, // --> Unloadloot
        UnloadLoot, // --> Arm
        Arm, // --> LocalWatch
        LocalWatch,
        WaitingforBadGuytoGoAway,
        WarpOutStation, // --> FindAnomaly
        UseScanner, // --> BookmarkAnomaly
        DoneWithCurrentAnomaly, // --> BookmarkAnomaly
        PickAnomaly, // --> BookmarkAnomaly
        TravelToAnomaly,
        BookmarkAnomaly, // --> ExecuteMission
        ExecuteMission, // --> GotoHomeBookmark
        FindSystemWithSignatures,
        TravelToSystemWithSignatures,
        Error,
        Paused,
        Traveler
    }
}