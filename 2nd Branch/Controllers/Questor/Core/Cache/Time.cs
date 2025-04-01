// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using System;
using System.Collections.Generic;

namespace EVESharpCore.Lookup
{
    public class Time
    {
        #region Constructors

        // This is used to update the session running time counter every x seconds: default is 15 seconds
        public Time()
        {
            TryGroupWeapons = true;
        }

        #endregion Constructors

        #region Fields

        public static int HourInMilliSeconds = 3600000;
        public static int MinuteInMilliSeconds = 60000;
        public static int SecondInMilliSeconds = 1000;
        public static int HalfHourInMilliSeconds = MinuteInMilliSeconds * 30;
        public static int FifteenMinutesInMilliSeconds = MinuteInMilliSeconds * 15;
        public static int TenMinutesInMilliSeconds = MinuteInMilliSeconds * 10;
        public static int FiveMinutesInMilliSeconds = MinuteInMilliSeconds * 10;

        public DateTime NextClearPocketCache = DateTime.UtcNow;

        public int AfterburnerDelay_milliseconds = 3500;

        //
        public int AlignDelay_seconds = 15;

        // Delay between the last align command and the next, units: minutes. Default is 2
        public int ApproachDelay_seconds = 4;

        public int AverageTimeToCompleteAMission_minutes = 40;
        public int AverageTimetoSalvageMultipleMissions_minutes = 40;
        public int BookmarkPocketRetryDelay_seconds = 20;
        public int CheckLocalDelay_seconds = 5;
        public int DefenceDelay_milliseconds = 400;

        public int DelayBetweenSalvagingSessions_minutes = 10;

        public static TimeSpan ItHasBeenThisLongSinceWeStartedThePocket = new TimeSpan();
        //
        public int DockingDelay_seconds = 15;
        public DateTime LastFormFleetAttempt = DateTime.UtcNow;
        /// <summary>
        ///     Modules last activated time
        /// </summary>
        public Dictionary<long, DateTime> LastActivatedTimeStamp = new Dictionary<long, DateTime>();

        //public Dictionary<long, DateTime> LastOverloadToggleTimeStamp = new Dictionary<long, DateTime>();

        public DateTime LastActivateFilamentAttempt = DateTime.UtcNow.AddDays(-1);
        public DateTime LastActivateKeyActivationWindowAttempt = DateTime.UtcNow.AddDays(-1);
        public DateTime LastActivateAbyssalActivationWindowAttempt = DateTime.UtcNow.AddDays(-1);
        public DateTime LastAlignAttempt = DateTime.UtcNow;

        /// <summary>
        ///     Last Booster was injected at... This is used to determine when we can next inject a booster w/o getting a popup
        ///     from eve
        /// </summary>
        public Dictionary<long, DateTime> LastBoosterInjectAttempt = new Dictionary<long, DateTime>();

        /// <summary>
        ///     Modules last changed ammo time
        /// </summary>
        public Dictionary<long, DateTime> LastChangedAmmoTimeStamp = new Dictionary<long, DateTime>();

        /// <summary>
        ///     Modules last Click time (this is used for activating AND deactivating!)
        /// </summary>
        public Dictionary<long, DateTime> LastClickedTimeStamp = new Dictionary<long, DateTime>();
        public DateTime LastDronesNeedToBePulledTimeStamp = DateTime.UtcNow.AddDays(-1);
        public DateTime LastEcmAttempt = DateTime.UtcNow;
        public Dictionary<long, DateTime> LastFleetInvite = new Dictionary<long, DateTime>();
        public Dictionary<long, DateTime> LastFleetMemberTimeStamp = new Dictionary<long, DateTime>();
        public DateTime LastInWarp { get; set; } = DateTime.UtcNow.AddSeconds(-30);
        public DateTime LastJumpAction = DateTime.UtcNow;
        public DateTime LastInJump = DateTime.UtcNow;
        public DateTime LastLocalWatchAction = DateTime.UtcNow;
        public Dictionary<long, DateTime> LastMarketOrderAdjustmentTimeStamp = new Dictionary<long, DateTime>();
        public Dictionary<long, DateTime> LastMarketOrderCheckTimeStamp = new Dictionary<long, DateTime>();
        public DateTime LastOpenDirectionalScanner = DateTime.UtcNow;
        public DateTime LastOpenHangar = DateTime.UtcNow;
        public DateTime LastOpenShipAndProbeScanner = DateTime.UtcNow;

