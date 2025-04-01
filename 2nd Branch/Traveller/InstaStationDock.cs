extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Traveller
{
    public static class InstaStationDock
    {
        #region Fields

        private static DateTime lastInstaStationDockAction = DateTime.UtcNow;
        private static DateTime lastWhereAreWeCheck = DateTime.UtcNow;
        private static int processStateIteractions;
        private static int DistanceToMakeBookmarkFromStation { get; set; }

        #endregion Fields

        #region Methods

        public static bool ChangeInstaStationDockState(InstaStationDockState state, bool wait = false)
        {
            try
            {
                if (State.CurrentInstaStationDockState != state)
                {
                    if (DebugConfig.DebugDockBookmarks || DebugConfig.DebugTraveler) Log.WriteLine("New InstaStationDockState [" + state + "]");
                    State.CurrentInstaStationDockState = state;
                    if (wait || ESCache.Instance.LastInteractedWithEVE == DateTime.UtcNow)
                        return true;
                    // ProcessState again as we have not interacted with eve thus we do not need to wait 500ms
                    // the ability to ProcessState again here is the whole reason for ChangeArmState()
                    ProcessState();
                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static void ProcessState()
        {
            try
            {


                /**
                if (DateTime.UtcNow < lastInstaStationDockAction.AddMilliseconds(200))
                {
                    processStateIteractions++;
                    if (DebugConfig.DebugTraveler) Log.WriteLine("InstaStationDock: ProcessState [" + DateTime.UtcNow + "]");
                }
                else
                {
                    processStateIteractions = 0;
                }

                if (processStateIteractions > 5)
                    return;
                **/

                lastInstaStationDockAction = DateTime.UtcNow;

                if (ESCache.Instance.InStation)
                {
                    ChangeInstaStationDockState(InstaStationDockState.Done);
                    return;
                }

                if (ESCache.Instance.InWarp)
                    return;

                if (DebugConfig.DebugTraveler) Log.WriteLine("InstaStationDock: State.CurrentInstaStationDockState [" + State.CurrentInstaStationDockState + "]");

                switch (State.CurrentInstaStationDockState)
                {
                    case InstaStationDockState.Idle:
                        {
                            ChangeInstaStationDockState(InstaStationDockState.WhereAreWe);
                            break;
                        }

                    case InstaStationDockState.WhereAreWe:
                        {
                            WhereAreWe();
                            break;
                        }

                    case InstaStationDockState.JustArrivedInSystem:
                        {
                            JustArrivedInSystem();
                            break;
                        }

                    case InstaStationDockState.JustArrivedAtStation:
                        {
                            JustArrivedAtStation();
                            break;
                        }

                    case InstaStationDockState.CreateNewDockBookmark:
                        {
                            MakeInstaDockBookmark();
                            break;
                        }

                    case InstaStationDockState.WaitForTraveler:
                        {
                            if (DateTime.UtcNow > lastWhereAreWeCheck.AddSeconds(7))
                                ChangeInstaStationDockState(InstaStationDockState.WhereAreWe);

                            break;
                        }

                    case InstaStationDockState.Done:
                        {
                            if (DebugConfig.DebugDockBookmarks) Log.WriteLine("InstaStationDockState is [" + State.CurrentInstaStationDockState + "]");
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void JustArrivedAtStation()
        {
            ChangeInstaStationDockState(InstaStationDockState.CreateNewDockBookmark);
        }

        private static void JustArrivedInSystem()
        {
            if (ESCache.Instance.InWarp) return;

            if (NavigateOnGrid.StationToGoTo != null)
            {
                DirectBookmark stationDockBookmark = null;
                if (ESCache.Instance.CachedBookmarks != null && ESCache.Instance.CachedBookmarks.Count > 0)
                {
                    IOrderedEnumerable<DirectBookmark> BookmarksToLookThrough = null;
                    if (ESCache.Instance.DirectEve.Session.IsKnownSpace)
                    {
                        BookmarksToLookThrough = ESCache.Instance.CachedBookmarks.Where(i => i.SolarSystem != null && i.IsKSpace && i.BookmarkType == BookmarkType.Coordinate).OrderBy(b => b.Distance);
                    }
                    else BookmarksToLookThrough = ESCache.Instance.CachedBookmarks.Where(i => !i.IsKSpace && i.BookmarkType == BookmarkType.Coordinate).OrderBy(b => b.Distance);

                    foreach (DirectBookmark thisBookmark in BookmarksToLookThrough)
                    {
                        if (thisBookmark.IsInCurrentSystem && NavigateOnGrid.StationToGoTo != null) //Bookmark is in local and so is the station...
                        {
                            if (DebugConfig.DebugDockBookmarks) Log.WriteLine("ThisBookmark [" + thisBookmark.Title + "] LocationId [" + thisBookmark.LocationId + "] found in local");
                            if (thisBookmark.Title.ToLower().Contains(Settings.Instance.StationDockBookmarkPrefix.ToLower()))
                            {
                                if (DebugConfig.DebugDockBookmarks)
                                    try
                                    {
                                        Log.WriteLine("ThisBookmark [" + thisBookmark.Title + "] contains the StationDockPrefix [" + Settings.Instance.StationDockBookmarkPrefix + "]");
                                        //Log.WriteLine("StationToGoto X [" + NavigateOnGrid.StationToGoTo._directEntity.X + "]");
                                        //Log.WriteLine("StationToGoto Y [" + NavigateOnGrid.StationToGoTo._directEntity.Y + "]");
                                        //Log.WriteLine("StationToGoto Z [" + NavigateOnGrid.StationToGoTo._directEntity.Z + "]");
                                        //Log.WriteLine("thisBookmark X [" + thisBookmark.X + "]");
                                        //Log.WriteLine("thisBookmark Y [" + thisBookmark.Y + "]");
                                        //Log.WriteLine("thisBookmark Z [" + thisBookmark.Z + "]");
                                        Log.WriteLine("Distance from bookmark to station [" + Math.Round((double)thisBookmark.DistanceFromEntity(NavigateOnGrid.StationToGoTo._directEntity), 0) / 1000 + "k]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.WriteLine("Exception [" + ex + "]");
                                    }

                                if (thisBookmark.DistanceFromEntity(NavigateOnGrid.StationToGoTo._directEntity) <= 95000) //95k and if inside station we can still be an instadock, counterintuitively.
                                {
                                    stationDockBookmark = thisBookmark;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (stationDockBookmark == null)
                {
                    if (DirectEve.Interval(10000))
                    {
                        if (Settings.Instance.UseDockBookmarks)
                            Util.PlayNoticeSound();

                        Log.WriteLine("Notification: No Bookmark (yet) with dockPrefix: [" + Settings.Instance.StationDockBookmarkPrefix + "] in System.");
                    }
                }

                long solarid = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                if (NavigateOnGrid.StationToGoTo.Distance > 150000 && ESCache.Instance.Stargates.Count > 0 && ESCache.Instance.Stargates.All(i => i.Distance > 12000))
                {
                    if (stationDockBookmark == null)
                    {
                        //Log.WriteLine("No Bookmark (yet) with dockPrefix [" + Settings.Instance.StationDockBookmarkPrefix + "] in System");

                        if (NavigateOnGrid.StationToGoTo.Distance > (double)Distances.WarptoDistance)
                        {
                            NavigateOnGrid.StationToGoTo.WarpTo();
                            return;
                        }

                        ChangeInstaStationDockState(InstaStationDockState.Done, true);
                        return;
                    }

                    if (stationDockBookmark.LocationId == solarid)
                    {
                        if (stationDockBookmark.WarpTo())
                        {
                            Log.WriteLine("Warped to stationDockBookmark [" + stationDockBookmark.Title + "] near [" + NavigateOnGrid.StationToGoTo.Name + "]");
                            return;
                        }

                        return;
                    }
                }

                if (DebugConfig.DebugDockBookmarks) Log.WriteLine("We are currently too close to the station to warp. We are [" + Math.Round(NavigateOnGrid.StationToGoTo.Distance / 1000, 0) + "k ] from [" + NavigateOnGrid.StationToGoTo.Name + "].");
                if (NavigateOnGrid.StationToGoTo.Distance < 10000)
                {
                    ChangeInstaStationDockState(InstaStationDockState.JustArrivedAtStation);
                    return;
                }

                ChangeInstaStationDockState(InstaStationDockState.Done, true);
                return;
            }

            if (DebugConfig.DebugDockBookmarks) Log.WriteLine("There are no stations in this system");
            ChangeInstaStationDockState(InstaStationDockState.Done, true);
        }

        private static bool MakeInstaDockBookmark()
        {
            if (ESCache.Instance.InWarp) return false;

            if (!Settings.Instance.CreateDockBookmarksAsNeeded)
            {
                if (DebugConfig.DebugDockBookmarks) Log.WriteLine("MakeInstaDockBookmark: if (!Settings.Instance.CreateUndockBookmarksAsNeeded) --> Done");
                ChangeInstaStationDockState(InstaStationDockState.Done, true);
                return true;
            }

            if (NavigateOnGrid.StationToGoTo == null)
            {
                Log.WriteLine("MakeInstaDockBookmark: if (NavigateOnGrid.stationToGoTo == null)");
                ChangeInstaStationDockState(InstaStationDockState.Done, true);
                return true;
            }

            IEnumerable<DirectBookmark> bookMarksOnGrid = ESCache.Instance.CachedBookmarks.Where(i => i.IsInCurrentSystem && i.DistanceFromEntity(ESCache.Instance.MyShipEntity._directEntity) < 140000);
            if (bookMarksOnGrid.Any(i => i.DistanceFromEntity(NavigateOnGrid.StationToGoTo._directEntity) < 30000))
            {
                Log.WriteLine("MakeInstaDockBookmark: Found bookmark on grid with station less than 0k from it (inside docking ring)");
                ChangeInstaStationDockState(InstaStationDockState.Done, true);
                return true;
            }

            if (ESCache.Instance.DockableLocations.Any(i => i.IsOnGridWithMe))
            {
                if (NavigateOnGrid.StationToGoTo.Distance > -500)
                {
                    if (NavigateOnGrid.StationToGoTo.Distance > (double)Distances.WarptoDistance)
                    {
                        NavigateOnGrid.StationToGoTo.WarpTo();
                        return false;
                    }
                    //
                    // wait until we are less than -2k (or so) away before making the bookmark
                    //
                    Defense.AlwaysActivateSpeedModForThisGridOnly = true;
                    NavigateOnGrid.StationToGoTo.Approach();
                    Log.WriteLine("MakeInstaDockBookmark: waiting until we are inside the docking ring of the station [" + Math.Round(NavigateOnGrid.StationToGoTo.Distance / 1000, 2) + "k]");
                    return false;
                }

                Log.WriteLine("MakeInstaDockBookmark: making bookmark");
                Defense.AlwaysActivateSpeedModForThisGridOnly = false;

                if (!bookMarksOnGrid.Any(i => i.Distance < 2000 && i.Title.Contains(Settings.Instance.StationDockBookmarkPrefix)))
                {
                    //if (ESCache.Instance.DirectEve.BookmarkCurrentLocation(Settings.Instance.StationDockBookmarkPrefix, "", null))
                    //{
                    //    Log.WriteLine("MakeInstaDockBookmark: making stationDockBookmark [" + Settings.Instance.StationDockBookmarkPrefix + "] for [" + NavigateOnGrid.StationToGoTo.Name + "]");
                    //    WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
                    //    Time.Instance.NextBookmarkAction = DateTime.UtcNow.AddSeconds(10);
                    //    ChangeInstaStationDockState(InstaStationDockState.Done, true);
                    //    return true;
                    //}

                    Log.WriteLine("MakeInstaDockBookmark: we have a bookmark within 2k of here... aborting making another bookmark");
                    return true;
                }

                return true;
            }

            Log.WriteLine("MakeInstaDockBookmark: There are no stations on grid");
            ChangeInstaStationDockState(InstaStationDockState.Done, true);
            return true;
        }

        private static void WhereAreWe()
        {
            if (ESCache.Instance.InWarp) return;

            lastWhereAreWeCheck = DateTime.UtcNow;
            DistanceToMakeBookmarkFromStation = -1000 - (ESCache.Instance.RandomNumber(1, 4) * 1000);

            if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.Distance > 10000 && ESCache.Instance.ClosestStargate.Distance < 20000)
            {
                ChangeInstaStationDockState(InstaStationDockState.JustArrivedInSystem);
                return;
            }

            if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.Distance < 10000)
            {
                ChangeInstaStationDockState(InstaStationDockState.Done, true);
                return;
            }

            if (NavigateOnGrid.StationToGoTo == null)
            {
                ChangeInstaStationDockState(InstaStationDockState.Done, true);
                return;
            }

            if (NavigateOnGrid.StationToGoTo != null && NavigateOnGrid.StationToGoTo.Distance < 10000)
            {
                ChangeInstaStationDockState(InstaStationDockState.JustArrivedAtStation);
                return;
            }

            ChangeInstaStationDockState(InstaStationDockState.JustArrivedInSystem);
        }

        #endregion Methods
    }
}