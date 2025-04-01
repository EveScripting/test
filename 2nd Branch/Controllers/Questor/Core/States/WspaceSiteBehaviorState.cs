namespace EVESharpCore.Questor.States
{
    public enum WspaceSiteBehaviorState
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
        WarpOutStation, // --> GotoBayssalBookmark
        GotoAbyssalBookmark, // --> ActivateAbyssalDeadspace
        ActivateAbyssalDeadspace, // --> ExecuteMission
        ActivateFleetAbyssalFilaments, // --> WeFoundAnAbyssalFilamentGate
        WeFoundAFleetAbyssalFilamentGate, // --> ActivateFleetAbyssalFilamentGate
        ExecuteMission, // --> GotoHomeBookmark
        Error,
        Paused,
        Traveler
    }
}