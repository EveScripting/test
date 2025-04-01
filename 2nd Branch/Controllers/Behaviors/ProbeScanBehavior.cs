extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Actions;
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
    public class ProbeScanBehavior
    {
        #region Constructors

        private ProbeScanBehavior()
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

        public static bool ChangeProbeScanBehaviorState(ProbeScanBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentProbeScanBehaviorState != _StateToSet)
                {
                    if (_StateToSet == ProbeScanBehaviorState.GotoHomeBookmark)
                    {
                        ESCache.Instance.TaskSetEveAccountAttribute("AbyssalPocketNumber", 0);
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.Idle;
                    }

                    if (_StateToSet == ProbeScanBehaviorState.GotoAbyssalBookmark)
                    {
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.Idle;
                    }

                    if (_StateToSet == ProbeScanBehaviorState.ExecuteMission)
                    {
                        State.CurrentCombatMissionCtrlState = ActionControlState.Start;
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.AtDestination;
                    }

                    Log.WriteLine("New ProbeScanBehaviorState [" + _StateToSet + "]");
                    State.CurrentProbeScanBehaviorState = _StateToSet;
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
            Log.WriteLine("LoadSettings: ProbeScanBehavior");

            HomeBookmarkName =
                (string)CharacterSettingsXml.Element("HomeBookmark") ?? (string)CharacterSettingsXml.Element("HomeBookmark") ??
                (string)CommonSettingsXml.Element("HomeBookmark") ?? (string)CommonSettingsXml.Element("HomeBookmark") ?? "HomeBookmark";
            Log.WriteLine("LoadSettings: ProbeScanBehavior: HomeBookmarkName [" + HomeBookmarkName + "]");
        }

        public static void ProcessState()
        {
            try
            {
                if (!EveryPulse()) return;

                switch (State.CurrentProbeScanBehaviorState)
                {
                    case ProbeScanBehaviorState.Idle:
                        MissionSettings.MissionSafeDistanceFromStructure = 22000;
                        IdleCMBState();
                        break;

                    case ProbeScanBehaviorState.Start:
                        StartCMBState();
                        break;

                    case ProbeScanBehaviorState.Switch:
                        SwitchCMBState();
                        break;

                    case ProbeScanBehaviorState.Arm:
                        ArmCMBState();
                        break;

                    case ProbeScanBehaviorState.LocalWatch:
                        LocalWatchCMBState();
                        break;

                    case ProbeScanBehaviorState.WaitingforBadGuytoGoAway:
                        WaitingFoBadGuyToGoAway();
                        break;

                    case ProbeScanBehaviorState.WarpOutStation:
                        WarpOutBookmarkCMBState();
                        break;

                    case ProbeScanBehaviorState.ExecuteMission:
                        Salvage.LootWhileSpeedTanking = true;
                        ExecuteAbyssalDeadspaceSiteState();
                        break;

                    case ProbeScanBehaviorState.GotoHomeBookmark:
                        GotoHomeBookmarkState();
                        break;

                    case ProbeScanBehaviorState.UnloadLoot:
                        UnloadLootCMBState();
                        break;

                    case ProbeScanBehaviorState.Traveler:
                        TravelerCMBState();
                        break;

                    case ProbeScanBehaviorState.Default:
                        ChangeProbeScanBehaviorState(ProbeScanBehaviorState.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void ArmCMBState()
        {
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
                ChangeProbeScanBehaviorState(ProbeScanBehaviorState.LocalWatch, true);
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
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastAmmoBuyAttempt), DateTime.UtcNow);
                        ControllerManager.Instance.AddController(new BuyItemsController());
                        return false;
                    }

            return true;
        }

        public static void ClearPerPocketCache()
        {
            return;
        }

        /**
        private static void ProcessAlerts()
        {
            TimeSpan ItHasBeenThisLongSinceWeStartedThePocket = Statistics.StartedPocket - DateTime.UtcNow;
            int minutesInPocket = ItHasBeenThisLongSinceWeStartedThePocket.Minutes;
            if (minutesInPocket > (AbyssalPocketWarningSeconds / 60) && !WeHaveBeenInPocketTooLong_WarningSent)
            {
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ABYSSAL_POCKET_TIME, "AbyssalDeadspace: FYI: We have been in pocket number [" + ActionControl.PocketNumber + "] for [" + minutesInPocket + "] min: You might need to check that we arnt stuck."));
                WeHaveBeenInPocketTooLong_WarningSent = true;
                Log.WriteLine("We have been in AbyssalPocket [" + ActionControl.PocketNumber + "] for more than [" + AbyssalPocketWarningSeconds + "] seconds!");
                return;
            }

            return;
        }
        **/

        private static bool EveryPulse()
        {
            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("AbyssalDeadspaceBehavior: CMBEveryPulse: if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)");
                return false;
            }

            if (ESCache.Instance.InWormHoleSpace)
            {
                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("AbyssalDeadspaceBehavior: CMBEveryPulse: if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace)");
                return true;
            }

            if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace)
            {
                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("AbyssalDeadspaceBehavior: CMBEveryPulse: if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace)");
                return true;
            }

            Panic.ProcessState(HomeBookmarkName);

            if (State.CurrentPanicState == PanicState.Resume)
            {
                if (ESCache.Instance.InSpace || ESCache.Instance.InStation)
                {
                    State.CurrentPanicState = PanicState.Normal;
                    State.CurrentTravelerState = TravelerState.Idle;
                    ChangeProbeScanBehaviorState(ProbeScanBehaviorState.GotoAbyssalBookmark);
                    return true;
                }

                return false;
            }

            return true;
        }

        private static void ExecuteAbyssalDeadspaceSiteState()
        {
            if (!ESCache.Instance.InSpace)
            {
                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("AbyssalDeadspaceBehavior: ExecuteAbyssalDeadspaceSiteState: if (!ESCache.Instance.InSpace)");
                return;
            }

            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("AbyssalDeadspaceBehavior: ExecuteAbyssalDeadspaceSiteState: if (ESCache.Instance.CurrentShipsCargo == null)");
                return;
            }

            if (!ESCache.Instance.InAbyssalDeadspace && DateTime.UtcNow > Time.Instance.LastActivateFilamentAttempt.AddSeconds(60))
            {
                Log.WriteLine("ProbeScanBehavior: ExecuteMission: InRegularSpace: Go Home");
                ChangeProbeScanBehaviorState(ProbeScanBehaviorState.GotoHomeBookmark);
                return;
            }

            if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("AbyssalDeadspaceBehavior: ExecuteMission: _actionControl.ProcessState();");

            ActionControl.ProcessState(null, null);

            if (NavigateOnGrid.ChooseNavigateOnGridTargetIds != null)
                NavigateOnGrid.NavigateIntoRange(NavigateOnGrid.ChooseNavigateOnGridTargetIds, "ClearPocket", true);
        }

        private static void GotoHomeBookmarkState()
        {
            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "AbyssalDeadspaceBehaviorState.GotoBase", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());
            }

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: Traveler.TravelToBookmarkName(" + HomeBookmarkName + ");");

            Traveler.TravelToBookmarkName(HomeBookmarkName);

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("HomeBookmark is defined as [" + HomeBookmarkName + "] and should be a bookmark of a station or citadel we can dock at: why are we still in space?!");
                    return;
                }

                Traveler.Destination = null;
                ChangeProbeScanBehaviorState(ProbeScanBehaviorState.Start, true);
            }
        }

        private static void IdleCMBState()
        {
            State.CurrentAgentInteractionState = AgentInteractionState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentDroneControllerState = DroneControllerState.Idle;
            State.CurrentSalvageState = SalvageState.Idle;
            State.CurrentStorylineState = StorylineState.Idle;
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

            ChangeProbeScanBehaviorState(ProbeScanBehaviorState.GotoHomeBookmark);
        }

        public static void InvalidateCache()
        {
            // Method intentionally left empty.
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
                    ChangeProbeScanBehaviorState(ProbeScanBehaviorState.WarpOutStation);
                    return;
                }

                Log.WriteLine("Bad standings pilots in local: We will stay 5 minutes in the station and then we will check if it is clear again");
                ChangeProbeScanBehaviorState(ProbeScanBehaviorState.WaitingforBadGuytoGoAway);
                return;
            }

            if (ESCache.Instance.DirectEve.Me.PVPTimerExist)
            {
                Log.WriteLine("AbyssalSitePrerequisiteCheck: We have pvp timer: waiting");
                return;
            }

            ChangeProbeScanBehaviorState(ProbeScanBehaviorState.WarpOutStation);
        }

        private static bool ResetStatesToDefaults()
        {
            Log.WriteLine("ProbeScanBehavior.ResetStatesToDefaults: start");
            State.CurrentProbeScanBehaviorState = ProbeScanBehaviorState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            State.CurrentTravelerState = TravelerState.AtDestination;
            Log.WriteLine("ProbeScanBehavior.ResetStatesToDefaults: done");
            return true;
        }

        private static void StartCMBState()
        {
            //
            // It takes 20 minutes (potentially) to do an abyssal site: if it is within 25min of Downtime (10:35 evetime) pause
            //
            if (Time.Instance.IsItDuringDowntimeNow)
            {
                Log.WriteLine("AbyssalDeadspaceController: Arm: Downtime is less than 25 minutes from now: Pausing");
                ControllerManager.Instance.SetPause(true);
                return;
            }

            ChangeProbeScanBehaviorState(ProbeScanBehaviorState.Switch);
        }

        private static void SwitchCMBState()
        {
            if (State.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin");
                Arm.SwitchShipsOnly = true;
                Arm.ChangeArmState(ArmState.ActivateScanningShip, true, null);
            }

            if (DebugConfig.DebugArm) Log.WriteLine("CombatMissionBehavior.Switch is Entering Arm.Processstate");
            Arm.ProcessState();

            if (State.CurrentArmState == ArmState.Done)
            {
                Log.WriteLine("Done");
                Arm.SwitchShipsOnly = false;
                Arm.ChangeArmState(ArmState.Idle, true, null);
                ChangeProbeScanBehaviorState(ProbeScanBehaviorState.UnloadLoot);
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
                    if (ESCache.Instance.CachedBookmarks != null && ESCache.Instance.CachedBookmarks.Count > 0)
                    {
                        IEnumerable<DirectBookmark> bookmarks = ESCache.Instance.CachedBookmarks.Where(b => b.LocationId == destination.LastOrDefault()).ToList();
                        if (bookmarks.Any())
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
                        ChangeProbeScanBehaviorState(ProbeScanBehaviorState.Error, true);
                        return;
                    }

                    if (ESCache.Instance.InSpace)
                    {
                        Log.WriteLine("Arrived at destination (in space, Questor stopped)");
                        ChangeProbeScanBehaviorState(ProbeScanBehaviorState.Error, true);
                        return;
                    }

                    Log.WriteLine("Arrived at destination");
                    ChangeProbeScanBehaviorState(ProbeScanBehaviorState.Idle, true);
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

                    ChangeProbeScanBehaviorState(ProbeScanBehaviorState.Arm, true);
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

            ChangeProbeScanBehaviorState(ProbeScanBehaviorState.LocalWatch);
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
                        ChangeProbeScanBehaviorState(ProbeScanBehaviorState.GotoAbyssalBookmark);
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
                            ChangeProbeScanBehaviorState(ProbeScanBehaviorState.GotoAbyssalBookmark);
                            Traveler.Destination = null;
                        }
                    }
                    else
                    {
                        Log.WriteLine("No Bookmark in System");
                        State.CurrentTravelerState = TravelerState.Idle;
                        ChangeProbeScanBehaviorState(ProbeScanBehaviorState.GotoAbyssalBookmark);
                    }

                    return;
                }
            }

            Log.WriteLine("No Bookmark in System");
            ChangeProbeScanBehaviorState(ProbeScanBehaviorState.GotoAbyssalBookmark);
        }

        #endregion Methods
    }
}