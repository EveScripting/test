namespace EVESharpCore.Questor.States
{
    public enum SortBlueprintsBehaviorState
    {
        Default,
        Idle, // --> GotoHomeBookmark
        //GotoHomeBookmark, // --> Start
        Start,
        CheckPrerequisites,
        SortItems,
        GoGetCans,
        Done,
        Error,
        Paused,
        Traveler
    }
}