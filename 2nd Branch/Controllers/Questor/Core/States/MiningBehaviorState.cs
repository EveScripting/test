namespace EVESharpCore.Questor.States
{
    public enum MiningBehaviorState
    {
        Default,
        Idle, // --> GotoHomeBookmark
        GotoHomeBookmark, // --> Start
        MakeBookmarkForNextOreRoid, // --> GotoHomeBookmark
        Start, // --> Switch
        Switch, // --> Unloadloot
        UnloadLoot, // --> Arm
        Arm, // --> LocalWatch
        LocalWatch,
        WaitingforBadGuytoGoAway,
        WarpOutStation, // --> FindAsteroidToMine
        FindAsteroidToMine,
        Error,
        Paused,
        Traveler
    }
}