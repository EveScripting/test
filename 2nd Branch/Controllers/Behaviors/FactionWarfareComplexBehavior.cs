extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Traveller;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.Behaviors
{
    public class FactionWarfareComplexBehavior
    {
        #region Constructors

        public FactionWarfareComplexBehavior()
        {
            ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Fields

        #endregion Fields

        #region Properties

        public static string HomeBookmarkName { get; set; }

        #endregion Properties

        #region Methods

        public static bool ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentFactionWarfareComplexBehaviorState != _StateToSet)
                {
                    if (_StateToSet == FactionWarfareComplexBehaviorState.ExecuteMission)
                    {
                        State.CurrentCombatMissionCtrlState = ActionControlState.Start;
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.AtDestination;
                    }

                    Log.WriteLine("New FactionWarfareComplexBehaviorState [" + _StateToSet + "]");
                    State.CurrentFactionWarfareComplexBehaviorState = _StateToSet;
                    //if (!wait) ProcessState();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            Log.WriteLine("LoadSettings: AbyssalDeadspaceBehavior");

            HomeBookmarkName =
                (string)CharacterSettingsXml.Element("HomeBookmark") ?? (string)CharacterSettingsXml.Element("HomeBookmark") ??
                (string)CommonSettingsXml.Element("HomeBookmark") ?? (string)CommonSettingsXml.Element("HomeBookmark") ?? "HomeBookmark";
            Log.WriteLine("AbyssalDeadspaceBehavior: HomeBookmarkName [" + HomeBookmarkName + "]");
        }

        public static void ProcessState()
        {
            try
            {
                if (!EveryPulse()) return;

                if (DebugConfig.DebugFactionWarfareComplexBehavior) Log.WriteLine("State.CurrentFactionWarfareComplexBehaviorState is [" + State.CurrentFactionWarfareComplexBehaviorState + "]");

                switch (State.CurrentFactionWarfareComplexBehaviorState)
                {
                    case FactionWarfareComplexBehaviorState.Idle:
                        MissionSettings.MissionSafeDistanceFromStructure = 22000;
                        IdleCMBState();
                        break;

                    case FactionWarfareComplexBehaviorState.Start:
                        StartCMBState();
                        break;

                    case FactionWarfareComplexBehaviorState.Switch:
                        SwitchCMBState();
                        break;

                    case FactionWarfareComplexBehaviorState.Arm:
                        ArmCMBState(1);
                        break;

                    case FactionWarfareComplexBehaviorState.LocalWatch:
                        LocalWatchCMBState();
                        break;

                    case FactionWarfareComplexBehaviorState.WaitingforBadGuytoGoAway:
                        WaitingFoBadGuyToGoAway();
                        break;

                    case FactionWarfareComplexBehaviorState.WarpOutStation:
                        WarpOutBookmarkCMBState();
                        break;

                    case FactionWarfareComplexBehaviorState.UnloadLoot:
                        UnloadLootCMBState();
                        break;

                    case FactionWarfareComplexBehaviorState.Traveler:
                        TravelerCMBState();
                        break;

                    case FactionWarfareComplexBehaviorState.Default:
                        ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }
        private static void ArmCMBState(int FilamentsToLoad = 1)
        {
            //if (ESCache.Instance.DirectEve.Session.Structureid.HasValue)
            //{
            //    Log.WriteLine("Pausing: Currently Arm does not work in Citadels, manually arm your ammo, drones, repair, etc and unpause");
            //    State.CurrentAbyssalDeadspaceBehaviorState = AbyssalDeadspaceBehaviorState.LocalWatch;
            //    ControllerManager.Instance.SetPause(true);
            //    return;
            //}

            if (!AttemptToBuyAmmo()) return;

            if (State.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin Arm");
                Arm.ChangeArmState(ArmState.Begin, true, null);
            }

            if (!ESCache.Instance.InStation) return;

            Arm.ProcessState();

            if (State.CurrentArmState == ArmState.NotEnoughAmmo ||
                State.CurrentArmState == ArmState.NotEnoughDrones)
            {
                if (Settings.Instance.BuyAmmo)
                {
                    Log.WriteLine("Armstate [" + State.CurrentArmState + "]: BuyAmmo [" + Settings.Instance.BuyAmmo + "]");
                    ESCache.Instance.EveAccount.LastAmmoBuy.AddDays(-1);
                    Arm.ChangeArmState(ArmState.Done, true, null);
                    return;
                }

                Log.WriteLine("Armstate [" + State.CurrentArmState + "]: BuyAmmo [" + Settings.Instance.BuyAmmo + "]");
                Arm.ChangeArmState(ArmState.NotEnoughAmmo, true, null);
                return;
            }

            if (State.CurrentArmState == ArmState.Done)
            {
                Arm.ChangeArmState(ArmState.Idle, true, null);

                if (Settings.Instance.BuyAmmo && BuyItemsController.CurrentBuyItemsState != BuyItemsState.DisabledForThisSession)
                {
                    BuyItemsController.CurrentBuyItemsState = BuyItemsState.Idle;
                    ControllerManager.Instance.RemoveController(typeof(BuyItemsController));
                }

                State.CurrentDroneControllerState = DroneControllerState.WaitingForTargets;
                ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.LocalWatch, true);
            }
        }

        private static bool AttemptToBuyAmmo()
        {
            if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace || ESCache.Instance.InWormHoleSpace)
                return true;

            if (Settings.Instance.BuyAmmo)
                if (BuyItemsController.CurrentBuyItemsState != BuyItemsState.Done && BuyItemsController.CurrentBuyItemsState != BuyItemsState.DisabledForThisSession)
                    if (DateTime.UtcNow > ESCache.Instance.EveAccount.LastAmmoBuy.AddHours(8) && DateTime.UtcNow > ESCache.Instance.EveAccount.LastBuyLpItemAttempt.AddHours(1))
                    {
                        WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LastAmmoBuyAttempt), DateTime.UtcNow);
                        ControllerManager.Instance.AddController(new BuyItemsController());
                        return false;
                    }

            return true;
        }

        //private static bool WeHaveBeenInPocketTooLong_WarningSent = false;

        public static void ClearPerPocketCache()
        {
            //WeHaveBeenInPocketTooLong_WarningSent = false;
            return;
        }

        private static void ProcessAlerts()
        {
            /**
            TimeSpan ItHasBeenThisLongSinceWeStartedThePocket = Statistics.StartedPocket - DateTime.UtcNow;
            int minutesInPocket = ItHasBeenThisLongSinceWeStartedThePocket.Minutes;
            if (minutesInPocket > (AbyssalPocketWarningSeconds / 60) && !WeHaveBeenInPocketTooLong_WarningSent)
            {
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ABYSSAL_POCKET_TIME, "AbyssalDeadspace: FYI: We have been in pocket number [" + ActionControl.PocketNumber + "] for [" + minutesInPocket + "] min: You might need to check that we arnt stuck."));
                WeHaveBeenInPocketTooLong_WarningSent = true;
                Log.WriteLine("We have been in AbyssalPocket [" + ActionControl.PocketNumber + "] for more than [" + AbyssalPocketWarningSeconds + "] seconds!");
                return;
            }
            **/
            return;
        }

        private static bool EveryPulse()
        {
            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugFactionWarfareComplexBehavior) Log.WriteLine("AbyssalDeadspaceBehavior: CMBEveryPulse: if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)");
                return false;
            }

            if (Time.Instance.LastActivateFilamentAttempt.AddSeconds(15) > DateTime.UtcNow)
                return false;

            //
            // this needs to be adjusted to work in lowsec!
            //
            Panic.ProcessState(string.Empty);

            if (State.CurrentPanicState == PanicState.Resume)
            {
                if (ESCache.Instance.InSpace || ESCache.Instance.InStation)
                {
                    State.CurrentPanicState = PanicState.Normal;
                    State.CurrentTravelerState = TravelerState.Idle;
                    //ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoAbyssalBookmark);
                    //set: travel to next mission state
                    return true;
                }

                if (DebugConfig.DebugFactionWarfareComplexBehavior) Log.WriteLine("FactionWarfareComplexBehavior: CMBEveryPulse: if (State.CurrentPanicState == PanicState.Resume)");
                return false;
            }

            return true;
        }

        private static void ExecuteRunComplexState()
        {
            if (!ESCache.Instance.InSpace)
            {
                if (DebugConfig.DebugFactionWarfareComplexBehavior) Log.WriteLine("FactionWarfareComplexBehavior: ExecuteAbyssalDeadspaceSiteState: if (!ESCache.Instance.InSpace)");
                return;
            }

            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                if (DebugConfig.DebugFactionWarfareComplexBehavior) Log.WriteLine("FactionWarfareComplexBehavior: ExecuteAbyssalDeadspaceSiteState: if (ESCache.Instance.CurrentShipsCargo == null)");
                return;
            }

            if (DebugConfig.DebugFactionWarfareComplexBehavior) Log.WriteLine("FactionWarfareComplexBehavior: NavigateOnGrid.NavigateInFactionWarfareComplex();");

            NavigateOnGrid.NavigateInFactionWarfareComplex();
        }

        private static void IdleCMBState()
        {
            State.CurrentAgentInteractionState = AgentInteractionState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentDroneControllerState = DroneControllerState.Idle;
            State.CurrentSalvageState = SalvageState.Idle;
            State.CurrentStorylineState = StorylineState.Idle;
            //State.CurrentTravelerState = TravelerState.AtDestination;
            State.CurrentUnloadLootState = UnloadLootState.Idle;

            if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
            {
                if (DebugConfig.DebugTargetWrecks)
                    Log.WriteLine("Salvage.OpenWrecks = false;");
                Salvage.OpenWrecks = false;
            }
            else
            {
                Salvage.OpenWrecks = true;
            }

            ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.GotoHomeBookmark);
        }

        public static void InvalidateCache()
        {

        }

        private static void LocalWatchCMBState()
        {
            if (Settings.Instance.UseLocalWatch)
            {
                Time.Instance.LastLocalWatchAction = DateTime.UtcNow;

                if (DebugConfig.DebugArm) Log.WriteLine("Starting: Is LocalSafe check...");
                if (ESCache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    Log.WriteLine("local is clear");
                    ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.WarpOutStation);
                    return;
                }

                Log.WriteLine("Bad standings pilots in local: We will stay 5 minutes in the station and then we will check if it is clear again");
                ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.WaitingforBadGuytoGoAway);
                return;
            }

            if (ESCache.Instance.DirectEve.Me.PVPTimerExist)
            {
                Log.WriteLine("AbyssalSitePrerequisiteCheck: We have pvp timer: waiting");
                return;
            }

            ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.WarpOutStation);
        }

        private static bool ResetStatesToDefaults()
        {
            Log.WriteLine("FactionWarfareComplexBehavior.ResetStatesToDefaults: start");
            State.CurrentAbyssalDeadspaceBehaviorState = AbyssalDeadspaceBehaviorState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            State.CurrentTravelerState = TravelerState.AtDestination;
            Log.WriteLine("FactionWarfareComplexBehavior.ResetStatesToDefaults: done");
            return true;
        }

        private static void StartCMBState()
        {
            //
            // It takes 20 minutes (potentially) to do an abyssal site: if it is within 25min of Downtime (10:35 evetime) pause
            //
            if (Time.Instance.IsItDuringDowntimeNow)
            {
                Log.WriteLine("FactionWarfareComplexController: Arm: Downtime is less than 25 minutes from now: Pausing");
                ControllerManager.Instance.SetPause(true);
                return;
            }

            ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.Switch);
        }

        private static void SwitchCMBState()
        {
            //if (ESCache.Instance.CurrentShipsCargo == null || ESCache.Instance.CurrentShipsCargo.Items == null || ESCache.Instance.ItemHangar == null ||
            //    ESCache.Instance.ItemHangar.Items == null)
            //    return;

            //if (ESCache.Instance.InStation && Settings.Instance.BuyPlex && BuyPlexController.ShouldBuyPlex)
            //{
            //    BuyPlexController.CheckBuyPlex();
            //    ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.Idle);
            //    return;
            //}

            if (State.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin");
                Arm.SwitchShipsOnly = true;
                Arm.ChangeArmState(ArmState.ActivateCombatShip, true, null);
            }

            if (DebugConfig.DebugArm) Log.WriteLine("FactionWarfareComplexBehavior.Switch is Entering Arm.Processstate");
            Arm.ProcessState();

            if (State.CurrentArmState == ArmState.Done)
            {
                Log.WriteLine("Done");
                Arm.SwitchShipsOnly = false;
                Arm.ChangeArmState(ArmState.Idle, true, null);
                ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.UnloadLoot);
            }
        }

        private static void TravelerCMBState()
        {
            try
            {
                if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
                {
                    if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                    Salvage.OpenWrecks = false;
                }

                List<long> destination = ESCache.Instance.DirectEve.Navigation.GetDestinationPath();
                if (destination == null || destination.Count == 0)
                {
                    Log.WriteLine("No destination?");
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                    return;
                }

                if (destination.Count == 1 && destination.FirstOrDefault() == 0)
                    destination[0] = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                if (Traveler.Destination == null || Traveler.Destination.SolarSystemId != destination.LastOrDefault())
                {
                    if (ESCache.Instance.CachedBookmarks != null && ESCache.Instance.CachedBookmarks.Any())
                    {
                        IEnumerable<DirectBookmark> bookmarks = ESCache.Instance.CachedBookmarks.Where(b => b.LocationId == destination.LastOrDefault()).ToList();
                        if (bookmarks.FirstOrDefault() != null && bookmarks.Any())
                        {
                            Traveler.Destination = new BookmarkDestination(bookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault());
                            return;
                        }

                        Log.WriteLine("Destination: [" + ESCache.Instance.DirectEve.Navigation.GetLocation(destination.Last()).Name + "]");
                        long lastSolarSystemInRoute = destination.LastOrDefault();

                        Log.WriteLine("Destination: [" + lastSolarSystemInRoute + "]");
                        Traveler.Destination = new SolarSystemDestination(destination.LastOrDefault());
                        return;
                    }

                    return;
                }

                Traveler.ProcessState();

                if (State.CurrentTravelerState == TravelerState.AtDestination)
                {
                    if (State.CurrentCombatMissionCtrlState == ActionControlState.Error)
                    {
                        Log.WriteLine("an error has occurred");
                        ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.Error, true);
                        return;
                    }

                    if (ESCache.Instance.InSpace)
                    {
                        Log.WriteLine("Arrived at destination (in space, Questor stopped)");
                        ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.Error, true);
                        return;
                    }

                    Log.WriteLine("Arrived at destination");
                    ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.Idle, true);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static void UnloadLootCMBState()
        {
            try
            {
                if (!ESCache.Instance.InStation)
                    return;

                //if (ESCache.Instance.DirectEve.Session.Structureid.HasValue)
                //{
                //    Log.WriteLine("Currently Unloadloot does not work in Citadels, manually move your loot.");
                //    ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.Arm);
                //    return;
                //}

                if (State.CurrentUnloadLootState == UnloadLootState.Idle)
                {
                    Log.WriteLine("UnloadLoot: Begin");
                    State.CurrentUnloadLootState = UnloadLootState.Begin;
                }

                UnloadLoot.ProcessState();

                if (State.CurrentUnloadLootState == UnloadLootState.Done)
                {
                    State.CurrentUnloadLootState = UnloadLootState.Idle;

                    if (State.CurrentCombatState == CombatState.OutOfAmmo)
                        Log.WriteLine("State.CurrentCombatState == CombatState.OutOfAmmo");

                    ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.Arm, true);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        private static void WaitingFoBadGuyToGoAway()
        {
            if (DateTime.UtcNow.Subtract(Time.Instance.LastLocalWatchAction).TotalMinutes <
                Time.Instance.WaitforBadGuytoGoAway_minutes + ESCache.Instance.RandomNumber(1, 3))
                return;

            ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.LocalWatch);
        }

        private static void WarpOutBookmarkCMBState()
        {
            if (ESCache.Instance.EveAccount.OtherToonsAreStillLoggingIn)
            {
                Log.WriteLine("WarpOutBookmarkCMBState: Waiting for other toons to finish logging in before we undock!");
                return;
            }

            if (!string.IsNullOrEmpty(Settings.Instance.UndockBookmarkPrefix))
            {
                IEnumerable<DirectBookmark> warpOutBookmarks = ESCache.Instance.BookmarksByLabel(Settings.Instance.UndockBookmarkPrefix ?? "");
                if (warpOutBookmarks != null && warpOutBookmarks.Any())
                {
                    DirectBookmark warpOutBookmark = warpOutBookmarks.FirstOrDefault(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId && b.Distance < 10000000 && b.Distance > (int)Distances.WarptoDistance);

                    long solarid = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (warpOutBookmark == null)
                    {
                        Log.WriteLine("No Bookmark");
                        State.CurrentTravelerState = TravelerState.Idle;
                        ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.GotoAbyssalBookmark);
                    }
                    else if (warpOutBookmark.LocationId == solarid)
                    {
                        if (Traveler.Destination == null)
                        {
                            Log.WriteLine("Warp at " + warpOutBookmark.Title);
                            Traveler.Destination = new BookmarkDestination(warpOutBookmark);
                        }

                        Traveler.ProcessState();
                        if (State.CurrentTravelerState == TravelerState.AtDestination)
                        {
                            Log.WriteLine("Safe!");
                            State.CurrentTravelerState = TravelerState.Idle;
                            ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.GotoAbyssalBookmark);
                            Traveler.Destination = null;
                        }
                    }
                    else
                    {
                        Log.WriteLine("No Bookmark in System");
                        State.CurrentTravelerState = TravelerState.Idle;
                        ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.GotoAbyssalBookmark);
                    }

                    return;
                }
            }

            Log.WriteLine("No Bookmark in System");
            ChangeFactionWarfareComplexBehaviorState(FactionWarfareComplexBehaviorState.GotoAbyssalBookmark);
        }

        #endregion Methods
    }
}