namespace EVESharpCore.Questor.States
{
    public enum InsuranceFraudBehaviorState
    {
        Default,
        Idle,
        GoHome,
        Start,
        DetermineShipToBuy,
        BuyShip,
        ReadyShip,
        GoToSelfDestructSpot,
        SelfDestruct,
        WaitForPod,
        Error,
        NotEnoughIsk,
        Paused,
    }
}