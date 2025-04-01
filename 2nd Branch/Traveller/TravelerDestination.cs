extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Events;
using SC::SharedComponents.IPC;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using EVESharpCore.Framework.Lookup;
using SharpDX;
using SC::SharedComponents.Utility;
using EVESharpCore.Questor.Combat;

namespace EVESharpCore.Traveller
{
    public class BookmarkDestination : TravelerDestination
    {
        #region Properties

        public long BookmarkId { get; set; }

        #endregion Properties

        #region Constructors

        public BookmarkDestination(DirectBookmark bookmark)
        {
            if (bookmark == null)
            {
                Log.WriteLine("Invalid bookmark destination!");

                SolarSystemId = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                BookmarkId = -1;
                return;
            }

            Log.WriteLine("Destination set to bookmark [" + bookmark.Title + "]!");
            BookmarkId = bookmark.BookmarkId ?? -1;
            if (bookmark.TypeId == (int)TypeID.SolarSystem && bookmark.ItemId.HasValue)
            {
                Log.WriteLine("Bookmark is a solar system bookmark.");
                SolarSystemId = bookmark.ItemId.Value;
            }
            else
            {
                SolarSystemId = bookmark.LocationId ?? -1;
            }
        }

        public BookmarkDestination(long bookmarkId)
            : this(ESCache.Instance.BookmarkById(bookmarkId))
        {
        }

        #endregion Constructors

        #region Methods

        public override bool PerformFinalDestinationTask()
        {
            DirectBookmark bookmark = ESCache.Instance.BookmarkById(BookmarkId);
            bool arrived = PerformFinalDestinationTask(bookmark);

            return arrived;
        }

        internal static bool PerformFinalDestinationTask(DirectBookmark bookmark)
        {
            // The bookmark no longer exists, assume we are not there
            if (bookmark == null)
                return false;

            // Is this a station bookmark?
            if (bookmark.Entity != null && (bookmark.Entity.CategoryId == (int)CategoryID.Station || bookmark.Entity.CategoryId == (int)CategoryID.Citadel))
            {
                bool arrived = NavigateOnGrid.PerformFinalDestinationTask(bookmark.Entity.Id, bookmark.Entity.Name);
                if (arrived)
                    Log.WriteLine("Arrived at bookmark [" + bookmark.Title + "]");

                return arrived;
            }

            if (ESCache.Instance.InStation)
            {
                // We have arrived
                if (bookmark.ItemId.HasValue && (bookmark.ItemId == ESCache.Instance.DirectEve.Session.StationId ||
                                                 bookmark.ItemId == ESCache.Instance.DirectEve.Session.Structureid))
                    return true;

                Log.WriteLine("bookmark ID [" + bookmark.ItemId + "] StationID [" + ESCache.Instance.DirectEve.Session.StationId + "] StructureId [" + ESCache.Instance.DirectEve.Session.Structureid + "]");

                // We are in a station, but not the correct station!
                if (DateTime.UtcNow > Time.Instance.NextUndockAction)
                {
                    if (Undock())
                        Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.RandomNumber(4000, 6000));

                    return false;
                }

                return false;
            }

            if (!ESCache.Instance.InSpace)
                return false;

            if (NextTravelerDestinationAction > DateTime.UtcNow)
                return false;

            UndockAttempts = 0;

            if (ESCache.Instance.UndockBookmark != null && ESCache.Instance.UndockBookmark.IsInCurrentSystem)
            {
                double distanceToUndockBookmark = ESCache.Instance.DistanceFromMe(bookmark.X ?? 0, bookmark.Y ?? 0, bookmark.Z ?? 0);
                if (distanceToUndockBookmark < (int)Distances.WarptoDistance)
                {
                    Log.WriteLine("Arrived at undock bookmark [" + ESCache.Instance.UndockBookmark.Title + "]");
                    ESCache.Instance.UndockBookmark = null;
                }
                else
                {
                    if (ESCache.Instance.UndockBookmark.WarpTo())
                    {
                        Log.WriteLine("Warping to undock bookmark [" + ESCache.Instance.UndockBookmark.Title + "][" +
                                      Math.Round(distanceToUndockBookmark / 1000 / 149598000, 2) + " AU away]");
                        NextTravelerDestinationAction = DateTime.UtcNow.AddSeconds(Time.Instance.TravelerInWarpedNextCommandDelay_seconds);
                        //if (!Combat.ReloadAll(Cache.Instance.EntitiesNotSelf.OrderBy(t => t.Distance).FirstOrDefault(t => t.Distance < (double)Distance.OnGridWithMe))) return false;
                        return false;
                    }
                }
            }

