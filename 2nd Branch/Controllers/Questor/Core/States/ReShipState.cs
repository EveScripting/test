// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

namespace EVESharpCore.Questor.States
{
    public enum ReShipState
    {
        Idle,
        Begin,
        GotoHomeStation,
        DoWeNeedToReShip,
        BoardNewbShipForTraveling, //most of the time this will be a one way trip with this ship
        GotoMarketStation,
        BuildListItemItemsToBuy, //We need to have a list of items we buy when we reship: keep in mind that the fitting may not be enough for this as we may need cap boosters, ammo, combat boosters, etc.
        CheckMarketOrders, //Is item available? Is item under the cieling price set?
        BuyItem,
        ConstructPackagedShip,
        OpenShipHangar,
        RepairShop,
        OnlineAllModules,
        StripFitting,
        LoadSavedFitting,
        Cleanup,
        Done,
        FittingManagerHasFailed,
        MissingShip,
        NoOrderFoundThatMeetsRequirements,
    }
}