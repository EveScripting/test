namespace EVESharpCore.Questor.States
{
    public enum SalvageGridBehaviorState
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
        GotoSalvageGridBookmark,
        SalvageGrid,
        //GotoAbyssalBookmark, // --> ActivateAbyssalDeadspace
        //ActivateAbyssalDeadspace, // --> ExecuteMission
        //ActivateFleetAbyssalDeadspace, // --> ExecuteMission
        //ExecuteMission, // --> GotoHomeBookmark
        Error,
        Paused,
        Traveler
    }
}