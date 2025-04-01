namespace EVESharpCore.Questor.States
{
    public enum StorylineState
    {
        Idle,
        Arm,
        BeforeGotoAgent,
        GotoAgent,
        PreAcceptMission,
        DeclineMission,
        AcceptMission,
        ExecuteMission,
        CompleteMission,
        Done,
        BlacklistAgentForThisSession,
        RemoveOffer,
        Statistics,
        BringSpoilsOfWar,
        ReturnToAgent
    }
}