        // This is the delay between docking attempts, units: seconds. Default is 15
        /// <summary>
        ///     Modules last overloaded time
        /// </summary>
        public Dictionary<long, DateTime> LastOverLoadedTimeStamp = new Dictionary<long, DateTime>();

        public DateTime LastPcNameDateTimeUpdated = DateTime.UtcNow.AddDays(-1);
        public DateTime LastRefreshMySkills = DateTime.UtcNow;
        public DateTime LastReloadAllInGameCommandTimeStamp = DateTime.UtcNow.AddDays(-1);
        public DateTime LastTooFarFromGate = DateTime.UtcNow.AddDays(-1);
        public DateTime LastWeaponUnloadToCargo = DateTime.UtcNow.AddDays(-1);
        public Dictionary<long, DateTime> LastReloadAttemptTimeStamp = new Dictionary<long, DateTime>();

        /// <summary>
        ///     Modules last reload time
        /// </summary>
        public Dictionary<long, DateTime> LastReloadedTimeStamp = new Dictionary<long, DateTime>();

        public DateTime LastSkillQueueCheck = DateTime.UtcNow;
        public DateTime LastSkillQueueModification = DateTime.UtcNow;
        public DateTime LastSubscriptionTimeLeftCheckAttempt = DateTime.UtcNow.AddDays(-1);
        public DateTime LastUpdateOfSessionRunningTime;
        public Dictionary<long, DateTime> LastWeaponHasNoAmmoTimeStamp = new Dictionary<long, DateTime>();

        public int LootingDelay_milliseconds = 400;

        // Delay between loot attempts
        public int MaxSalvageMinutesPerPocket = 10;

        // Max Salvage TIme per pocket before moving on to the next pocket. Default is 10 min
        public DateTime MissionBookmarkTimeout = DateTime.UtcNow.AddHours(10);

        public DateTime NextCheckCorpisAtWar = DateTime.UtcNow;

        public int NoGateFoundRetryDelay_seconds = 4;

        public int NosDelay_milliseconds = 220;

        //
        public int PainterDelay_milliseconds = 800;

        public DateTime Started_DateTime { get; set; } = DateTime.UtcNow;

        // This is the delay between target painter activations and should stagger the painters somewhat (purposely)
        public int RecallDronesDelayBetweenRetries = 15;

        /// <summary>
        ///     Reload time per module
        /// </summary>
        public Dictionary<long, long> ReloadTimePerModule = new Dictionary<long, long>();

        //Time between recall commands for drones when attempting to pull drones
        public int ReloadWeaponDelayBeforeUsable_seconds = 12;

        public Random Rnd = new Random();

        // no gate found on grid when executing the activate action, wait this long to see if it appears (lag), units: seconds. Default is 30
        // Delay after reloading before that module is usable again (non-energy weapons), units: seconds. Default is 12
        public int SalvageDelayBetweenActions_milliseconds = 500;
        public double SecondsSinceLastSessionChange => (DateTime.UtcNow - DirectSession.LastSessionChange).TotalSeconds;

        public int SessionRunningTimeUpdate_seconds = 15;
        public DateTime StartedBoosting = DateTime.UtcNow;

        public DateTime StartedTravelToMarketStation = DateTime.UtcNow.AddDays(-1);
        public int SwitchShipsCheck_seconds = 3;

        // Switch Ships Check to see if ship is correct, units: seconds. Default is 7
        public int SwitchShipsDelay_seconds = 3;

        // Switch Ships Delay before retrying, units: seconds. Default is 10
        public int TargetDelay_milliseconds = 800;

        //
        public int TargetsAreFullDelay_seconds = 2;

        public int TravelerExitStationAmIInSpaceYet_seconds = 8;

        public int TravelerInWarpedNextCommandDelay_seconds = 7;
        public int TravelerJumpedGateNextCommandDelay_seconds = 8;
        public int TravelerNoStargatesFoundRetryDelay_seconds = 10;

        public int WaitforBadGuytoGoAway_minutes = 25;

        // Stay docked for this amount of time before checking local again, units: minutes. Default is 5
        public int WarpScrambledNoDelay_seconds = 10;

        // Time after you are no longer warp scrambled to consider it IMPORTANT That you warp soon
        public int WarptoDelay_seconds = 3;

        public int WeaponDelay_milliseconds = 100;

        //
        public int WebDelay_milliseconds = 220;

        //
        public DateTime WehaveMoved = DateTime.UtcNow;

