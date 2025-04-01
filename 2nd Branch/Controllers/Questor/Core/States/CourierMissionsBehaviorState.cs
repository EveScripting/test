namespace EVESharpCore.Questor.States
{
    public enum CourierMissionsBehaviorState
    {
        //
        // In Station
        //      Process Missions: In Order
        //          If State: Complete Mission
        //          If State: Pull Items
        //      Pull missions from agents in station
        //          Only Accept the mission if we can fit the courier items in our cargo, otherwise skip for now (but allow us to pull that mission later when we might have more m3?)
        //          If State: Pull Items
        //      if no more missions available to pull or if we are "full" undock
        //
        // In Space
        Default,

        Idle,
        Start,
        CompleteMissions,
        ProcessCourierMissionsInThisStation,

        //ChooseNextAgent,
        TryToAcceptMissions,

        CanWePullMoreMissions,
        TravelForMissions,
        UnloadImplants,
        GotoBase,
        Error,
        CourierMissionArm
    }
}