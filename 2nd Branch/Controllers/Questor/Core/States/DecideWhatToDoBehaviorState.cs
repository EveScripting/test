namespace EVESharpCore.Questor.States
{
    public enum DecideWhatToDoBehaviorState
    {
        Default,
        Idle,
        IsThereACachedBehaviorWeShouldChoose,
        FindControllersWithMetPrerequisites,
        ChooseController,
        Wait,
        Error,
        Paused,
        Traveler
    }
}