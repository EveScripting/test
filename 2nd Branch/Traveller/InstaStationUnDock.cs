extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.States;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Traveller
{
    public static class InstaStationUnDock
    {
        #region Fields

        private static int distanceToMakeBookmarkFromStation;

        #endregion Fields

        #region Methods

        public static bool ChangeInstaStationUndockState(InstaStationUndockState state, bool wait = false)
        {
            try
            {
                if (State.CurrentInstaStationUndockState != state)
                {
                    if (DebugConfig.DebugUndockBookmarks) Log.WriteLine("New InstaStationUndockState [" + state + "]");
                    State.CurrentInstaStationUndockState = state;
                    if (wait || ESCache.Instance.LastInteractedWithEVE == DateTime.UtcNow)
                        return true;
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

        public static void ClearPocketSpecificSettings()
        {
            try
            {
                State.CurrentInstaStationUndockState = InstaStationUndockState.Idle;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void ProcessState()
        {
            try
            {
                if (ESCache.Instance.InWormHoleSpace)
                {
                    if (DebugConfig.DebugUndockBookmarks) Log.WriteLine("if (ESCache.Instance.InWormHoleSpace): No Stations here, only citadels and we dont insta off citadels: tether");
                    ChangeInstaStationUndockState(InstaStationUndockState.Done);
                }

                switch (State.CurrentInstaStationUndockState)
                {
                    case InstaStationUndockState.Idle:
                        {
                            State.CurrentInstaStationUndockState = InstaStationUndockState.DidWeJustUndock;
                            break;
                        }

                    case InstaStationUndockState.DidWeJustUndock:
                        {
                            if (DateTime.UtcNow < Time.Instance.NextUndockAction.AddSeconds(15))
                            {
                                distanceToMakeBookmarkFromStation = 150000 + (ESCache.Instance.RandomNumber(3, 20) * 1000);
                                ChangeInstaStationUndockState(InstaStationUndockState.JustUndocked);
                                break;
                            }

                            State.CurrentInstaStationUndockState = InstaStationUndockState.Done;
                            break;
                        }

                    case InstaStationUndockState.JustUndocked:
                        {
                            JustUndocked();
                            break;
                        }

                    case InstaStationUndockState.CreateNewUndockBookmark:
                        {
                            MakeInstaUndockBookmark();
                            break;
                        }

                    case InstaStationUndockState.Done:
                        {
                            //if (DebugConfig.DebugUndockBookmarks) Log.WriteLine("InstaStationUndockState is [" + State.CurrentInstaStationUndockState + "]");
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void JustUndocked()
        {
            try
            {
                if (ESCache.Instance.Stations.Count > 0)
                {
                    DirectBookmark warpOutBookmark = null;
                    try
                    {
                        if (ESCache.Instance.CachedBookmarks != null && ESCache.Instance.CachedBookmarks.Count > 0)
                            foreach (DirectBookmark tempBookmark in ESCache.Instance.CachedBookmarks.Where(x => x.BookmarkType == BookmarkType.Coordinate && x.IsInCurrentSystem).OrderByDescending(i => i.CreatedOn))
                            {
                                if (tempBookmark.Title == null)
                                    continue;

                                if (tempBookmark.LocationId == null)
                                    continue;

                                if (ESCache.Instance.DirectEve.Session.SolarSystemId == null)
                                    continue;

                                if (tempBookmark.Title.ToLower().Contains(Settings.Instance.UndockBookmarkPrefix.ToLower()))
                                    if (tempBookmark.IsInCurrentSystem)
                                    {
                                        //b.DistanceFromEntity(ESCache.Instance.ClosestStation._directEntity) < 10000000 &&
                                        //b.DistanceFromEntity(ESCache.Instance.ClosestStation._directEntity) > 150000);
                                        warpOutBookmark = tempBookmark;
                                        break;
                                    }
                            }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }

                    long solarid = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (ESCache.Instance.Stations != null && ESCache.Instance.ClosestStation.Distance < 50000)
                    {
                        if (warpOutBookmark == null)
                        {
                            if (Settings.Instance.UseUndockBookmarks)
                                Util.PlayNoticeSound();

                            if (DebugConfig.DebugUndockBookmarks) Log.WriteLine("No Bookmark (yet) with undockPrefix [" + Settings.Instance.UndockBookmarkPrefix + "] in System");

                            if (Settings.Instance.CreateUndockBookmarksAsNeeded)
                            {
                                //ChangeInstaStationUndockState(InstaStationUndockState.CreateNewUndockBookmark);
                                //return;
                            }

                            ChangeInstaStationUndockState(InstaStationUndockState.Done);
                            return;
                        }

                        if (warpOutBookmark.LocationId == solarid)
                        {
                            if (warpOutBookmark.WarpTo())
                                return;

                            return;
                        }
                    }

                    if (ESCache.Instance.ClosestStation != null)
                    {
                        if (DebugConfig.DebugUndockBookmarks) Log.WriteLine("We are not currently close to the station. We are [" + Math.Round(ESCache.Instance.ClosestStation.Distance / 1000, 0) + "k ] from [" + ESCache.Instance.ClosestDockableLocation.Name + "].");
                    }
                    else if (DebugConfig.DebugUndockBookmarks) Log.WriteLine("No Station: might be a citadel, but we dont insta off citadels because: tether");

                    State.CurrentInstaStationUndockState = InstaStationUndockState.Done;
                    return;
                }

                if (DebugConfig.DebugUndockBookmarks) Log.WriteLine("There are no stations in this system");
                State.CurrentInstaStationUndockState = InstaStationUndockState.Done;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool MakeInstaUndockBookmark()
        {
            if (!Settings.Instance.CreateUndockBookmarksAsNeeded)
            {
                if (DebugConfig.DebugUndockBookmarks) Log.WriteLine("MakeInstaUndockBookmark: if (!Settings.Instance.CreateUndockBookmarksAsNeeded) --> Done");
                ChangeInstaStationUndockState(InstaStationUndockState.Done);
                return true;
            }

            IEnumerable<DirectBookmark> bookMarksOnGrid = ESCache.Instance.CachedBookmarks.Where(i => i.IsInCurrentSystem && i.DistanceFromEntity(ESCache.Instance.MyShipEntity._directEntity) < 900000);
            if (bookMarksOnGrid.Any(i => i.DistanceFromEntity(ESCache.Instance.ClosestDockableLocation._directEntity) > 152000))
            {
                if (DebugConfig.DebugUndockBookmarks) Log.WriteLine("MakeInstaUndockBookmark: Found bookmark on grid with station more than 150k from it");
                ChangeInstaStationUndockState(InstaStationUndockState.Done);
                return true;
            }

            if (ESCache.Instance.Stations.Count > 0)
            {
                //this is purposely NOT ClosestDocableLocation: that would include citadels and citadels get tethering, no need to insta off
                if (ESCache.Instance.ClosestStation == null)
                {
                    ChangeInstaStationUndockState(InstaStationUndockState.Done);
                    return true;
                }

                if (ESCache.Instance.ClosestStation.Distance < distanceToMakeBookmarkFromStation)
                {
                    //
                    // wait until we are greater than 150k away before making the bookmark
                    //
                    Defense.AlwaysActivateSpeedModForThisGridOnly = true;
                    if (DebugConfig.DebugUndockBookmarks) Log.WriteLine("MakeInstaUndockBookmark: waiting until we are 160k from the station");
                    return false;
                }

                //if (DebugConfig.DebugUndockBookmarks) Log.WriteLine("MakeInstaUndockBookmark: making bookmark");
                //Defense.AlwaysActivateSpeedModForThisGridOnly = false;
                //ESCache.Instance.DirectEve.BookmarkCurrentLocation(Settings.Instance.UndockBookmarkPrefix, "", null);
                ChangeInstaStationUndockState(InstaStationUndockState.Done);
                return true;
            }

            if (DebugConfig.DebugUndockBookmarks) Log.WriteLine("There are no stations in this system");
            ChangeInstaStationUndockState(InstaStationUndockState.Done);
            return true;
        }

        #endregion Methods
    }
}