        // Traveler - Exit Station before you are in space delay, units: seconds. Default is 7
        // Traveler is in warp - delay before processing another command, units: seconds. Default is 15
        // Traveler jumped a gate - delay before assuming we have loaded grid, units: seconds. Default is 15
        // Traveler could not find any StarGates, retry when this time has elapsed, units: seconds. Default is 15
        public int WrecksDisappearAfter_minutes = 110;

        // Delay between defence actions
        private DateTime lastDockAction;

        #endregion Fields

        #region Properties

        //
        public static Time Instance { get; } = new Time();

        public bool IsItDuringDowntimeNow
        {
            get
            {
                if (ESCache.Instance.EveAccount.ConnectToTestServer)
                {
                    //
                    // does this need adjustment for daylight savings time?!
                    //
                    if (DateTime.UtcNow.Hour == 4 && DateTime.UtcNow.Minute > 38)
                    {
                        Log.WriteLine("IsItDuringDowntimeNow [" + true + "] ConnectToTestServer [" + ESCache.Instance.EveAccount.ConnectToTestServer + "] if (DateTime.UtcNow.Hour == 5 && DateTime.UtcNow.Minute > 35)");

                        if (ESCache.Instance.EveAccount.IgnoreDowntime)
                        {
                            Log.WriteLine("IsItDuringDowntimeNow: IgnoreDowntime [" + true + "]");
                            return false;
                        }

                        return true;
                    }

                    return false;
                }

                //
                // Broken?
                //
                //if (22 > ESCache.Instance.DirectEve.Me.TimeTillDownTime.TotalMinutes)
                //{
                //    Log.WriteLine("IsItDuringDowntimeNow [" + true + "] if (22 > ESCache.Instance.DirectEve.Me.TimeTillDownTime.TotalMinutes [" + ESCache.Instance.DirectEve.Me.TimeTillDownTime.TotalMinutes + "] ServerShutDownTime [" + ESCache.Instance.DirectEve.Me.ServerShutDownTime + "] )");
                //    return true;
                //}

                if (DateTime.UtcNow.Hour == 10 && DateTime.UtcNow.Minute > 38)
                {
                    Log.WriteLine("IsItDuringDowntimeNow [" + true + "] if (DateTime.UtcNow.Hour == 10 && DateTime.UtcNow.Minute > 38)");

                    if (ESCache.Instance.EveAccount.IgnoreDowntime)
                    {
                        Log.WriteLine("IsItDuringDowntimeNow: IgnoreDowntime [" + true + "]");
                        return false;
                    }

                    return true;
                }

                return false;
            }
        }

        public HashSet<DateTime> RepairLedger = new HashSet<DateTime>();

        public DateTime LastStripFitting = DateTime.UtcNow.AddHours(-1);

        public DateTime LastSetSpeed = DateTime.MinValue;

        public DateTime LastOrbit = DateTime.MinValue;

        public DateTime LastKeepAtRange = DateTime.MinValue;

        public DateTime LastAcceptMissionAttempt = DateTime.MinValue;
        public DateTime LastIndustryJobStart = DateTime.MinValue;

        public DateTime LastCompleteMissionAttempt = DateTime.MinValue;

        public DateTime LastDeclineMissionAttempt = DateTime.MinValue;

        public DateTime LastMessageRetrievalTimeStamp = DateTime.MinValue;

        public DateTime LastMissionCompletionError = DateTime.MinValue;
        //public DateTime LastLoginRewardClaim = DateTime.MinValue;
        //public DateTime LastRewardRedeem = DateTime.MinValue;


        public DateTime LastAlign { get; set; } = DateTime.UtcNow;
        public DateTime LastApproachAction { get; set; } = DateTime.UtcNow;
        public DateTime LastBookmarkAction { get; set;} = DateTime.UtcNow.AddDays(-1);
        public DateTime LastDockAction
        {
            get => lastDockAction;
            set
            {
                TryGroupWeapons = true;
                lastDockAction = value;
            }
        }

        public DateTime LastGroupWeapons { get; set; }
        public DateTime LastDamagedModuleCheck { get; set; }

        // Local Check for bad standings pilots, delay between checks, units: seconds. Default is 5
        public DateTime LastInitiatedWarp { get; set; } = DateTime.UtcNow.AddSeconds(-30);

        public DateTime LastLoggingAction { get; set; }
        public DateTime LastOfflineModuleCheck { get; set; }

        public DateTime LastOnlineAModule { get; set; }
        public DateTime LastPreferredDroneTargetDateTime { get; set; }
        public DateTime LastPreferredPrimaryWeaponTargetDateTime { get; set; }
        public DateTime LastUndockAction { get; set; }

        public DateTime LastActivateAccelerationGate { get; set; } = DateTime.UtcNow.AddDays(-1);

