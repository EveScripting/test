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
using EVESharpCore.Questor.Stats;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.Events;

namespace EVESharpCore.Questor.Behaviors
{
    public class SkillTrainThenLogoffBehavior
    {
        #region Constructors

        public SkillTrainThenLogoffBehavior()
        {
            ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Fields

        #endregion Fields

        #region Properties
        private static string HomeBookmarkName { get; set; }

        #endregion Properties

        #region Methods

        public static bool ChangeSkillTrainThenLogoffBehaviorState(SkillTrainThenLogoffBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentSkillTrainThenLogoffBehaviorState != _StateToSet)
                {
                    if (_StateToSet == SkillTrainThenLogoffBehaviorState.GotoHomeBookmark)
                    {
                        //WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, "AbyssalPocketNumber", 0);
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.Idle;
                    }

                    Log.WriteLine("New SkillTrainThenLogoffBehaviorState [" + _StateToSet + "]");
                    State.CurrentSkillTrainThenLogoffBehaviorState = _StateToSet;
                    if (ESCache.Instance.InStation && !wait) ProcessState();
                    if (State.CurrentSkillTrainThenLogoffBehaviorState == SkillTrainThenLogoffBehaviorState.GotoHomeBookmark) ProcessState();
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
            Log.WriteLine("LoadSettings: SkillTrainThenLogoffBehaviorState");
            HomeBookmarkName =
                (string)CharacterSettingsXml.Element("HomeBookmark") ?? (string)CharacterSettingsXml.Element("HomeBookmark") ??
                (string)CommonSettingsXml.Element("HomeBookmark") ?? (string)CommonSettingsXml.Element("HomeBookmark") ?? "HomeBookmark";
            Log.WriteLine("SkillTrainThenLogoffBehaviorState: HomeBookmarkName [" + HomeBookmarkName + "]");
        }

        public static void ProcessState()
        {
            try
            {
                if (!EveryPulse()) return;

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("State.CurrentAbyssalDeadspaceBehaviorState is [" + State.CurrentAbyssalDeadspaceBehaviorState + "]");

                switch (State.CurrentSkillTrainThenLogoffBehaviorState)
                {
                    case SkillTrainThenLogoffBehaviorState.Idle:
                        MissionSettings.MissionSafeDistanceFromStructure = 22000;
                        IdleCMBState();
                        break;

                    case SkillTrainThenLogoffBehaviorState.Start:
                        StartCMBState();
                        break;

                    case SkillTrainThenLogoffBehaviorState.GotoHomeBookmark:
                        GotoHomeBookmarkState();
                        break;

                    case SkillTrainThenLogoffBehaviorState.Traveler:
                        TravelerCMBState();
                        break;

                    case SkillTrainThenLogoffBehaviorState.Default:
                        ChangeSkillTrainThenLogoffBehaviorState(SkillTrainThenLogoffBehaviorState.Idle, false);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }
        private static bool AttemptToBuyAmmo()
        {
            if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace || ESCache.Instance.InWormHoleSpace)
                return true;

            //if (Settings.Instance.BuyAmmo)
            //    if (BuyAmmoController.CurrentBuyAmmoState != BuyAmmoState.Done && BuyAmmoController.CurrentBuyAmmoState != BuyAmmoState.DisabledForThisSession)
            //        if (DateTime.UtcNow > ESCache.Instance.EveAccount.LastAmmoBuy.AddHours(8) && DateTime.UtcNow > ESCache.Instance.EveAccount.LastBuyLpItemAttempt.AddHours(1))
            //        {
            //            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LastAmmoBuyAttempt), DateTime.UtcNow);
            //            ControllerManager.Instance.AddController(new BuyAmmoController());
            //            return false;
            //        }

            return true;
        }

        public static void ClearPerPocketCache()
        {
            ESCache.Instance.OldAccelerationGateId = null;
            AbyssalSpawn.ClearPerPocketCache();
            return;
        }

        private static bool EveryPulse()
        {
            try
            {
                if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                {
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log.WriteLine("SkillTrainThenLogoffBehaviorState: CMBEveryPulse: if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)");
                    return false;
                }

                if (State.CurrentSkillTrainThenLogoffBehaviorState != SkillTrainThenLogoffBehaviorState.GotoHomeBookmark)
                    Panic.ProcessState(HomeBookmarkName);

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return true;
            }
        }

        private static void GotoHomeBookmarkState()
        {
            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "SkillTrainThenLogoffBehaviorState.GotoBase", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());
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
                ChangeSkillTrainThenLogoffBehaviorState(SkillTrainThenLogoffBehaviorState.Start, false);
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

            ChangeSkillTrainThenLogoffBehaviorState(SkillTrainThenLogoffBehaviorState.GotoHomeBookmark, false);
        }

        public static void InvalidateCache()
        {
        }
        private static void ResetStatesToDefaults()
        {
            Log.WriteLine("AbyssalDeadspaceBehavior.ResetStatesToDefaults: start");
            State.CurrentAbyssalDeadspaceBehaviorState = AbyssalDeadspaceBehaviorState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            State.CurrentTravelerState = TravelerState.AtDestination;
            Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: done");
            return;
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

            if (ESCache.Instance.InStation)
            {
                if (ESCache.Instance.EveAccount.ShouldBeStopped)
                {
                    ESCache.Instance.CloseEveReason = "if (ESCache.Instance.EveAccount.ShouldBeStopped)";
                    ESCache.Instance.BoolCloseEve = true;
                    return;
                }
            }

            ChangeSkillTrainThenLogoffBehaviorState(SkillTrainThenLogoffBehaviorState.Train, false);
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
                        ChangeSkillTrainThenLogoffBehaviorState(SkillTrainThenLogoffBehaviorState.Error, true);
                        return;
                    }

                    if (ESCache.Instance.InSpace)
                    {
                        Log.WriteLine("Arrived at destination (in space, Questor stopped)");
                        ChangeSkillTrainThenLogoffBehaviorState(SkillTrainThenLogoffBehaviorState.Error, false);
                        return;
                    }

                    Log.WriteLine("Arrived at destination");
                    ChangeSkillTrainThenLogoffBehaviorState(SkillTrainThenLogoffBehaviorState.Idle, false);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        #endregion Methods
    }
}