namespace EVESharpCore.Questor.States
{
    public enum SkillTrainThenLogoffBehaviorState
    {
        Default,
        Idle, // --> GotoHomeBookmark
        GotoHomeBookmark, // --> Start
        Start,
        Train,
        Done,
        Error,
        Paused,
        Traveler
    }
}