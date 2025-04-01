// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

namespace EVESharpCore.Framework.Lookup
{
    public enum Distances : long
    {
        TwoKMeters = 2000,
        TwentyFiveHundredMeters = 2500,
        ThreeKMeters = 3000,
        FiveKMeters = 5000,
        MIN_PROBE_RECOVER_DISTANCE = TwoKMeters,
        ScoopRange = 2499,
        SafeScoopRange = ScoopRange - 700,
        TooCloseToStructure = TwoKMeters,
        SafeDistancefromStructure = ThreeKMeters,
        WarptoDistance = 152000,
        NextPocketDistance = 100000, // If we moved more then 100km, assume next Pocket
        GateActivationRange = TwentyFiveHundredMeters - 1,
        GateActivationRangeWhileCloaked = 1999,
        CloseToGateActivationRange = GateActivationRange + FiveKMeters,
        WayTooClose = -10100, // This is usually used to determine how far inside the 'docking ring' of an acceleration gate we are.
        OrbitDistanceCushion = FiveKMeters,

        // This is used to determine when to stop orbiting or approaching, if not speed tanking (orbit distance + orbitDistanceCushion)
        OptimalRangeCushion = FiveKMeters, // This is used to determine when to stop approaching, if not speed tanking (optimal distance + optimalDistanceCushion)

        InsideThisRangeIsLikelyToBeMostlyFrigates = 9000, // 9k - overall this assumption works, use with caution
        DecloakRange = 1999,
        JumpRange = 2499,
        SafeToCloakDistance = 2001,
        DockingRange = 0,
        MissionWarpLimit = 150000000,

        // Mission bookmarks have a 1.000.000 distance warp-to limit (changed it to 150.000.000 as there are some bugged missions around)
        PanicDistanceToConsiderSafelyWarpedOff = 500000,

        WeCanWarpToStarFromHere = 500000000,
        OnGridWithMe = 999000, //999k by default, was 250
        HalfOfALightYearInAu = OneAu * 63239 / 2,
        DirectioanLScanner14Au       = OneAu * 14,
        FourAu = OneAu * 4,
        TwoHundredAu = OneAu * 200,
        FourHundredAu = OneAu * 400,
        MaxPocketsDistanceKm = 100000,
        HalfAu = OneAu / 2,
        OneAu = 149597870000, // 1 AU - 1 Astronomical Unit = 149 598 000 000 meters
        DirectionalScannerCloseRange = OneAu,
        MaxSlowboatDistanceToNextAsteroid = 65000,
        MaxSlowBoatDistanceToBelt = 30000,

        CloseToSmallSpeedCloud = 20000,
        CloseToMediumSpeedCloud = 40000,
        CloseToLargeSpeedCloud = 45000,

        CloseToSmallDeviantAutomataSuppressor = 20000,
        CloseToMediumDeviantAutomataSuppressor = 48000,
    }
}