extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;

namespace EVESharpCore.Questor.Behaviors
{
    public class MarketCreateNewOrdersBehavior
    {
        #region Fields

        private static DateTime _lastPulse = DateTime.UtcNow.AddDays(-1);

        #endregion Fields

        #region Constructors

        public MarketCreateNewOrdersBehavior()
        {
            ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Methods

        public static bool ChangeMarketCreateNewOrdersBehaviorState(CombatMissionsBehaviorState _CMBStateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentCombatMissionBehaviorState != _CMBStateToSet)
                {
                    if (_CMBStateToSet == CombatMissionsBehaviorState.GotoBase)
                        State.CurrentTravelerState = TravelerState.Idle;

                    Log.WriteLine("New CombatMissionsBehaviorState [" + _CMBStateToSet + "]");
                    State.CurrentCombatMissionBehaviorState = _CMBStateToSet;
                    if (!wait) ProcessState();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        public static void ProcessState()
        {
            try
            {
                if (!CMBEveryPulse()) return;

                if (DebugConfig.DebugCombatMissionsBehavior) Log.WriteLine("State.CurrentCombatMissionBehaviorState is [" + State.CurrentCombatMissionBehaviorState + "]");

                switch (State.CurrentCombatMissionBehaviorState)
                {
                    case CombatMissionsBehaviorState.Idle:
                        IdleCMBState();
                        break;

                    case CombatMissionsBehaviorState.GotoBase:
                        GotoBaseCMBState();
                        break;

                    case CombatMissionsBehaviorState.Traveler:
                        TravelerCMBState();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool CMBEveryPulse()
        {
            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                return false;

            if (Settings.Instance.FinishWhenNotSafe && State.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.GotoNearestStation)
                if (ESCache.Instance.InSpace &&
                    !ESCache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    EntityCache station = null;
                    if (ESCache.Instance.DockableLocations.Any())
                        station = ESCache.Instance.DockableLocations.OrderBy(x => x.Distance).FirstOrDefault();

                    if (station != null)
                    {
                        Log.WriteLine("Station found. Going to nearest station");
                        ChangeMarketCreateNewOrdersBehaviorState(CombatMissionsBehaviorState.GotoNearestStation, true);
                    }
                    else
                    {
                        Log.WriteLine("Station not found. Going back to base");
                        ChangeMarketCreateNewOrdersBehaviorState(CombatMissionsBehaviorState.GotoBase, true);
                    }
                }

            Panic.ProcessState(string.Empty);

            if (State.CurrentPanicState == PanicState.Resume)
            {
                if (ESCache.Instance.InSpace || ESCache.Instance.InStation)
                {
                    State.CurrentPanicState = PanicState.Normal;
                    State.CurrentTravelerState = TravelerState.Idle;
                    ChangeMarketCreateNewOrdersBehaviorState(CombatMissionsBehaviorState.GotoMission);
                    return true;
                }

                return false;
            }

            return true;
        }

        private static void GotoBaseCMBState()
        {
            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "CombatMissionsBehaviorState.GotoBase", 2000, NavigateOnGrid.AvoidBumpingThingsBool());
            }

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: Traveler.TravelHome()");

            Traveler.TravelHome(MissionSettings.AgentToPullNextRegularMissionFrom);

            if (State.CurrentTravelerState == TravelerState.AtDestination && ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: We are at destination");

                Traveler.Destination = null;
                ChangeMarketCreateNewOrdersBehaviorState(CombatMissionsBehaviorState.UnloadLoot, true);
            }
        }

        private static void IdleCMBState()
        {
            if (ESCache.Instance.InSpace)
            {
                ChangeMarketCreateNewOrdersBehaviorState(CombatMissionsBehaviorState.GotoBase);
                return;
            }

            State.CurrentAgentInteractionState = AgentInteractionState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentDroneControllerState = DroneControllerState.Idle;
            State.CurrentSalvageState = SalvageState.Idle;
            State.CurrentStorylineState = StorylineState.Idle;
            State.CurrentTravelerState = TravelerState.AtDestination;
            State.CurrentUnloadLootState = UnloadLootState.Idle;

            ChangeMarketCreateNewOrdersBehaviorState(CombatMissionsBehaviorState.Start);
        }

        private static void ResetStatesToDefaults()
        {
            Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: start");
            State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
            State.CurrentAgentInteractionState = AgentInteractionState.Idle;
            State.CurrentArmState = ArmState.Idle;
            State.CurrentUnloadLootState = UnloadLootState.Idle;
            State.CurrentTravelerState = TravelerState.AtDestination;
            Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: done");
            return;
        }

        private static void TravelerCMBState()
        {
            try
            {
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

                if (State.CurrentTravelerState == TravelerState.AtDestination)
                {
                    if (ESCache.Instance.InSpace)
                    {
                        Log.WriteLine("Arrived at destination (in space, Questor stopped)");
                        ChangeMarketCreateNewOrdersBehaviorState(CombatMissionsBehaviorState.Error, true);
                        return;
                    }

                    Log.WriteLine("Arrived at destination");
                    ChangeMarketCreateNewOrdersBehaviorState(CombatMissionsBehaviorState.Idle, true);
                    return;
                }

                Traveler.ProcessState();
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        #endregion Methods
    }
}