        public DateTime NextActivateModuleAction { get; set; } = DateTime.UtcNow;

        public DateTime NextActivateAccelerationGate { get; set; } = DateTime.UtcNow;

        public DateTime NextActivateKeyActivationWindow { get; set; } = DateTime.UtcNow;
        public DateTime NextActivateCloak { get; set; } = DateTime.UtcNow;
        public DateTime NextActivateModules { get; set; }

        public DateTime NextActivateSmartBomb { get; set; }
        public DateTime NextAlign { get; set; } = DateTime.UtcNow;
        public DateTime NextApproachAction { get; set; }
        public DateTime NextArmAction { get; set; }
        public DateTime NextBastionAction { get; set; }

        public DateTime NextBastionModeDeactivate { get; set; }
        public DateTime LastBastionDeactivate { get; set; }

        public DateTime NextBookmarkAction { get; set; }
        public DateTime NextBookmarkPocketAttempt { get; set; }
        public DateTime NextBuyLpItemAction { get; set; }
        public DateTime NextCheckSessionReady { get; set; } = DateTime.UtcNow;

        public DateTime NextDockAction { get; set; }

        public DateTime NextDroneBayAction { get; set; }

        public DateTime NextGetAgentMissionAction { get; set; }

        public DateTime NextGetBestCombatTarget { get; set; }

        public DateTime NextGetBestDroneTarget { get; set; }

        public DateTime NextJumpAction { get; set; }

        //
        // average time for all missions, all races, all ShipTypes (guestimated)... it is used to determine when to do things like salvage. units: minutes. Default is 30
        // average time it will take to salvage the multiple mission chain we plan on salvaging all in one go.
        // When checking to see if a bookmark needs to be made in a pocket for after mission salvaging this is the delay between retries, units: seconds. Default is 20
        public DateTime NextLoadSettings { get; set; }
        public DateTime NextFleetWindowAction { get; set; }
        public DateTime NextLootAction { get; set; }
        public DateTime NextLPStoreAction { get; set; }
        public DateTime NextMakeActiveTargetAction { get; set; } = DateTime.UtcNow;
        public DateTime NextModuleDisableAutoReload { get; set; }
        public DateTime NextOpenCargoAction { get; set; } = DateTime.UtcNow;
        public DateTime NextOpenContainerInSpaceAction { get; set; }
        public DateTime NextOpenCurrentShipsCargoWindowAction { get; set; }
        public DateTime NextOpenHangarAction { get; set; }
        public DateTime LastStackItemHangarAction { get; set; } = DateTime.UtcNow.AddMinutes(-30);
        public DateTime LastStackLootHangarAction { get; set; } = DateTime.UtcNow.AddMinutes(-30);
        public DateTime LastStackAmmoHangarAction { get; set; } = DateTime.UtcNow.AddMinutes(-30);
        public DateTime LastStackCargoAction { get; set; } = DateTime.UtcNow.AddMinutes(-30);
        public DateTime NextOrbit { get; set; }
        public DateTime NextRepairItemsAction { get; set; } = DateTime.UtcNow.AddMinutes(-1);
        public DateTime LastAlwaysRepairResetNeedRepair { get; set; } = DateTime.UtcNow.AddMinutes(-1);
        public DateTime NextRepModuleAction { get; set; }
        public DateTime NextAssaultDamageControlModuleAction { get; set; }
        public DateTime NextSalvageAction { get; set; }
        public DateTime NextStartupAction { get; set; }
        public DateTime NextStopAction { get; set; }
        public DateTime NextTargetAction { get; set; }
        public DateTime NextTractorBeamAction { get; set; }
        public DateTime NextTravelerAction { get; set; }
        public DateTime NextUndockAction { get; set; }
        public DateTime NextUnlockTargetOutOfRange { get; set; }
        public DateTime NextWarpAction { get; set; }
        public DateTime NextWindowAction { get; set; }
        public DateTime StartTime { get; set; }
        public bool TryGroupWeapons { get; set; }
        public DateTime LastWindowInteraction { get; internal set; }
        public DateTime LastRefreshBookmarksTimeStamp { get; set; } = DateTime.UtcNow.AddDays(-1);
        public DateTime LastLaunchForSelf { get; set; }
        public DateTime LastIndustryNoJobsReadyToDeliver { get; set; }
        public DateTime LastIndustryJobsDeliverAll { get; set; }
        public DateTime LastAssetRefresh { get; set; } = DateTime.UtcNow.AddDays(-1);

        #endregion Properties
    }
}