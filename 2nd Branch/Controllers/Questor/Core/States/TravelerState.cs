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
    public enum InstaStationDockState
    {
        Idle,
        WhereAreWe,
        JustArrivedInSystem,
        JustArrivedAtStation,
        CreateNewDockBookmark,
        Done,
        WaitForTraveler,
        Error
    }

    public enum InstaStationUndockState
    {
        Idle,
        DidWeJustUndock,
        JustUndocked,
        CreateNewUndockBookmark,
        Done,
        WaitForTraveler,
        Error
    }

    public enum TravelerState
    {
        Idle,
        CalculatePath,
        UseInstaUndockBM,
        Traveling,
        AtDestination,
        Error
    }
}