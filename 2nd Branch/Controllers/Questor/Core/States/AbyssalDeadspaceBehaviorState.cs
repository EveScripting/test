namespace EVESharpCore.Questor.States
{
    public enum AbyssalDeadspaceBehaviorState
    {
        Default,
        Idle, // --> GotoHomeBookmark
        GotoHomeBookmark, // --> Start
        Start, // --> Switch
        Switch, // --> Unloadloot
        UnloadLoot, // --> Arm
        Arm, // --> LocalWatch
        DumpSurveyDatabases,
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