namespace EVESharpCore.Questor.States
{
    public enum HighSecCombatSignaturesBehaviorState
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
        DoneWithCurrentSite, // --> BookmarkAnomaly
        PickSite, // --> BookmarkAnomaly
        TravelToAnomaly,
        BookmarkAnomaly, // --> ExecuteMission
        ExecuteMission, // --> GotoHomeBookmark
        TravelToTargetSystem,
        Error,
        Paused
    }
}