            // This bookmark has no x / y / z, assume we are there.
            if (bookmark.X == -1 || bookmark.Y == -1 || bookmark.Z == -1 || bookmark.X == null || bookmark.Y == null || bookmark.Z == null)
            {
                Log.WriteLine("Arrived at the bookmark [" + bookmark.Title + "][No XYZ]");
                return true;
            }

            double distance = ESCache.Instance.DistanceFromMe(bookmark.X ?? 0, bookmark.Y ?? 0, bookmark.Z ?? 0);
            if (distance < 8000)
            {
                NavigateOnGrid.StopMyShip("Arrived at the bookmark");
                Log.WriteLine("Arrived at the bookmark [" + bookmark.Title + "]");
                return true;
            }

            if (NextTravelerDestinationAction > DateTime.UtcNow)
                return false;

            if (Math.Round(distance / 1000) < (int)Distances.MaxPocketsDistanceKm && ESCache.Instance.AccelerationGates.Count != 0)
            {
                Log.WriteLine("Warp to bookmark in same pocket requested but acceleration gate found delaying.");
                return true;
            }

            string nameOfBookmark = "";
            if (Settings.Instance.EveServerName == "Tranquility") nameOfBookmark = "Encounter";
            if (Settings.Instance.EveServerName == "Serenity") nameOfBookmark = "遭遇战";
            if (string.IsNullOrEmpty(nameOfBookmark)) nameOfBookmark = "Encounter";

            if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                if (MissionSettings.MissionWarpAtDistanceRange != 0 && bookmark.Title.Contains(nameOfBookmark))
                    if (bookmark.WarpTo(MissionSettings.MissionWarpAtDistanceRange * 1000))
                    {
                        Log.WriteLine("Warping to bookmark [" + bookmark.Title + "][" + " At " +
                                      MissionSettings.MissionWarpAtDistanceRange + " km]");
                        return true;
                    }

            if (bookmark.Distance < (int)Distances.WarptoDistance)
            {
                if (DirectEve.Interval(30000)) Log.WriteLine("bm [" + bookmark.Title + "][" + Math.Round(distance / 1000, 2) + " k away] is on grid!");
                bookmark.Approach();
                return false;
            }

            if (bookmark.WarpTo())
            {
                Log.WriteLine("Warping to bookmark [" + bookmark.Title + "][" +
                              Math.Round(distance / 1000 / 149598000, 2) + " AU away]");
                Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(4);
                return false;
            }

            return false;
        }

