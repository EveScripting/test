namespace EVESharpCore.Questor.States
{
    public enum MiningMissionCtrlState
    {
        Start,
        ActivateTransportShip,
        ActivateOreMiningShip,
        ActivateIceMiningShip,
        ItemsFoundAndBeingMoved,
        TryToGrabPickupItemsFromHomeStation,
        GotoPickupLocation,
        PickupItem,
        GotoDropOffLocation,
        DropOffItem,
        Idle,
        CompleteMission,
        Done,
        Error,
        Statistics
    }
}