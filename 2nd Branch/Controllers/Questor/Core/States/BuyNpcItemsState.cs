namespace EVESharpCore.Questor.States
{
    public enum BuyNpcItemsState
    {
        Start,
        ActivateTransportShip,
        CheckMarket,
        TravelToNpcMarketStation,
        BuyNpcMarketItems,
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