        #endregion Methods
    }

    public class MissionBookmarkDestination : TravelerDestination
    {
        #region Constructors

        public MissionBookmarkDestination(DirectAgentMissionBookmark bookmark)
        {
            if (bookmark == null)
            {
                if (DateTime.UtcNow > Time.Instance.MissionBookmarkTimeout.AddMinutes(2))
                {
                    Log.WriteLine("MissionBookmarkTimeout [ " + Time.Instance.MissionBookmarkTimeout.ToShortTimeString() +
                                  " ] did not get reset from last usage: resetting it now");
                    Time.Instance.MissionBookmarkTimeout = DateTime.UtcNow.AddYears(1);
                }

                if (!ESCache.Instance.MissionBookmarkTimerSet)
                {
                    ESCache.Instance.MissionBookmarkTimerSet = true;
                    Time.Instance.MissionBookmarkTimeout = DateTime.UtcNow.AddSeconds(10);
                }

                if (DateTime.UtcNow > Time.Instance.MissionBookmarkTimeout) //if CurrentTime is after the TimeOut value, freak out
                {
                    AgentId = -1;
                    Title = null;
                    SolarSystemId = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    const string msg = "TravelerDestination.MissionBookmarkDestination: Invalid mission bookmark! - Lag?! Closing EVE";
                    ESCache.Instance.CloseEveReason = msg;
                    ESCache.Instance.BoolRestartEve = true;
                    return;
                }

                Log.WriteLine("Invalid Mission Bookmark! retrying for another [ " + Math.Round(Time.Instance.MissionBookmarkTimeout.Subtract(DateTime.UtcNow).TotalSeconds, 0) + " ]sec");
                return;
            }

            Log.WriteLine("Destination set to mission bookmark [" + bookmark.Title + "]");
            AgentId = bookmark.AgentId ?? -1;
            Title = bookmark.Title;
            SolarSystemId = bookmark.SolarSystemId ?? -1;
            ESCache.Instance.MissionBookmarkTimerSet = false;
            Traveler.ChangeTravelerState(TravelerState.Idle);
        }

        public MissionBookmarkDestination(int agentId, string title)
            : this(GetMissionBookmark(agentId, title))
        {
        }

        #endregion Constructors

        #region Properties

        public long AgentId { get; set; }

        public string Title { get; set; }

        #endregion Properties

        #region Methods

        public override bool PerformFinalDestinationTask()
        {
            bool arrived = BookmarkDestination.PerformFinalDestinationTask(GetMissionBookmark(AgentId, Title));
            return arrived; // Mission bookmarks have a 1.000.000 distance warp-to limit (changed it to 150.000.000 as there are some bugged missions around)
        }

        private static DirectAgentMissionBookmark GetMissionBookmark(long agentId, string title)
        {
            DirectAgentMission mission = ESCache.Instance.DirectEve.AgentMissions.Find(m => m.AgentId == agentId);
            if (mission == null)
                return null;

            return mission.Bookmarks.Find(b => b.Title.ToLower() == title.ToLower());
        }

        #endregion Methods
    }

    public class SolarSystemDestination : TravelerDestination
    {
        #region Constructors

        public SolarSystemDestination(long solarSystemId)
        {
            try
            {
                DirectSolarSystem SystemWeWantToGoTo = null;
                try
                {
                    SystemWeWantToGoTo = ESCache.Instance.DirectEve.SolarSystems.FirstOrDefault(i => i.Key == solarSystemId).Value;
                }
                catch (Exception) { }

                if (ESCache.Instance.DirectEve.Session.SolarSystem != null)
                {
                    if (SystemWeWantToGoTo != null)
                        Log.WriteLine("We are in [" + ESCache.Instance.DirectEve.Session.SolarSystem.Name + "] Destination set to solarsystem [" + SystemWeWantToGoTo.Name + "][" + solarSystemId + "]");
                    else
                        Log.WriteLine("We are in [" + ESCache.Instance.DirectEve.Session.SolarSystem.Name + "] Destination set to solarsystemid [" + solarSystemId + "] - note: ID not found in list of k-space systems");
                }
            }
            catch (Exception){}

            SolarSystemId = solarSystemId;
        }

        #endregion Constructors

        #region Methods

        public override bool PerformFinalDestinationTask()
        {
            // The destination is the solar system, not the station in the solar system.
            if (ESCache.Instance.InStation && !ESCache.Instance.InSpace)
            {
                if (NextTravelerDestinationAction < DateTime.UtcNow)
                {
                    if (Undock())
                        Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.RandomNumber(4000, 6000));

                    return false;
                }

                // We are not there yet
                return false;
            }

            if (NextTravelerDestinationAction > DateTime.UtcNow)
                return false;

            UndockAttempts = 0;

            //if (!useInstaBookmark()) return false;

            // The task was to get to the solar system, we're there :)
            Log.WriteLine("Arrived in system");
            ESCache.Instance.MissionBookmarkTimerSet = false;
            return true;
        }

        #endregion Methods
    }

    public class StationDestination : TravelerDestination
    {
        #region Methods

        public override bool PerformFinalDestinationTask()
        {
            bool arrived = NavigateOnGrid.PerformFinalDestinationTask(StationId, StationName);
            return arrived;
        }

        #endregion Methods

        #region Constructors

        public StationDestination(long stationId)
        {
            NavigateOnGrid.StationIdToGoto = stationId;
            DirectLocation station = ESCache.Instance.DirectEve.Navigation.GetLocation(stationId);
            if (station == null || !station.ItemId.HasValue || !station.SolarSystemId.HasValue)
            {
                Log.WriteLine("Invalid station id [" + stationId + "]");
                SolarSystemId = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                StationId = -1;
                StationName = "";
                return;
            }

            Log.WriteLine("StationDestination: Destination set to [" + station.Name + "]");
            StationId = stationId;
            StationName = station.Name;
            SolarSystemId = station.SolarSystemId.Value;
        }

        public StationDestination(long solarSystemId, long stationId, string stationName)
        {
            Log.WriteLine("StationDestination: Destination set to [" + stationName + "]");
            SolarSystemId = solarSystemId;
            StationId = stationId;
            StationName = stationName;
        }

        #endregion Constructors

        #region Properties

        public long StationId { get; set; }

        public string StationName { get; set; }

        #endregion Properties
    }

    public abstract class TravelerDestination
    {
        #region Properties

        public long SolarSystemId { get; protected set; }

        public DirectSolarSystem SolarSystem
        {
            get
            {
                if (SolarSystemId != 0)
                {
                    DirectSolarSystem _solarsystem = ESCache.Instance.DirectEve.SolarSystems.FirstOrDefault(i => i.Key == SolarSystemId).Value;
                    return _solarsystem;
                }

                return null;
            }
        }
        #endregion Properties
        #region Fields

        internal static DateTime NextTravelerDestinationAction;
        internal static int UndockAttempts;

        #endregion Fields

        #region Methods

        public static bool Undock(bool AllowPodToUndock = false)
        {
            if (ESCache.Instance.InStation && !ESCache.Instance.InSpace)
            {
                if (!Cleanup.RepairItems()) return false;
                if (DirectEve.Interval(10000)) Log.WriteLine("Repair returned true");

                if (Time.Instance.IsItDuringDowntimeNow && ESCache.Instance.ActiveShip.GivenName.ToLower() == Combat.CombatShipName.ToLower())
                {
                    string msg = "Undock: Downtime is less than 25 minutes from now: Closing";
                    Log.WriteLine(msg);
                    ESCache.Instance.CloseEveReason = msg;
                    ESCache.Instance.BoolCloseEve = true;
                    return false;
                }

                if (DirectEve.Interval(10000)) Log.WriteLine("It is not within 25 min of downtime.");

                try
                {
                    List<NegativeBoosterEffect> tempNegativeBoosterEffects = ESCache.Instance.DirectEve.Me.GetAllNegativeBoosterEffects();
                    if (tempNegativeBoosterEffects.Any())
                    {
                        Log.WriteLine("Negative Booster Effects Found: ");
                        int count = 0;
                        foreach (NegativeBoosterEffect _negativeBoosterEffect in ESCache.Instance.DirectEve.Me.GetAllNegativeBoosterEffects())
                        {
                            count++;
                            Log.WriteLine("[" + count + "][" + _negativeBoosterEffect + "]");
                        }
                    }
                }
                catch (Exception){}


                try
                {
                    List<DirectDgmEffect> tempBoosterEffects = ESCache.Instance.DirectEve.Me.GetAllBoosterEffects();
                    if (tempBoosterEffects.Any())
                    {
                        Log.WriteLine("Booster Effects Found: ");
                        int count = 0;
                        foreach (string _boosterEffect in ESCache.Instance.DirectEve.Me.GetAllBoosterEffects().Select(e => e.EffectName))
                        {
                            count++;
                            Log.WriteLine("[" + count + "][" + _boosterEffect + "]");
                        }
                    }
                }
                catch (Exception){}

                ESCache.Instance.ResetInStationSettingsWhenExitingStation();
                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                {
                    if (!AllowPodToUndock)
                    {
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.ShipType), Group.Capsule);
                        Util.PlayNoticeSound();
                        Util.PlayNoticeSound();
                        Util.PlayNoticeSound();
                        Util.PlayNoticeSound();
                        Log.WriteLine("Notification: We are in a pod. Pausing");
                        ControllerManager.Instance.SetPause();
                        return false;
                    }
                }
                else if (DirectEve.Interval(10000)) Log.WriteLine("We are not in a pod");

                if (ESCache.Instance.DirectEve.Me.IsAtWar && !ESCache.Instance.InWormHoleSpace)
                {
                    Log.WriteLine("We are at war. We do not undock during wars. Pausing");
                    ControllerManager.Instance.SetPause();
                    return false;
                }
                if (DirectEve.Interval(10000)) Log.WriteLine("We are not at war");

                if (ESCache.Instance.DirectEve.Me.CriminalTimerExists)
                {
                    if (!ESCache.Instance.InWormHoleSpace)
                    {
                        Log.WriteLine("Waiting for Criminal time to end before undocking. Criminal [" + ESCache.Instance.DirectEve.Me.CriminalTimerRemainingSeconds + " sec ]");
                        return false;
                    }
                }
                else if (DirectEve.Interval(10000)) Log.WriteLine("No Criminal Timer");


                if (ESCache.Instance.DirectEve.Me.SuspectTimerExists)
                {
                    if (!ESCache.Instance.InWormHoleSpace)
                    {
                        if (!Traveler.IgnoreSuspectTimer)
                        {
                            Log.WriteLine("Waiting for Suspect time to end before undocking. Suspect [" + ESCache.Instance.DirectEve.Me.SuspectTimerRemainingSeconds + " sec ]");
                            return false;
                        }
                        if (DirectEve.Interval(10000)) Log.WriteLine("Suspect Timer: IgnoreSuspectTimer [true]");
                    }
                }
                if (DirectEve.Interval(10000)) Log.WriteLine("No Suspect Timer");

                //if (ESCache.Instance.DirectEve.Me.IsInvasionActive && !ESCache.Instance.InWormHoleSpace && ESCache.Instance.SelectedController != "AbyssalDeadspaceController")
                //{
                //    Log.WriteLine("Invasion is active in local: waiting in station!");
                //    return false;
                //}

                if (ESCache.Instance.DirectEve.Me.IsIncursionActive)
                {
                    if (!ESCache.Instance.InWormHoleSpace)
                    {
                        if (ESCache.Instance.SelectedController != "AbyssalDeadspaceController")
                        {
                            Log.WriteLine("Incursion is active in local: undocking anyway");
                            //return false;
                        }
                    }
                }
                if (DirectEve.Interval(10000)) Log.WriteLine("No Active Incursion in local");

                if (ESCache.Instance.EveAccount.RequireOmegaClone && !ESCache.Instance.DirectEve.Me.IsOmegaClone)
                {
                    Log.WriteLine("IsOmegaClone [" + ESCache.Instance.DirectEve.Me.IsOmegaClone + "] and RequireOmegaClone [" + ESCache.Instance.EveAccount.RequireOmegaClone + "]:  PauseAfterNextDock [true]");
                    ESCache.Instance.PauseAfterNextDock = true;
                    return false;
                }

                if (UndockAttempts + ESCache.Instance.RandomNumber(0, 4) > 10)
                //If we are having to retry at all there is likely something very wrong. Make it non-obvious if we do have to restart by restarting at diff intervals.
                {
                    string msg = "This is not the destination station, we have tried to undock [" + UndockAttempts +
                                 "] times - and it is evidentially not working (lag?) - restarting Questor (and EVE)";
                    ESCache.Instance.CloseEveReason = msg;
                    ESCache.Instance.BoolRestartEve = true;
                    return false;
                }

                if (Cleanup.intReDockTorepairModules > 5000)
                {
                    Log.WriteLine("intReDockTorepairModules [" + Cleanup.intReDockTorepairModules + "] - waiting in station! We probably need to strip our fitting and reload it");
                    int intModule = 0;
                    foreach (var module in ESCache.Instance.DirectEve.Modules)
                    {
                        intModule++;
                        Log.WriteLine("[" + intModule + "][" + module.TypeName + "] HeatDamagePercent [" + module.HeatDamagePercent + "] IsOnline [" + module.IsOnline + "]");
                    }

                    return false;
                }

                if (DateTime.UtcNow > Time.Instance.NextUndockAction)
                {
                    if (!ESCache.Instance.OkToInteractWithEveNow)
                    {
                        if (DebugConfig.DebugInteractWithEve) Log.WriteLine("Undock: !OkToInteractWithEveNow");
                        return false;
                    }

                    if (DirectEve.Interval(1000) && ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation))
                    {
                        Log.WriteLine("Exiting station.");
                        //Util.FlushMemIfThisProcessIsUsingTooMuchMemory(800);
                        MemoryOptimizer.OptimizeMemory();
                        UndockAttempts++;
                        ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                        DirectSession.SetSessionNextSessionReady();
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.UNDOCK, "Undocking from station."));
                        Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.RandomNumber(4000, 6000));
                        Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(Time.Instance.TravelerExitStationAmIInSpaceYet_seconds);
                        Time.Instance.NextUndockAction = DateTime.UtcNow.AddSeconds(Time.Instance.TravelerExitStationAmIInSpaceYet_seconds + ESCache.Instance.RandomNumber(0, 7));
                        Time.Instance.LastUndockAction = DateTime.UtcNow;
                        Time.Instance.LastDockAction = DateTime.UtcNow;
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AmountExceptionsCurrentSession), 0);
                        return true;
                    }

                    return false;
                }

                if (DebugConfig.DebugTraveler)
                    Log.WriteLine("LastInSpace is more than 45 sec old (we are docked), but NextUndockAction is still in the future [" +
                                  Time.Instance.NextUndockAction.Subtract(DateTime.UtcNow).TotalSeconds + "seconds]");

                // We are not UnDocked yet
                return false;
            }

            return false;
        }

        /// <summary>
        ///     This function returns true if we are at the final destination and false if the task is not yet complete
        /// </summary>
        /// <returns></returns>
        public abstract bool PerformFinalDestinationTask();

        internal static bool UseInstaBookmark()
        {
            try
            {
                if (ESCache.Instance.InWarp) return false;

                if (ESCache.Instance.InSpace)
                {
                    if ((ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe) ||
                        (ESCache.Instance.ClosestDockableLocation != null && ESCache.Instance.ClosestDockableLocation.IsOnGridWithMe))
                    {
                        if (ESCache.Instance.UndockBookmark != null)
                        {
                            if (ESCache.Instance.UndockBookmark.IsInCurrentSystem)
                            {
                                double distance = ESCache.Instance.DistanceFromMe(ESCache.Instance.UndockBookmark.X ?? 0, ESCache.Instance.UndockBookmark.Y ?? 0,
                                    ESCache.Instance.UndockBookmark.Z ?? 0);
                                if (distance < (int)Distances.WarptoDistance)
                                {
                                    Log.WriteLine("Arrived at undock bookmark [" + ESCache.Instance.UndockBookmark.Title +
                                                  "]");
                                    ESCache.Instance.UndockBookmark = null;
                                    return true;
                                }

                                if (distance >= (int)Distances.WarptoDistance)
                                {
                                    if (ESCache.Instance.UndockBookmark.WarpTo())
                                    {
                                        Log.WriteLine("Warping to undock bookmark [" + ESCache.Instance.UndockBookmark.Title +
                                                      "][" + Math.Round(distance / 1000 / 149598000, 2) + " AU away]");
                                        //if (!Combat.ReloadAll(Cache.Instance.EntitiesNotSelf.OrderBy(t => t.Distance).FirstOrDefault(t => t.Distance < (double)Distance.OnGridWithMe))) return false;
                                        NextTravelerDestinationAction = DateTime.UtcNow.AddSeconds(10);
                                        return true;
                                    }

                                    return false;
                                }

                                return false;
                            }

                            if (DebugConfig.DebugUndockBookmarks)
                                Log.WriteLine("Bookmark Named [" + ESCache.Instance.UndockBookmark.Title +
                                              "] was somehow picked as an UndockBookmark but it is not in local with us! continuing without it.");
                            return true;
                        }

                        if (DebugConfig.DebugUndockBookmarks)
                            Log.WriteLine("No undock bookmarks in local matching our undockPrefix [" + Settings.Instance.UndockBookmarkPrefix +
                                          "] continuing without it.");
                        return true;
                    }

                    if (DebugConfig.DebugUndockBookmarks)
                        Log.WriteLine("Not currently on grid with a station or a stargate: continue traveling");
                    return true;
                }

                if (DebugConfig.DebugUndockBookmarks)
                    Log.WriteLine("InSpace [" + ESCache.Instance.InSpace + "]: waiting until we have been undocked or in system a few seconds");
                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        #endregion Methods
    }
}