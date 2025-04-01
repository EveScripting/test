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
    public enum ActionState
    {
        LogWhatIsOnGrid,
        MoveTo,
        OrbitEntity,
        MoveToBackground,
        KeepAtRangeToBackground,
        MoveDirection,
        MoveToWreck,
        AbyssalActivate,
        Activate,
        WaitUntilTargeted,
        WaitForWreck,
        WaitForNPCs,
        AbyssalWaitUntilAggressed,
        WaitUntilAggressed,
        ClearPocket,
        ClearWithinWeaponsRangeOnly,
        AddWarpScramblerByName,
        AddWebifierByName,
        AddEcmNpcByName,
        Ecm,
        Kill,
        KillKeepAtRange,
        KillNoNavigateOnGrid,
        PickASingleTargetToKill,
        KillOnce,
        KillByItemId,
        UseDrones,
        KillClosestByName,
        KillClosest,
        Ignore,
        AbyssalLoot,
        Loot,
        LootFactionOnly,
        LootItem,
        Salvage,
        Analyze,
        PutItem,
        DropItem,
        Done,
        ReallyDone,
        SalvageBookmark,
        DebuggingWait,
        ActivateBastion,
        ReloadAll
    }
}